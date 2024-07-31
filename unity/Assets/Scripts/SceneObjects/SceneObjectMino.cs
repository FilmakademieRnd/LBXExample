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
using System.Runtime.CompilerServices;
using NaughtyAttributes;
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
            scale = new Parameter<Vector3>(tr.localScale, "scale", this);
            scale.hasChanged += updateScale;
            
            if(useThomasJitterFixHere && !jitterHotFixedUsed){
                InitJitterHotfix();
                position.hasChanged -= updatePosition;
            }

        }

        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public override void OnDestroy()
        {
            base.OnDestroy();

            if(jitterHotFixedUsed)
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
            if(!jitterHotFixedUsed){
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
                }//should automatically be called via our hasChangedFunction
                else{
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
        
        protected override void updateScale(object sender, Vector3 a)
        {
            
        }

        #region SIGGRAPH HOTFIXES

        public void ExecuteNeverBelowOrAboveParentCheck(){
            NeverBelowOrAboveParentPos(tr.position);
        }

        private Vector3 NeverBelowOrAboveParentPos(Vector3 pos){
            //ONLY FOR SCENEOBJECTMINO AND CHARACTERS!
            if(this.GetType() == typeof(MinoPlayerCharacter) && tr.parent != null && tr.parent.GetComponent<SceneObjectMino>()){
                float parentY = tr.parent.position.y;
                if(Mathf.Abs(parentY-pos.y) > 0.2f){
                    pos.y = parentY;
                }
            }
            return pos;
        }

        public void StartPlatformParentCheck(){
            usePlatformParentCheck = true;
            StartCoroutine(DoPlatformParentCheck());
        }
        public void StopPlatformParentCheck(){
            usePlatformParentCheck = false;
        }

        private bool usePlatformParentCheck = false;

        private IEnumerator DoPlatformParentCheck(){
            MinoPlatformManager mpf = Object.FindObjectOfType<MinoPlatformManager>();
            while(usePlatformParentCheck){
                mpf.ParentPlayersToNearestPlatform();
                yield return new WaitForSeconds(0.25f);
            }
        }
        #endregion

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
        public bool useThomasJitterFixHere = false; //for parenting and the discard message!

        protected Parameter<Vector4> currentNetworkData;    //Vector3 + sceneObjectID (if -1, the localPos is worldPos)

        private bool jitterHotFixedUsed = false;
        private Vector4 nonAllocVector4 = new Vector4();
        private Transform ourCurrentParentTr = null;
        private SceneObjectMino ourCurrentParentSOM = null;
        private short currentSOMID = -1;
        //reduce network send
        private Vector4 networkDataWeReceived;              //Vector3 + sceneObjectID (if -1, the localPos is worldPos)
        //do not update if not!
        private bool receivedDataOnce = false;

        private const bool DISCARD_POSITION_RUNAWAYS = false;
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
        protected void InitJitterHotfix(){
            if(jitterHotFixedUsed)
                return;
                
            if(MinoGameManager.SceneObjectsInitialized){
                tr.position = NeverBelowOrAboveParentPos(tr.position);
                CalculateNetworkData(tr);
                currentNetworkData = new Parameter<Vector4>(nonAllocVector4, "rootCharacterLocalPosAndParent", this);
                currentNetworkData.hasChanged += updateRootCharacterLocalPosAndParent;
                
                //JUST SO THAT LOCKED OBJECTS DONT START AT (0,0,0)
                networkDataWeReceived = tr.position;
                
                jitterHotFixedUsed = true;
            }else{
                StartCoroutine(WaitForSceneObjectInitialisation());
            }
        }

        private IEnumerator WaitForSceneObjectInitialisation(){
            while(!MinoGameManager.SceneObjectsInitialized)
                yield return null;
            InitJitterHotfix();
        }

        protected void RemoveJitterHotfix(){
            //From OnDestroyEvent
            if(jitterHotFixedUsed)
                currentNetworkData.hasChanged -= updateRootCharacterLocalPosAndParent;
        }

        //USE THE HOTFIX
        protected void EmitNonLockedPosData(){
            if(!jitterHotFixedUsed)    //ugly setup
                return;

            tr.position = NeverBelowOrAboveParentPos(tr.position);
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
            if(!jitterHotFixedUsed)
                return;

            //currentNetworkData.setValue(nonAllocVector4);
            emitHasChanged(currentNetworkData);
        }

        public Vector4 GetNetworkData(){ return nonAllocVector4; }

        //nec because we use GetComponentInParent in SetParentAndGetSOMID neuerdings
        public void CheckParentByTeleport(){
            if(!jitterHotFixedUsed)
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
