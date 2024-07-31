using UnityEngine;
using UnityEngine.Events;
using tracer;

public class MinoLocalTrigger : MinoInteractable    //made interactable so it can be send to specator
{
    public UnityEvent triggerEvent;
    public bool isUsed = false;
    public bool sendToSpectator = true;
    public bool repeatable = false;

    protected override void OnTriggerEnter(Collider other){
        if (!isUsed){
            if (other.CompareTag("Head")){
                if (MinoGameManager.Instance?.m_playerCharacter?.ghostModeOn == false)
                {
                    isUsed = !repeatable;
                    triggerEvent?.Invoke();
                    m_isTriggered.Call(true,true);
                    Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered", "Call only for Specator"));
                }
            }
        }
    }

    public override void IsTriggered(bool triggered){
        if (isUsed || !MinoGameManager.Instance.IsSpectator()) 
            return;
        
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Call only for Specator"));
        
        isUsed = !repeatable;
        triggerEvent?.Invoke();
    }  
}
