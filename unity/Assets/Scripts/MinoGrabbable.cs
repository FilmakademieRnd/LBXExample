using System;
using Autohand;
using tracer;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Grabbable))]
public class MinoGrabbable : SceneObjectMino{

    [Header("EVENTS")]
    // [Tooltip("OnBeforeGrabEvent, otherwise it seems we cannot grab the object")]
    // public bool makePhysicalBeforeGrab = true; 
    [Tooltip("Call LastGrabbedBy, lockObject, set isKinematic")]
    public bool addOnGrabEvent = true;
    [Tooltip("Reset LastGrabbedBy, set isKinematic to false")]
    public bool addOnReleaseEvent = true;

    [Header("DEBUG")]
    public bool debugCallGrab = false;
    public bool debugCallRelease = false;
    private Grabbable grabbable;
    private int lastGrabbedBy = -1;
    private RPCParameter<int> m_lastGrabbedBy;

    public override void Awake(){
        
        grabbable = GetComponent<Grabbable>();

        base.Awake();

        //if(makePhysicalBeforeGrab)  grabbable.OnGrabEvent += Event_SetPhysicalBeforeGrab;
        if(addOnGrabEvent)          grabbable.OnGrabEvent += Event_SetGrabbableGrab;
        if(addOnReleaseEvent)       grabbable.OnReleaseEvent += Event_SetGrabbableReleased;

        m_lastGrabbedBy = new RPCParameter<int>(-1, "lastGrabbedBy", this);
        m_lastGrabbedBy.hasChanged += UpdateRPC;
        m_lastGrabbedBy.setCall(UpdateLastGrabbedBy);
    }

    #region last grabbed by
    public override void OnDestroy(){
        m_lastGrabbedBy.hasChanged -= UpdateRPC;
    }
    private void UpdateRPC(object sender, int c){
        emitHasChanged((AbstractParameter)sender);
    }
    private void UpdateLastGrabbedBy(int e){
        lastGrabbedBy = e;
    }
    
    private void SetLastGrabbedBy(){
        if(!MinoGameManager.Instance.m_playerCharacter)
            return;
            
        int localPlayerId = MinoGameManager.Instance.m_playerCharacter.id;
        m_lastGrabbedBy.Call(localPlayerId,true);
    }
    public int GetLastGrabbedBy(){ return lastGrabbedBy; }

    #endregion


    protected override void Update(){
        base.Update();

        if(debugCallGrab){
            debugCallGrab = false;
            foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
                rg.isKinematic = false;
            }
            MinoGameManager.Instance.m_playerCharacter.GetComponentInChildren<Hand>().TryGrab(grabbable);
        }
        if(debugCallRelease){
            debugCallRelease = false;
            foreach(Hand h in MinoGameManager.Instance.m_playerCharacter.GetComponentsInChildren<Hand>()){
                h.Release();
            }
        }
    }


    /*public void Event_SetPhysicalBeforeGrab(Hand h, Grabbable g){
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            rg.isKinematic = false;
        }
    }*/

    public void Event_SetGrabbableGrab(Hand h, Grabbable g){
        SetLastGrabbedBy();

        lockObject(true);                                               //will also call AdjustPhysicsToLockState on other clients
        
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){  //make it kinematic on our side
            rg.isKinematic = true;
        }
    }
    public void Event_SetGrabbableReleased(Hand h, Grabbable g){   //dont get called via ForceHandsRelease (we call that if another snitched our grabable)
        m_lastGrabbedBy.Call(-1, true);
        //set to physical
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            rg.isKinematic = false;
        }
    }

    protected override void AdjustPhysicsToLockState(){
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            //set to kinematic if its locked on our side
            if(_lock){
                rg.angularVelocity = Vector3.zero;
                rg.velocity = Vector3.zero;
                rg.isKinematic = true;
                grabbable.ForceHandsRelease();
            }
        }
        if(!_lock)
            SetLastGrabbedBy();
    }


}
