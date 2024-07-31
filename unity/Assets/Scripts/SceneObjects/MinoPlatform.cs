using System;
using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoPlatform : MinoInteractable
{
    private Parameter<bool> m_isAvailable;

    private RPCParameter<bool> m_validPlatformTrigger;

    public MinoPlatformManager platformManager;
    public bool available = false;

    //[SerializeField] private Transform sceneRoot;

    public override void Awake()
    {
        base.Awake();

        m_isAvailable = new Parameter<bool>(false, "isAvailable", this);
        m_isAvailable.hasChanged += ChangeAvailable;

        m_validPlatformTrigger = new RPCParameter<bool>(false, "ValidPlatformTriggered", this);
        m_validPlatformTrigger.hasChanged += EmitChange;
        m_validPlatformTrigger.setCall(ValidPlatformTriggered);
    }

    protected override void Update()
    {
        base.Update();
        if (available != m_isAvailable.value)
        {
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isAvailable", "available: "+available));
            m_isAvailable.value = available;
        }
    }

    protected override void OnTriggerEnter(Collider other){
        if (m_triggerCollider != null && other.CompareTag("Head") && available){
            if(platformManager.IsMaster())
                platformManager.ChangeState();
            else{
                Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_validPlatformTrigger", "Network player triggered a platform"));        
                m_validPlatformTrigger.Call(true);
                if(_lock)
                    EmitDespiteLock(m_validPlatformTrigger);
                //m_validPlatformTrigger.setValue(true, true);
                //does not trigger because we are locked?
                //EmitDespiteLock(m_validPlatformTrigger);
            }
        }
    }

    private void EmitChange(object sender, bool b){
        emitHasChanged((AbstractParameter)sender);
    }

    //MinoPlatform will make a network call, so it will only on be executed on the PlatformManager - Master
    public void ValidPlatformTriggered(bool input){
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Network player triggered a platform"));
        if(platformManager.IsMaster()){
            platformManager.ChangeState();
        }
    }

    private void ChangeAvailable(object sender, bool input){
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "available: "+input));
        available = input;
        emitHasChanged((AbstractParameter)sender);
    }
    
}
