using tracer;
using UnityEngine;

public class MinoGrabbable : SceneObjectMino{

    [Header("DEBUG")]
    public bool debugCallGrab = false;
    public bool debugCallRelease = false;


    protected override void Update(){
        base.Update();

        if(debugCallGrab){
            debugCallGrab = false;
            Event_SetGrabbableGrab();
        }
        if(debugCallRelease){
            debugCallRelease = false;
            Event_SetGrabbableReleased();
        }
    }


    public void Event_SetGrabbableGrab(){
        lockObject(true);                                               //will also call AdjustPhysicsToLockState on other clients
        
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            rg.isKinematic = true;
        }

        //parent to our char and re-position
        transform.parent = MinoGameManager.Instance.GetPlayer().transform;
        transform.localPosition = new Vector3(1f, 1f, 1f);
    }
    public void Event_SetGrabbableReleased(){
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            rg.isKinematic = false;
        }
        transform.parent = null;
    }

    protected override void AdjustPhysicsToLockState(){
        foreach(Rigidbody rg in GetComponentsInChildren<Rigidbody>()){
            //set to kinematic if its locked on our side
            if(_lock){
                rg.angularVelocity = Vector3.zero;
                rg.velocity = Vector3.zero;
                rg.isKinematic = true;
            }
        }
    }


}