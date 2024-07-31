using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoGlobalEvent : MinoInteractable
{
    public UnityEvent triggerEvent;
    public bool isUsed = false;
    public bool reusable = false;

    public void TriggerEvent()
    {
        if(m_isTriggered == null){
            Debug.Log("MinoGlobalEvent m_isTriggered NOT INITIALIZED. WOULD THROW ERROR. MOST LIKELY OBJECT IS SWITCHED OFF");
            return;
        }
        //if (isUsed && !reusable) return;    //Reduce network load by not sending it if already triggered

        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
        
        //does this get triggered if it already is true? because of siggraph hack for calling repeatable, added !m_isTriggered.value instead true
        m_isTriggered.Call(!m_isTriggered.value, true);
    }

    public override void IsTriggered(bool triggered){
        
        if (isUsed) return;
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));
        triggerEvent.Invoke();
        if (reusable) return;
        isUsed = true;
    }

    //BALCONY JITTER FIX (PLAY ANIM ONLY LOCALLY)
    public void TriggerOnlyLocallyViaEvent(){
        triggerEvent.Invoke();
        if (reusable) return;
        isUsed = true;
    }
}