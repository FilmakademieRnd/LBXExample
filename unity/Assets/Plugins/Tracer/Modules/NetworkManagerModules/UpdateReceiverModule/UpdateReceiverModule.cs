/*
-----------------------------------------------------------------------------------
TRACER Location Based Experience Example

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://github.com/FilmakademieRnd/LBXExample

TRACER Location Based Experience Example is a development by Filmakademie 
Baden-Wuerttemberg, Animationsinstitut R&D Labs in the scope of the EU funded 
project EMIL (101070533).

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/


//! @file "UpdateReceiverModule.cs"
//! @brief Implementation of the update receiver module, listening to parameter updates from clients
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 28.10.2021

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using System.ComponentModel;

namespace tracer
{
    //!
    //! Class implementing the scene sender module, listening to scene requests and sending scene data.
    //!
    public class UpdateReceiverModule : NetworkManagerModule
    {
        //!
        //! Buffer for storing incoming message by time (array of lists of bytes).
        //!
        private List<byte[]>[] m_messageBuffer;

        //!
        //! Event emitted when parameter change should be added to undo/redo history
        //!
        public event EventHandler<AbstractParameter> receivedHistoryUpdate;

        //!
        //! A referece to TRACER's scene manager.
        //!
        private SceneManager m_sceneManager;

        //!
        //! Constructor
        //!
        //! @param  name  The  name of the module.
        //! @param core A reference to the TRACER core.
        //!
        public UpdateReceiverModule(string name, Manager manager) : base(name, manager)
        {
        }

        //locking fix
        private readonly object _lock = new object();

        //!
        //! Cleaning up event registrations. 
        //!
        protected override void Cleanup(object sender, EventArgs e)
        {
            base.Cleanup(sender, e);
            core.timeEvent -= consumeMessages;
            m_sceneManager.sceneReady -= connectAndStart;
        }

        //!
        //! Function for custom initialisation.
        //! 
        //! @param sender The TRACER core.
        //! @param e The pssed event arguments.
        //! 
        protected override void Init(object sender, EventArgs e)
        {
            // initialize message buffer
            lock(_lock){
                m_messageBuffer = new List<byte[]>[core.timesteps];
                for (int i = 0; i < core.timesteps; i++)
                    m_messageBuffer[i] = new List<byte[]>(64);
            }

            m_sceneManager = core.getManager<SceneManager>();
            m_sceneManager.sceneReady += connectAndStart;
        }



        //!
        //! Function that connects the scene object change events for parameter queuing.
        //!
        //! @param sender The emitting scene object.
        //! @param e The pssed event arguments.
        //!
        private void connectAndStart(object sender, EventArgs e)
        {
            startUpdateReceiver(manager.settings.ipAddress.value, "5556");

            core.timeEvent += consumeMessages;
        }

        //!
        //! Function, waiting for incoming message (executed in separate thread).
        //! Control message are executed immediately, parameter update message are buffered
        //! and executed later to obtain synchronicity.
        //!
        protected override void run()
        {
            m_isRunning = true;
            AsyncIO.ForceDotNet.Force();
            var receiver = new SubscriberSocket();
            m_socket = receiver;
            receiver.SubscribeToAnyTopic();
            receiver.Connect("tcp://" + m_ip + ":" + m_port);
            Helpers.Log("Update receiver connected: " + "tcp://" + m_ip + ":" + m_port);
            byte[] message = null;
            List<byte[]> messages = new List<byte[]>();
            while (m_isRunning)
            {
                try
                {
                    if (receiver.TryReceiveMultipartBytes(System.TimeSpan.FromSeconds(1), ref messages))
                    {
                        for (int i = 0; i < messages.Count; i++) 
                        {
                            message = messages[i];
                            if (message != null)
                            {
                                if (message[0] != manager.cID)
                                {
                                    lock (_lock)
                                    {
                                        switch ((MessageType)message[2])
                                        {
                                            case MessageType.LOCK:
                                                decodeLockMessage(message);
                                                break;
                                            case MessageType.SYNC:
                                                decodeSyncMessage(message);
                                                break;
                                            case MessageType.RESETOBJECT:
                                                decodeResetMessage(message);
                                                break;
                                            case MessageType.UNDOREDOADD:
                                                decodeUndoRedoMessage(message);
                                                break;
                                            case MessageType.DATAHUB:
                                                decodeDataHubMessage(message);
                                                break;
                                            case MessageType.RPC:
                                            case MessageType.PARAMETERUPDATE:
                                                // make sure that producer and consumer exclude eachother
                                                // message[1] is time
                                                //int time = (message[1] + (Mathf.RoundToInt((float)manager.pingRTT * 0.5f))) % core.timesteps;
                                                //m_messageBuffer[time].Add(message);
                                                m_messageBuffer[message[1]].Add(message);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e) { Helpers.Log(e.Message, Helpers.logMsgType.ERROR); }
                Thread.Yield();
            }
        }

        //! 
        //! Function that decodes a sync message and set the clients global time.
        //!
        //! @param message The message to be decoded.
        //! 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void decodeSyncMessage(byte[] message)
        {
            int runtime = Mathf.FloorToInt(manager.pingRTT * 0.5f);  // *0.5 to convert rtt to one way
            int coreTime = core.time;
            int syncTime = message[1] + runtime;
            int deltaTime = Helpers.DeltaTime(core.time, message[1], core.timesteps);

            if (deltaTime > 10 ||
                deltaTime > 3 && runtime < 8)
            {
                core.time = (byte)(Mathf.RoundToInt(syncTime) % core.timesteps);
                UnityEngine.Debug.LogWarning("decodeSyncMessage::Core time updated to: " + core.time);
            }
        }

        //! 
        //! Function that decodes a lock message and lock or unlock the corresponding scene object.
        //!
        //! @param message The message to be decoded.
        //!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void decodeLockMessage(byte[] message)
        {
            bool lockState = BitConverter.ToBoolean(message, 6);

            if (lockState)
            {
                byte sceneID = message[3];
                short sceneObjectID = BitConverter.ToInt16(message, 4);

                SceneObject sceneObject = m_sceneManager.getSceneObject(sceneID, sceneObjectID);
                if(sceneObject)                         //if we spawn an object but its not yet initiated at the client, this could happen
                    sceneObject.SetLock(lockState);
                else
                    UnityEngine.Debug.LogWarning("SceneObject for sceneObjectID not found: "+(int)sceneObjectID);   //maybe delay the lock?
            }
            // delay unlock message
            else
            {
                int bufferTime = (((message[1] + core.settings.framerate / 4) + core.timesteps) % core.timesteps);
                m_messageBuffer[bufferTime].Add(message);
            }
        }

        //! 
        //! Function that decodes a undo/redo message to reset the corresponding scene objects to their last state.
        //!
        //! @param message The message to be decoded.
        //!
        private void decodeUndoRedoMessage(byte[] message)
        {
            byte sceneID = message[3];
            short sceneObjectID = BitConverter.ToInt16(message, 4);
            short parameterID = BitConverter.ToInt16(message, 6);

            ParameterObject sceneObject = core.getParameterObject(sceneID, sceneObjectID);

            receivedHistoryUpdate?.Invoke(this, sceneObject.parameterList[parameterID]);
        }

        //! 
        //! Function that decodes a reset message reset the corresponding scene objects states to their defaults.
        //!
        //! @param message The message to be decoded.
        //!
        private void decodeResetMessage(byte[] message)
        {
            byte sceneID = message[3];
            short sceneObjectID = BitConverter.ToInt16(message, 4);
            SceneObject sceneObject = m_sceneManager.getSceneObject(sceneID, sceneObjectID);

            foreach (AbstractParameter p in sceneObject.parameterList)
                p.reset();
            m_sceneManager.getModule<UndoRedoModule>().vanishHistory(sceneObject);
        }

        //! 
        //! Function that decodes a DataHubMessage to inform the system that a client connection has changed.
        //!
        //! @param message The message to be decoded.
        //!
        private void decodeDataHubMessage(byte[] message)
        {
            byte dhType = message[3];
            bool status = BitConverter.ToBoolean(message, 4);
            byte cID = message[5];

            // dhType 0 = client connection status update
            if (dhType == 0 &&
                cID != manager.cID)
                manager.ClientConnectionUpdate(status, cID);
        }

        //!
        //! Function that triggers the parameter updates (called once a global time tick).
        //! It also decodes all parameter message and update the corresponding parameters. 
        //!
        private void consumeMessages(object o, EventArgs e)
        {
            // define the buffer size by defining the time offset in the ringbuffer
            // % time steps to take ring (0 to core.timesteps) into account
            // set to 1/10 second
            // int bufferTime = (((core.time - core.settings.framerate / 10) + core.timesteps) % core.timesteps);

            int bufferTime = (((core.time - core.settings.framerate / 6) + core.timesteps) % core.timesteps); // [MINO] 

            // caching the ParameterObject
            byte oldSceneID = 0;
            short oldParameterObjectID = 0;

            lock (_lock)
            {
                List<byte[]> timeSlotBuffer = m_messageBuffer[bufferTime];

                for (int i = 0; i < timeSlotBuffer.Count; i++)
                {
                    Span<byte> message = timeSlotBuffer[i];

                    if ((MessageType)message[2] == MessageType.LOCK)
                    {
                        byte sceneID = message[3];
                        short parameterObjectID = MemoryMarshal.Read<short>(message.Slice(4));
                        bool lockState = MemoryMarshal.Read<bool>(message.Slice(6));

                        SceneObject sceneObject = m_sceneManager.getSceneObject(sceneID, parameterObjectID);
                        sceneObject.SetLock(lockState);
                    }
                    else
                    {
                        ParameterObject parameterObject = null;
                        bool paraObjectNotFound = true;
                        int start = 3;
                        while (start < message.Length)
                        {
                            byte sceneID = message[start];
                            short parameterObjectID = MemoryMarshal.Read<short>(message.Slice(start + 1));
                            short parameterID = MemoryMarshal.Read<short>(message.Slice(start + 3));
                            int length = message[start + 6];

                            if (paraObjectNotFound || sceneID != oldSceneID || parameterObjectID != oldParameterObjectID)
                                parameterObject = core.getParameterObject(sceneID, parameterObjectID);

                            if (parameterObject != null)
                            {
                                parameterObject.parameterList[parameterID].deSerialize(message, start + 7);
                                //parameterID could be out of bounds, if we spawn an object and update it, before its spawned on the client!
                                paraObjectNotFound = false;
                            }
                            else
                            {
                                paraObjectNotFound = true;
                            }

                            start += length;
                            oldSceneID = sceneID;
                            oldParameterObjectID = parameterObjectID;
                        }
                    }
                }

                timeSlotBuffer.Clear();
            
            } //lock
        }

        private int latestUpdateTime = -100;
        private const int MAX_DIFFERENCE = 40;
        private const int MAX_LOOP_OFFSET = 120 - 40;   //if latest update time is above this, then we'll allow new updates above zero and below MAX_DIFFERENCE

        private Dictionary<short, int> sceneObjectLatestUpdateTime = new Dictionary<short, int>();

        /*private void checkMsgBuffer(object o, EventArgs e){
            lock(_lock){
                string notEmpty = "";;
                for(int x = 0; x<m_messageBuffer.Length;x++){
                    if(m_messageBuffer[x].Count != 0){
                        notEmpty += ","+x;
                    }    
                }
                if(!string.IsNullOrEmpty(notEmpty))
                    UnityEngine.Debug.Log("NOT EMPTY AT "+notEmpty);
                //UnityEngine.Debug.Break();
                //return;
                //UnityEngine.Debug.Log("<<<<<<<<<<<<<<< EMPTY!");
            }
        }*/

        //!
        //! Function to start the scene sender module.
        //! @param ip The IP address to be used from the sender.
        //! @param port The port number to be used from the sender.
        //!
        void startUpdateReceiver(string ip, string port)
        {
            start(ip, port);
        }
    }
}
