using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoMoveable : MinoInteractable{

    public bool unarmedOnly = false;
    public UnityEvent startMoving;

    /*
    *   Local call, because Global-Events will be called: 
    *       - Elevator, TIMELINE_Chapter2B_Ariadne_2
    *   Network call, because OnTriggerEnter will only be called by Players, not by NetworkPlayers
    *       - Spiral, Star, TRIGGER_ARIADNE_2A_2
    */
    public bool callOnlyLocally = true;
    private bool isUsed = false;

    protected override void OnTriggerEnter(Collider other){
        if (MinoGameManager.Instance.IsSpectator())
            return;

        if ((MinoGameManager.Instance?.m_playerCharacter?.ghostModeOn == true)) 
            return;

        if (isUsed) 
            return;

        base.OnTriggerEnter(other);
        
        if (other.CompareTag("Head")) //|| other.CompareTag("NetworkHead"))
        {
            if (unarmedOnly && (MinoGameManager.Instance?.m_playerCharacter?.holdingWeapon ?? false))
                return;

            //Debug.Log("Number of known Players in the Manager: " + gameManager.numberPlayers);
            // Check if all players are inside.
            if (m_collidedPlayerAmount >= MinoGameManager.Instance.numberPlayers || MinoGameManager.Instance.numberPlayers == 1){
                if(callOnlyLocally){
                    Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "invoke startMoving"));
                    startMoving?.Invoke();
                    isUsed = true;
                }else{
                    Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
                    startMoving?.Invoke();
                    isUsed = true;
                    m_isTriggered.Call(true, true);
                }
            }
        }
    }

    public void SetUsed_ForGlobalEvent(){ isUsed = true; }  //SpiralTrigger functionality!

    public override void IsTriggered(bool triggered){
        if (isUsed) return;
        
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));
        
        startMoving?.Invoke();
        isUsed = true;
    } 
}
