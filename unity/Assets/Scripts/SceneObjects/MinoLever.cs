using System.Collections;
using System.Collections.Generic;
using Autohand;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoLever : MinoInteractable
{
    public bool isPlaced = false;
    private HingeJoint hinge;
    public float triggerValue = 30;
    
    [SerializeField] private bool isUsed = false;
    public UnityEvent triggerEvent;
    private SceneObjectMino grabbableSOM; 
    private bool calledLockOnOthers = false;

    private RPCParameter<int> m_placedObjectIndex;

    #region Place Object Into Lever
    //require PlacePoint script anywhere here or on child
    [System.Serializable]
    public class PlacePointObjectClass{
        public bool allow = true;
        public Grabbable grabbableToPlace;
        public bool useParent = false;
        public Transform transformToMatch;

        private bool inited = false;
        private Transform grabbableToPlaceTr;

        //public UnityEvent additionalEventsToCall;   //e.g. exstinguish torch? specific audio for what is placed

        public void Setup(){
            if(inited)
                return;
            inited = true;
            grabbableToPlaceTr = grabbableToPlace.transform;
        }

        public void SetGrabbableToMatch(Transform parentTo){
            if(!grabbableToPlaceTr) //if that object somehow got destroyed (we should never destroy objects)
                return;

            if(!useParent){
                grabbableToPlaceTr.position = transformToMatch.transform.position;
                grabbableToPlaceTr.rotation = transformToMatch.transform.rotation;
                grabbableToPlaceTr.parent   = parentTo;

                if(parentTo.GetComponent<Grabbable>()){
                    GrabbableChild gChild = grabbableToPlaceTr.gameObject.AddComponent<GrabbableChild>();
                    gChild.grabParent = parentTo.GetComponent<Grabbable>();
                }
            }else{
                grabbableToPlaceTr.parent.position = transformToMatch.transform.position;
                grabbableToPlaceTr.parent.rotation = transformToMatch.transform.rotation;
                grabbableToPlaceTr.parent.parent   = parentTo;

                //TORCH PREFAB SETUP IS WEIRD 
                Destroy(grabbableToPlaceTr.parent.GetComponent<GrabbableChild>());

                if(parentTo.GetComponent<Grabbable>()){
                    GrabbableChild gChild = grabbableToPlaceTr.parent.gameObject.AddComponent<GrabbableChild>();
                    gChild.grabParent = parentTo.GetComponent<Grabbable>();
                }
            }
        }

        public void RemovePhysicBehaviour(){
            //IF WE HAVE A HINGE JOINT, DO DIFFERENTLY
            if(grabbableToPlace.GetComponent<HingeJoint>()){
                Destroy(grabbableToPlace.GetComponent<HingeJoint>());
                Destroy(grabbableToPlace.GetComponentInChildren<MinoWeapon>());
                foreach(Rigidbody rg in grabbableToPlace.GetComponentsInChildren<Rigidbody>())
                    Destroy(rg);
                Destroy(grabbableToPlace.GetComponent<MinoGrabbable>());
                Destroy(grabbableToPlace.GetComponent<GrabbablePose>());
                Destroy(grabbableToPlace.GetComponent<Grabbable>());
            }else{
                //REMOVE EVERYTHING DESPITE THE VIZ
                foreach(Component c in grabbableToPlace.GetComponents<Component>()){
                    if(
                        c.GetType() != typeof(Transform) &&
                        c.GetType() != typeof(MeshRenderer) &&
                        c.GetType() != typeof(MeshFilter) &&
                        c.GetType() != typeof(CapsuleCollider) &&
                        c.GetType() != typeof(GrabbableChild)
                    ){
                        Destroy(c);
                            
                    }
                }
                if(useParent){
                    foreach(Component c in grabbableToPlace.transform.parent.GetComponents<Component>()){
                        if(
                            c.GetType() != typeof(Transform) &&
                            c.GetType() != typeof(MeshRenderer) &&
                            c.GetType() != typeof(MeshFilter) &&
                            c.GetType() != typeof(CapsuleCollider) &&
                            c.GetType() != typeof(GrabbableChild) &&
                            c.GetType() != typeof(Animator)   //TORCH
                        ){
                            Destroy(c);
                                
                        }
                    }
                }
            }
            MinoGameManager.Instance.RemoveEmptySOMIDS();
            //Destroy(grabbableToPlace.GetComponent<Rigidbody>());
        }
    }
    public List<PlacePointObjectClass> objectsToPlaceInPlacePoint = new List<PlacePointObjectClass>();
    [Tooltip("Execute this event from player that places a valid object in 'PlacePoint'")]
    public MinoGlobalEvent globalEventOnPlacePoint;

    public override void OnDestroy(){
        base.OnDestroy();
        m_placedObjectIndex.hasChanged -= PlacedObjectHasChanged;
        GetComponentInChildren<PlacePoint>().OnPlaceEvent -= ObjectPlaced;
    }

    public void ObjectPlaced(PlacePoint pp, Grabbable placedGrabbable){
        //allow this event only to trigger via the grabbable that is not locked!
        SceneObjectMino som = placedGrabbable.GetComponentInChildren<SceneObjectMino>();
        if(som && som._lock){
            Debug.Log("object thats placed is locked on our side, we do not trigger the global event");
            return;
        }

        //lock lever AND grab child on all others, since we triggered it
        lockObject(true);
        grabbableSOM.lockObject(true);

        //call on network
        int index = 0;
        foreach(PlacePointObjectClass ppoc in objectsToPlaceInPlacePoint){
            if(ppoc.grabbableToPlace == placedGrabbable){
                m_placedObjectIndex.Call(index, true);
                globalEventOnPlacePoint?.TriggerEvent();
                return;
            }
            index++;
        }
        
    }
    private void ObjectWasPlaced(int index){
        //PLACE OBJECT
        if(objectsToPlaceInPlacePoint == null || index >= objectsToPlaceInPlacePoint.Count)
            return;

        objectsToPlaceInPlacePoint[index].RemovePhysicBehaviour();
        objectsToPlaceInPlacePoint[index].SetGrabbableToMatch(grabbableSOM.transform);

        #if UNITY_EDITOR
        if( UnityEditor.Selection.activeTransform == objectsToPlaceInPlacePoint[index].grabbableToPlace.transform ||
            UnityEditor.Selection.activeTransform == objectsToPlaceInPlacePoint[index].grabbableToPlace.transform.parent){

            UnityEditor.Selection.activeTransform = null;
        }
        #endif

        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Placed Grabbable at index: "+index));
    }

    private void PlacedObjectHasChanged(object sender, int id){
        emitHasChanged((AbstractParameter)sender);
    }
    #endregion

    public override void Awake(){
        base.Awake();

        hinge = GetComponentInChildren<HingeJoint>();
        grabbableSOM = hinge.GetComponent<SceneObjectMino>();

        m_placedObjectIndex = new RPCParameter<int>(0, "m_placedObjectIndex", this);
        m_placedObjectIndex.hasChanged += PlacedObjectHasChanged;
        m_placedObjectIndex.setCall(ObjectWasPlaced);

        //allow our grabbables in PlacePoint
        PlacePoint pp = GetComponentInChildren<PlacePoint>();
        foreach(PlacePointObjectClass ppoc in objectsToPlaceInPlacePoint){
            ppoc.Setup();
            if(!ppoc.allow)
                continue;

            if(!pp.onlyAllows.Contains(ppoc.grabbableToPlace)){
                pp.onlyAllows.Add(ppoc.grabbableToPlace);
            }
        }
        pp.OnPlaceEvent += ObjectPlaced;
    }

    public void SetPlaced(bool value){
        isPlaced = value;
    }
    
    //WILL BE UPDATED VIA ROTATION FROM NETWORK
    //SO THIS WILL BE CALLED LOCALLY ON EVERY CLIENT
    //__BUT__ THIS triggerEvent HAS GLOBAL EVENTS AND THAT MUST NOT BE CALLED MULTIPLE TIMES
    //HOTFIX: ONLY EXECUTE THE UPDATE CHECK, IF WE ARE GRABBING THE LEVER
    protected override void Update(){
        if (!isPlaced || grabbableSOM == null)
            return;
            
        /* IS SET UP IN CHILD GRABABLE ON EVENTS!
        if(grabbableSOM._lock){                 //if another player is grabbing the lever
            if(!isUsed && calledLockOnOthers){  //if we did trigger the function call and we did lock it earlier > unlock it again
                calledLockOnOthers = false;
                lockObject(false);
                Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Called lockObject(false)"));
            }
        }else{                                  //we are grabbing the lever
            if(!calledLockOnOthers){            //if we did not lock it yet, lock it on network
                calledLockOnOthers = true;
                lockObject(true);
                Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Called lockObject(true)"));
            }
        }*/

        if(grabbableSOM._lock || isUsed || _lock) return;

        if (hinge.angle >= triggerValue && isPlaced){
            Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Lever Rotated enough"));
            #if UNITY_EDITOR
            //if we rotate the wheel in the editor, this could happen
            if(!calledLockOnOthers){
                calledLockOnOthers = true;
                lockObject(true);
            }
            #endif
            TriggerDialEvent();
        }
    }



    //CALLED ONLY LOCALLY (like the GlobalEventOption 'callOnlyLocally')
    private void TriggerDialEvent(){
        if(isUsed)
            return;

        Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "invoke triggerEvent"));
        isUsed = true;
        
        triggerEvent.Invoke();
        MinoGameManager.Instance.RedoGlobalEvent(triggerEvent, false);
    }

    public override void IsTriggered(bool isTriggered){
    }
}
