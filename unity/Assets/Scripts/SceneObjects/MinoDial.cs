using System.Collections;
using System.Collections.Generic;
using Autohand;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoDial : MinoInteractable
{
    public HingeJoint hinge;
    public Rigidbody rb;
    public bool usable = true;
    private bool isUsed;
    public UnityEvent triggerEvent;
    public SceneObjectMino trackedObject;
    [HideInInspector] public float hingeAngleOffset = 0;

    private int grabbingHands = 0;
       
    protected override void Update()
    {
        if (isUsed) return;
        if (!usable) return;
       
        if (hinge.angle >= hinge.limits.max + hingeAngleOffset || hinge.angle <= hinge.limits.min + hingeAngleOffset){
            Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "MinoDial activated"));
            isUsed = true;
            SetDialInPlace();
        }

    }

    public void SetDialInPlace(){
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
        m_isTriggered.Call(true, true);
        FreezeAllConstraints();

    }

    public override void IsTriggered( bool triggered){
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "invoke triggerEvent & FreezeAllConstraints"));
        triggerEvent.Invoke();
        FreezeAllConstraints();
    }

    private void FreezeAllConstraints(){
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void AddGrabValue(){
        grabbingHands += 1;
        CheckGrabValue();
    }

    public void RemoveGrabValue(){
        grabbingHands -= 1;
        CheckGrabValue();
    }

    private void CheckGrabValue()
    {
        if (grabbingHands > 0)
        {
            lockObject(true);
            _lock = false;
            trackedObject.lockObject(true);
            trackedObject._lock = false;
        }
        else
        {
            lockObject(false);
            _lock = false;
            trackedObject.lockObject(false);
            trackedObject._lock = false;
        }
        
    }
}
