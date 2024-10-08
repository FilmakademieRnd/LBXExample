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

//! @file "SceneObject.cs"
//! @brief Implementation of the VPET SceneObject, connecting Unity and VPET functionalty.
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 01.03.2022

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tracer
{
    //!
    //! Implementation of the VPET SceneObject, connecting Unity and VPET functionalty 
    //! around 3D scene specific objects.
    //!
    [DisallowMultipleComponent]
    public class SceneObjectMino : SceneObject{

        //NEW UID FOR SCENE INITIALISATION
        [HideInInspector] public string ourGUID = "";   //will be set via MinoGUICreator/CreateGUIDs within AssetMenu "Tools/Create GUIDs"

        public bool adjustPhysicsToLockState = true;    //if we are locked, switch of physics

        public enum InteractableNetworkBehaviourEnum{
            byEveryone = 0,             //never checks for its own lock
            byEveryone4Everyone = 3,    //should emit its signal and everyone should execute stuff locally (and do not emit any position etc)
            byMaster = 10               //only send updates if we are the master ( == we are not locked!)
        }

        private bool lockStateWas = true;

        //!
        //! Factory to create a new SceneObject and do it's initialisation.
        //! Use this function instead GameObject.AddComponen<>!
        //!
        //! @param gameObject The gameObject the new SceneObject will be attached to.
        //! @sceneID The scene ID for the new SceneObject.
        //!
        public static new SceneObjectMino Attach(GameObject gameObject, byte sceneID = 254)
        {
            SceneObjectMino obj = gameObject.AddComponent<SceneObjectMino>();
            obj.Init(sceneID);

            return obj;
        }
        //!
        //! Initialisation
        //!
        public override void Awake(){

            if (_core == null)
                _core = GameObject.FindObjectOfType<Core>();

            _sceneID = 254;
            
            _parameterList = new List<AbstractParameter>();
            tr = GetComponent<Transform>();

            lockStateWas = _lock;
            
            position = new Parameter<Vector3>(tr.position, "position", this);
            position.hasChanged += updatePosition;
            rotation = new Parameter<Quaternion>(tr.rotation, "rotation", this);
            rotation.hasChanged += updateRotation;

            InitToUseParentingPosUpdates();


            if(adjustPhysicsToLockState){
                onLockStateChanged.AddListener(AdjustPhysicsToLockState);
                //execute once at init!
                AdjustPhysicsToLockState();
            }
        }

        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public override void OnDestroy(){
            if(parentingInitialized)
                currentNetworkData.hasChanged -= updateRootCharacterLocalPosAndParent;
            

            rotation.hasChanged -= updateRotation;

            _core.removeParameterObject(this);
            _core.getManager<NetworkManager>().RemoveSceneObject(this);
        }

        public void Init(byte sID = 254, short oID = -1)
        {
            _sceneID = sID;

            if (oID == -1)
                _id = getSoID();
            else
                _id = oID;

            if(MinoGameManager.Instance.logInitCalls)
                LogInitialisation(System.Reflection.MethodBase.GetCurrentMethod(), "sets _id: " + _id);

            _core.addParameterObject(this);
            _core.getManager<NetworkManager>().AddSceneObject(this);
        }
        
        public override void Setup(byte sID = 254, short oID = -1)
        {
            _core.removeParameterObject(this);

            _sceneID = sID;

            if (oID == -1)
                _id = getSoID();
            else
                _id = oID;

            if(MinoGameManager.Instance.logInitCalls)
                LogInitialisation(System.Reflection.MethodBase.GetCurrentMethod(), "sets _id: " + _id);

            _core.addParameterObject(this);
        }

        protected override void Update(){
            if (!_lock){
                EmitNonLockedPosData();
            }else{
                ApplyNetworkPosDataOnLocked();
            }

            if(_lock != lockStateWas){              //cannot be called from SetLock and lockObject only gets called when called from the locking-client-object
                onLockStateChanged?.Invoke();       //e.g. also lock physics
                lockStateWas = _lock;
            }
        }

        protected virtual void AdjustPhysicsToLockState(){
            foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
                if(IsCharacter(rg.gameObject))
                    continue;
                if(_lock){
                    //Debug.Log("SET KINEMATIC");
                    rg.angularVelocity = Vector3.zero;
                    rg.velocity = Vector3.zero;
                    rg.isKinematic = true;
                }else{
                    //Debug.Log("SET PHYSICAL");
                    rg.isKinematic = false;
                }
            }
        }

        private bool IsCharacter(GameObject g){
            return g.GetComponentInParent<MinoCharacter>() != null;
        }

        #region NETWORK_PARENTING
        //called so we send local position if neccessary and otherwise our global position
        //its neccessary if we are a child of any object that inherits from SceneObjectMino (since it most likely can move)
        //we also send our parent updates so we have the same on every client
        //cant be any object, because we dont track "non sceneobjectmino" gameobject with an unique id

        [Header("Network Parenting")]
        protected Parameter<Vector4> currentNetworkData;    //Vector3 + sceneObjectID (if -1, the localPos is worldPos)

        private Vector4 nonAllocVector4 = new Vector4();
        private Transform ourCurrentParentTr = null;
        private SceneObjectMino ourCurrentParentSOM = null;
        private short currentSOMID = -1;
        //reduce network send
        private Vector4 networkDataWeReceived;              //Vector3 + sceneObjectID (if -1, the localPos is worldPos)
        //do not update if not!
        private bool receivedDataOnce = false;
        private bool parentingInitialized = false;

        private const bool  DISCARD_POSITION_RUNAWAYS = true;            
        private const float DISCARD_DISTANCE = 2f;                        //how big amplitudes are allowed to from the mean
        private const int   DISCARD_CHECKLIST_LENGTH = 10;                //how many valid positions should be kept for mean calculation
        private const int   IGNORE_DISCARDS_IF_MORE_THAN_X_IN_ROW = 10;   //if we get more than these discards in row without a sync, sync either way!

        private List<Vector3> discardCheckPositionList = new();
        private Vector3 discardPositionListSum;
        private int discardsInRow = 0;  

        protected void InitToUseParentingPosUpdates(){
            if(MinoGameManager.SceneObjectsInitialized){
                CalculateNetworkData(tr);
                currentNetworkData = new Parameter<Vector4>(nonAllocVector4, "rootCharacterLocalPosAndParent", this);
                currentNetworkData.hasChanged += updateRootCharacterLocalPosAndParent;
                
                //JUST SO THAT LOCKED OBJECTS DONT START AT (0,0,0)
                networkDataWeReceived = tr.position;
                parentingInitialized = true;
            }else{
                StartCoroutine(WaitForSceneObjectInitialisation());
            }
        }

        private IEnumerator WaitForSceneObjectInitialisation(){
            while(!MinoGameManager.SceneObjectsInitialized)
                yield return null;
            InitToUseParentingPosUpdates();
        }

        protected void updateRootCharacterLocalPosAndParent(object sender, Vector4 posAndID){
            receivedDataOnce = true;
            networkDataWeReceived = posAndID;
        }

        
        private void CalculateNetworkData(Transform _transform){
            if(ourCurrentParentTr != _transform.parent)         //IF WE HAVE A NEW PARENT
                SetParentAndGetSOMID();                         //GET CORRECT VALUES
            
            //VALID PARENT: SEND LOCAL POS, ELSE: SEND WORLD POS (valid only if it is a SceneObjectMino object)
            nonAllocVector4 = currentSOMID < 0 ? tr.position : ourCurrentParentTr.InverseTransformPoint(tr.position);
            nonAllocVector4.w = currentSOMID;                   //PACK ID INTO V4   (could be optimized into byte array?)
        }

        private void SetParentAndGetSOMID(){
            ourCurrentParentTr = tr.parent;
            ourCurrentParentSOM = ourCurrentParentTr?.GetComponentInParent<SceneObjectMino>();
            currentSOMID = MinoGameManager.Instance.GetIdViaSceneObject(ourCurrentParentSOM);
        }
   
        protected void EmitNonLockedPosData(){
            if(!parentingInitialized)
                return;

            CalculateNetworkData(tr);

            if (currentNetworkData.value != nonAllocVector4){
                currentNetworkData.setValue(nonAllocVector4, false);
                emitHasChanged(currentNetworkData);
            }
            
            if (tr.rotation != rotation.value){             //rotation is always globally
                rotation.setValue(tr.rotation, false);      //will be applied over the network
                emitHasChanged(rotation);
            }
        }

        public void EmitCurrentNetworkDataOnPlayerJoin(){   //so even if we have no updated values, send emit it, so the joining player gets the current pos+parent
            emitHasChanged(currentNetworkData);
        }


        protected void ApplyNetworkPosDataOnLocked(){
            if(!receivedDataOnce)
                return;

            ApplyParentUpdateOnLocked();

            if(currentSOMID < 0 || ourCurrentParentTr == null){
                tr.position = DiscardPositionUpdate(networkDataWeReceived);
            }else{
                tr.position = DiscardPositionUpdate(ourCurrentParentTr.TransformPoint(networkDataWeReceived));
            }
        }

        private void ApplyParentUpdateOnLocked(){
            if((int)networkDataWeReceived.w != currentSOMID){    //ID CHANGED
                currentSOMID = (short)networkDataWeReceived.w;
                //SET PARENT IF ITS A VALID TARGET, ELSE: null
                ourCurrentParentTr = currentSOMID < 0 ? null : MinoGameManager.Instance.GetSceneObjectViaID(currentSOMID)?.transform;
            }
            
            if(tr.parent != ourCurrentParentTr)
                tr.parent = ourCurrentParentTr;
        }

        private Vector3 DiscardPositionUpdate(Vector3 receivedTransformPos){
            if(!DISCARD_POSITION_RUNAWAYS)
                return receivedTransformPos;

            if(discardsInRow > IGNORE_DISCARDS_IF_MORE_THAN_X_IN_ROW){  //if too many are discarded, we may should use the update!
                discardCheckPositionList = new();
                discardsInRow = 0;
            }

            if(discardCheckPositionList.Count < DISCARD_CHECKLIST_LENGTH){
                discardCheckPositionList.Add(receivedTransformPos);
                return receivedTransformPos;
            }

            if(receivedTransformPos == discardCheckPositionList[^1])
                return receivedTransformPos;

            //check that ~mean~ is not too far away from our shown previous positions
            discardPositionListSum = Vector3.zero;
            foreach(Vector3 v in discardCheckPositionList)
                discardPositionListSum += v;
            discardPositionListSum /= DISCARD_CHECKLIST_LENGTH;
            if(Vector3.Distance(discardPositionListSum, receivedTransformPos) > DISCARD_DISTANCE){
                discardsInRow++;
                return discardCheckPositionList[^1];
            }
            discardsInRow = 0;
            discardCheckPositionList.Add(receivedTransformPos);
            discardCheckPositionList.RemoveAt(0);
            return receivedTransformPos;
        }       
        #endregion

    }
}
