/*
-------------------------------------------------------------------------------
VPET - Virtual Production Editing Tools
vpet.research.animationsinstitut.de
https://github.com/FilmakademieRnd/VPET
 
Copyright (c) 2022 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Lab
 
This project has been initiated in the scope of the EU funded project 
Dreamspace (http://dreamspaceproject.eu/) under grant agreement no 610005 2014-2016.
 
Post Dreamspace the project has been further developed on behalf of the 
research and development activities of Animationsinstitut.
 
In 2018 some features (Character Animation Interface and USD support) were
addressed in the scope of the EU funded project  SAUCE (https://www.sauceproject.eu/) 
under grant agreement no 780470, 2018-2022
 
VPET consists of 3 core components: VPET Unity Client, Scene Distribution and
Syncronisation Server. They are licensed under the following terms:
-------------------------------------------------------------------------------
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

        public bool adjustChildOnLockStateChanged = false;  //not nec if the object uses the "jitterhotfix"

        public bool adjustPhysicsToLockState = true;    //if we are locked, switch of physics

        public enum InteractableNetworkBehaviourEnum{
            byEveryone = 0,             //never checks for its own lock
            byEveryone4Everyone = 3,    //should emit its signal and everyone should execute stuff locally (and do not emit any position etc)
            byMaster = 10               //only send updates if we are the master ( == we are not locked!)
            
            //byMasterClient = 20 //checks for MasterClient == client with lowest [player]nr
        }

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
        public override void Awake()
        {
            // MINO: AnimatorOverrideController base awake
            ////////////////////////////////////////////////////////
            if (_core == null)
                _core = GameObject.FindObjectOfType<Core>();

            _sceneID = 254;
            //_id = getSoID();
            
            _parameterList = new List<AbstractParameter>();
            tr = GetComponent<Transform>();

            m_uiManager = _core.getManager<UIManager>();
            /////////////////////////////////////////////////////////

            
            position = new Parameter<Vector3>(tr.position, "position", this);
            position.hasChanged += updatePosition;
            rotation = new Parameter<Quaternion>(tr.rotation, "rotation", this);
            rotation.hasChanged += updateRotation;
            //scale = new Parameter<Vector3>(tr.localScale, "scale", this);
            //scale.hasChanged += updateScale;
            
            if(sendPosViaParent && !useParentingAndPosUpdates){
                InitToUseParentingPosUpdates();
                position.hasChanged -= updatePosition;
            }

            if(adjustChildOnLockStateChanged)
                onLockStateChanged.AddListener(AdjustChildrenLockState);
            if(adjustPhysicsToLockState){
                onLockStateChanged.AddListener(AdjustPhysicsToLockState);
                //execute once at init!
                AdjustPhysicsToLockState();
            }
        }

        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public override void OnDestroy()
        {
            base.OnDestroy();

            if(useParentingAndPosUpdates)
                currentNetworkData.hasChanged -= updateRootCharacterLocalPosAndParent;

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

            //LogInitialisation(System.Reflection.MethodBase.GetCurrentMethod(), "sets _id: " + _id);

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

            //this will most likely overwrite an _id we have given earlier via Init and would result in wrong InitPersistentSOM
            //LogInitialisation(System.Reflection.MethodBase.GetCurrentMethod(), "sets _id: " + _id);

            _core.addParameterObject(this);
        }

        protected override void Update(){
            if(!useParentingAndPosUpdates){
                if (!_lock){
                    if (tr.position != position.value){
                        position.setValue(tr.position, false);
                        emitHasChanged(position);
                    }
                    if (tr.rotation != rotation.value){
                        rotation.setValue(tr.rotation, false);
                        emitHasChanged(rotation);
                    }
                }
            }else{
                if (!_lock){
                    EmitNonLockedPosData();
                }else{
                    ApplyNetworkPosDataOnLocked();
                }
            }
        }
        
        
        protected override void updatePosition(object sender, Vector3 a){
            tr.position = a;
            emitHasChanged((AbstractParameter)sender);
        }

        protected override void updateRotation(object sender, Quaternion a)
        {
            tr.rotation = a;
            emitHasChanged((AbstractParameter)sender);
        }
        
        private void AdjustChildrenLockState(){ //only the client who gains master state, will call this on its children (will be send over network either way!)
            if(_lock)
                return;

            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>()){
                if(som != this && !IsCharacter(som.gameObject)) //dont change lock on characters or child objects they hold!
                    som.lockObject(true);
            }
        }

        protected virtual void AdjustPhysicsToLockState(){
            foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
                if(IsCharacter(rg.gameObject))
                    continue;
                if(_lock){
                    rg.angularVelocity = Vector3.zero;
                    rg.velocity = Vector3.zero;
                    rg.isKinematic = true;
                }else{
                    rg.isKinematic = false;
                }
            }
        }

        private bool IsCharacter(GameObject g){
            return g.GetComponentInParent<MinoCharacter>() != null;
        }

        #region NETWORK_PARENTING
        //kind of jitter hotfix, so we send local data if we ware parented to any sceneobjectmino

        /***********
         * How to use
         * Any subclass of SceneObjectMino that want to use this, should:
         *      - call InitJitterHotfix in the Awake                        (this will init the parameter and callbacks)
         *      - if you override the Update and dont call base.Update      (this prevents position and rotation updates)
         *      -- in Update: add CheckAndEmitLocalOrWorldPos               (take care of our pos and parent for all network objects, if not locked!)
         *      -- in Update: add ApplyPosDataWeReceived();                 (this will receive values once we are locked)
         *      -- in Update: add ReParentIfNeccessary(); in Update         (this will receive values once we are locked)
         *      -- or dont override or call base.Update
         *
         *  TODO:
         *      - add for rotation as well (but ever only check for parenting in one of those calls)
         *
         ***********/

        [Header("Network Parenting")]
        public bool sendPosViaParent = false; //for parenting and the discard message!

        protected Parameter<Vector4> currentNetworkData;    //Vector3 + sceneObjectID (if -1, the localPos is worldPos)

        private bool useParentingAndPosUpdates = false;
        private Vector4 nonAllocVector4 = new Vector4();
        private Transform ourCurrentParentTr = null;
        private SceneObjectMino ourCurrentParentSOM = null;
        private short currentSOMID = -1;
        //reduce network send
        private Vector4 networkDataWeReceived;              //Vector3 + sceneObjectID (if -1, the localPos is worldPos)
        //do not update if not!
        private bool receivedDataOnce = false;

        private const bool DISCARD_POSITION_RUNAWAYS = true;
        private const float DISCARD_DISTANCE = 3f;
        private const int DISCARD_CHECKLIST_LENGTH = 10;
        private const int IGNORE_DISCARDS_IF_MORE_THAN_X_IN_ROW = 10;   //if we get more than these discards in row without a sync, than sync either way!

        private List<Vector3> discardCheckPositionList = new();
        private Vector3 discardPositionListSum;
        private int discardsInRow = 0;  

        private void SetParentAndGetSOMID(){
            ourCurrentParentTr = tr.parent;
            ourCurrentParentSOM = ourCurrentParentTr?.GetComponentInParent<SceneObjectMino>();
            currentSOMID = MinoGameManager.Instance.GetIdViaSceneObject(ourCurrentParentSOM);
        }

        //Calculate our position and ID for network transfer
        private void CalculateNetworkData(Transform _transform){
            if(ourCurrentParentTr != _transform.parent)         //IF WE HAVE A NEW PARENT
                SetParentAndGetSOMID();                         //GET CORRECT VALUES
            
            //VALID PARENT: SEND LOCAL POS, ELSE: SEND WORLD POS (valid only if it is a SceneObjectMino object)
            nonAllocVector4 = currentSOMID < 0 ? tr.position : ourCurrentParentTr.InverseTransformPoint(tr.position);
            nonAllocVector4.w = currentSOMID;                   //PACK ID INTO V4   (could be optimized into byte array?)
        }

        protected void updateRootCharacterLocalPosAndParent(object sender, Vector4 posAndID){
            receivedDataOnce = true;
            networkDataWeReceived = posAndID;      //UPDATE THE POS
            //only emit via script
            //emitHasChanged((AbstractParameter)sender);
        }

        public void UpdateNetworkDataOnPlayerOnJoin(Vector4 posAndID){
            Debug.Log("<<<<<<<<<<<<<< RECEIVED ROOT UPDATE: "+posAndID);
            short somId = (short)posAndID.w;
            Transform parentFromData = somId < 0 ? null : MinoGameManager.Instance.GetSceneObjectViaID(somId)?.transform;
            if(parentFromData != null && getTr.parent != parentFromData){
                getTr.parent = parentFromData;
            }

            currentSOMID = somId;
            Vector3 pos = (Vector3)posAndID;

            if(currentSOMID < 0 || ourCurrentParentTr == null){
                tr.position = DiscardPositionUpdate(pos);
            }else{
                tr.position = DiscardPositionUpdate(ourCurrentParentTr.TransformPoint(pos));
            }
        }

        //INIT THE HOTFIX
        protected void InitToUseParentingPosUpdates(){
            if(useParentingAndPosUpdates)
                return;
                
            if(MinoGameManager.SceneObjectsInitialized){
                CalculateNetworkData(tr);
                currentNetworkData = new Parameter<Vector4>(nonAllocVector4, "rootCharacterLocalPosAndParent", this);
                currentNetworkData.hasChanged += updateRootCharacterLocalPosAndParent;
                
                //JUST SO THAT LOCKED OBJECTS DONT START AT (0,0,0)
                networkDataWeReceived = tr.position;
                
                useParentingAndPosUpdates = true;
            }else{
                StartCoroutine(WaitForSceneObjectInitialisation());
            }
        }

        private IEnumerator WaitForSceneObjectInitialisation(){
            while(!MinoGameManager.SceneObjectsInitialized)
                yield return null;
            InitToUseParentingPosUpdates();
        }

        protected void RemoveJitterHotfix(){
            //From OnDestroyEvent
            if(useParentingAndPosUpdates)
                currentNetworkData.hasChanged -= updateRootCharacterLocalPosAndParent;
        }

        //USE THE HOTFIX
        protected void EmitNonLockedPosData(){
            if(!useParentingAndPosUpdates)    //ugly setup
                return;

            CalculateNetworkData(tr);

            if (currentNetworkData.value != nonAllocVector4){
                currentNetworkData.setValue(nonAllocVector4, false);
                //actually this will right now only send an update when parent changes, but not when we do any movement
                //especially this will be different on the chars, since the root barely moves (despite when we are in the eleveator/baclony)

                //Debug.Log(">>>>>>>>> SEND NETWORK DATA: "+currentNetworkData.ToString());
                emitHasChanged(currentNetworkData);
            }
            
            if (tr.rotation != rotation.value){
                rotation.setValue(tr.rotation, false);
                emitHasChanged(rotation);
            }
        }

        public void EmitCurrentNetworkDataOnPlayerJoin(){
            if(!useParentingAndPosUpdates)
                return;

            //currentNetworkData.setValue(nonAllocVector4);
            emitHasChanged(currentNetworkData);
        }

        public Vector4 GetNetworkData(){ return nonAllocVector4; }

        //nec because we use GetComponentInParent in SetParentAndGetSOMID neuerdings
        public void CheckParentByTeleport(){
            if(!useParentingAndPosUpdates)
                return;

            //when we teleport the platform, our parent does not explicitely change, but maybe their parent SOM
            SceneObjectMino somStillAvailable = ourCurrentParentTr?.GetComponentInParent<SceneObjectMino>();
            if(somStillAvailable != ourCurrentParentSOM){
                ourCurrentParentSOM = somStillAvailable;
                currentSOMID = -1;
                CalculateNetworkData(tr.parent);
            }
        }

        #region ForLockedObjects
        //Called in the MinoNetworkCharacter, since they will NEVER send any values and that should always be called from All objects that are locked
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

        private Vector3 DiscardPositionUpdate(Vector3 receivedTransformPos){
            if(!DISCARD_POSITION_RUNAWAYS)
                return receivedTransformPos;
            
            if(discardsInRow > IGNORE_DISCARDS_IF_MORE_THAN_X_IN_ROW){
                discardCheckPositionList = new();
                discardsInRow = 0;
            }

            if(discardCheckPositionList.Count < DISCARD_CHECKLIST_LENGTH){
                discardCheckPositionList.Add(receivedTransformPos);
                return receivedTransformPos;
            }

            //check that ~mean~ is not too far away from our shown previous positions
            discardPositionListSum = Vector3.zero;
            foreach(Vector3 v in discardCheckPositionList)
                discardPositionListSum += v;
            discardPositionListSum /= DISCARD_CHECKLIST_LENGTH;
            if(Vector3.Distance(discardPositionListSum, receivedTransformPos) > DISCARD_DISTANCE){
                //DISCARD!
                discardsInRow++;
                return discardCheckPositionList[^1];
            }
            discardsInRow = 0;
            discardCheckPositionList.Add(receivedTransformPos);
            discardCheckPositionList.RemoveAt(0);
            return receivedTransformPos;
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
        #endregion

        /***********
         *
         * Example
         * The Character will update its head and hand position in local space
         * It will call CheckAndEmitLocalOrWorldPos in its Update,
         * but never calls ApplyPosDataWeReceived nor ReParentIfNeccessary, since it will never receive pos data - only sends
         *
         *      void override Update(){
         *          if(!_lock)
         *              CheckAndEmitLocalOrWorldPos()
         *          else{
         *              ApplyPosDataWeReceived();
         *              ReParentIfNeccessary();
         *          }
         *      }
         *
         * TODO: add bool var at top, take care of how and what we handle, no more override of Update, but allow another virtual function others can use
         *       VirtualPreUpdate and VirtualAfterUpdate
         *
         ***********/

        #endregion

    }
}
