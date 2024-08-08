/*
-----------------------------------------------------------------------------------
TRACER FOUNDATION -
Toolset for Realtime Animation, Collaboration & Extended Reality

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://research.animationsinstitut.de/tracer 
https://github.com/FilmakademieRnd/TRACER

TRACER FOUNDATION is a development by Filmakademie Baden-Wuerttemberg,
Animationsinstitut R&D Labs in the scope of the EU funded project
MAX-R (101070072) and funding on the own behalf of Filmakademie Baden-Wuerttemberg.
Former EU projects Dreamspace (610005) and SAUCE (780470) have inspired the
TRACER FOUNDATION development.

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/

//! @file "ParameterObject.cs"
//! @brief Implementation of the VPET ParameterObject, collecting parameters and providing parameter update functionalities.
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 01.03.2022

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace tracer
{
    //!
    //! Implementation of the VPET ParameterObject, collecting parameters and providing parameter update functionalities.
    //!
    [System.Serializable]
    public class ParameterObject : MonoBehaviour
    {
        //!
        //! The global id counter for generating unique parameterObject IDs.
        //! start at 255, so the player id (range 1-255) will not interfere
        //!
        private static short s_id = 1; //255;  //was 1
        //!
        //! The unique ID of this parameter object.
        //!
        protected short _id = 0;
        //!
        //! The unique ID of this parameter object.
        //!
        protected byte _sceneID = 254;
        //!
        //! The unique ID of this parameter object.
        //!
        public short id
        {
            get => _id;
        }
        public byte sceneID
        {
            get => _sceneID;
        }
        //!
        //! A reference to the vpet core.
        //!
        static protected Core _core = null;
        public static Core core
        {
            get => _core;
        }
        //!
        //! Event emitted when parameter changed.
        //!
        public event EventHandler<AbstractParameter> hasChanged;
        //!
        //! List storing all parameters of this SceneObject.
        //!
        protected List<AbstractParameter> _parameterList;
        //!
        //! Getter for parameter list
        //!
        public ref List<AbstractParameter> parameterList
        {
            get => ref _parameterList;
        }
        //!
        //! Function that emits the parameter objects hasChanged event. (Used for parameter updates)
        //!
        //! @param parameter The parameter that has changed. 
        //!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void emitHasChanged(AbstractParameter parameter)
        {
            if (parameter._distribute)
                hasChanged?.Invoke(this, parameter);
        }
        //!
        //! Function that searches and returns a parameter of this parameter object based on a given name.
        //!
        //! @param name The name of the parameter to be returned.
        //!
        public Parameter<T> getParameter<T>(string name)
        {
            return (Parameter<T>)_parameterList.Find(parameter => parameter.name == name);
        }
        //!
        //! Factory to create a new ParameterObject and do it's initialisation.
        //! Use this function instead GameObject.AddComponen<>!
        //!
        //! @param gameObject The gameObject the new ParameterObject will be attached to.
        //! @sceneID The scene ID for the new ParameterObject.
        //!
        public static ParameterObject Attach(GameObject gameObject, byte sceneID = 254)
        {
            ParameterObject obj = gameObject.AddComponent<ParameterObject>();
            obj.Init(sceneID);

            return obj;
        }
        //!
        //! Initialisation
        //!
        protected void Init(byte sceneID)
        {
            _core.removeParameterObject(this);
            _sceneID = sceneID;
            _core.addParameterObject(this);
        }
        //!
        //! Initialisation
        //!
        public virtual void Awake()
        {
            if (_core == null)
                _core = GameObject.FindObjectOfType<Core>();

            _sceneID = 254;

            _id = getSoID();
            _parameterList = new List<AbstractParameter>();

            _core.addParameterObject(this);

            LogInitialisation(System.Reflection.MethodBase.GetCurrentMethod(), "sets _id: " + _id);
        }
        //!
        //! provide a unique id
        //! @return     unique id as int
        //!
        protected static short getSoID()
        {
            return s_id++;
        }

        //! if another object is spawned from another client, we need to update this var as well!
        protected static void increaseSoID(){ s_id++; }

        public static bool Output_Log = true;

        protected void LogInitialisation(System.Reflection.MethodBase functionWeCallFrom, string msg){
            if(Output_Log)
                Debug.Log(
                    "<color=#D96EBF>" +
                    functionWeCallFrom.ReflectedType.Name + "." +               //Class where we call the Method from   e.g. SceneObjectMino
                    functionWeCallFrom.Name + "@" + gameObject.name + "("+      //Method Name + GameObject Name         e.g. Init
                    this.GetType().Name + ")</color> " +                        //Class of the Script that calls it     e.g. MinoGlobalTrigger
                    msg
                );
        }

        //!
        //! skips the ids of the MinoPlayerCharacter, since the spectator does not have this
        //!
        public static void IncreaseSIDForSpectator(int offset){
            for(int x = 0; x<offset; x++)
                s_id++;
        }
    }
}
