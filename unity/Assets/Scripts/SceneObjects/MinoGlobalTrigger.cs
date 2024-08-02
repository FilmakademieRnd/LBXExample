using System;
using System.Collections;
using System.Collections.Generic;
using tracer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MinoGlobalTrigger : MinoInteractable
{
    public UnityEvent triggerEvent;

    public bool shouldResetGrababbleToOrigin = false;
    public bool isUsed = false;
    public bool reusable = false;

    protected override void OnTriggerEnter(Collider other){
        if (!isUsed){
            if (other.CompareTag("Head")){
                if (MinoGameManager.Instance?.m_playerCharacter?.ghostModeOn == false){
                    m_isTriggered.Call(true,true);
                    //Debug.Log("<color=green>[MinoGlobalTrigger] OnTriggerEnter local on "+gameObject.name+"</color>");
                    Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
                }
            }
        }

        if (!reusable) return;

        // if (shouldResetGrababbleToOrigin && other.GetComponent<MinoGrabbable>()){
        //     other.GetComponent<MinoGrabbable>().ResetToOrigin(); 
        // }
    }

    public override void IsTriggered(bool triggered){
        
        if (isUsed) return;
        
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Call only for Specator"));
        triggerEvent.Invoke();
        
        if(!reusable)
            isUsed = true;
    }   
    
    

}
