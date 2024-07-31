using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoButton : MinoInteractable
{
    public bool active = false;

    public UnityEvent localOnPressedEvent;
    public bool onlyExecuteOnMasterClient = true;
    public bool executeOnlyOnce = false;            //to test the jitter thingy, if we have a local coro for an elevator thingy to test
    private bool executedOnce = false;
    private bool isExecuting = false;

    public override void Awake()
    {
        base.Awake();
    }

    protected override void Update(){
        base.Update();

        if(active){
            if(!onlyExecuteOnMasterClient || !_lock){
                if(!executeOnlyOnce || !executedOnce){
                    executedOnce = true;
                    localOnPressedEvent?.Invoke();
                }
            }
        }
    }
    public void SetButtonPressed(){
        if (!_lock){
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
            m_isTriggered.Call(true, true);
        }

        Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));
        active = true;
    }

    public void SetButtonReleased(){
        if (!_lock){
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_isTriggered"));
            m_isTriggered.Call(false, true);
        }

        active = false;
        executedOnce = false;
        Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));
    }

    
    public override void IsTriggered(bool triggered){
        active = triggered;

        if(!active)
            executedOnce = false;
    }



    //******************* ONLY FOR TEST SCENE CHECKS
    public Transform transformToMoveOnClient;
    public void ClientEvent_MoveDefinedTransform(int verticalDir){
        transformToMoveOnClient.position += Vector3.up * verticalDir * Time.fixedDeltaTime;
    }

    public void ClientEvent_OnEachLocalOnce_MoveCoro(){
        StartCoroutine(OnEachClientLocalMoveCoro());
    }

    public void ClientEvent_OnMasterOngoing_MoveCoro(){
        StartCoroutine(OnMasterClientMoveCoro(true));
    }

    private IEnumerator OnEachClientLocalMoveCoro(bool repeat = false){    //just for testing, could be pressed more often -> buggy!
        //dirty
        isExecuting = true;
        SceneObject so = transformToMoveOnClient.GetComponent<SceneObject>();
        if(so)
            so.lockObjectLocal(true);

        bool executeAgain = true;
        while(executeAgain){
            Vector3 start = transformToMoveOnClient.position;
            Vector3 end = start + Vector3.up * 5f;
            float t = 0f;
            while(t<1f){
                t += Time.fixedDeltaTime/3f;
                transformToMoveOnClient.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while(t>0f){
                t -= Time.fixedDeltaTime/3f;
                transformToMoveOnClient.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
            executeAgain = repeat && (!so || so._lock);
            if(executeAgain)
                yield return new WaitForSeconds(1f);
            executeAgain = repeat && (!so || so._lock);
        }

        isExecuting = false;
        if(so)
            so.lockObjectLocal(false);
    }

    private IEnumerator OnMasterClientMoveCoro(bool repeat = false){    //just for testing, could be pressed more often -> buggy!
        //dirty
        isExecuting = true;
        SceneObject so = transformToMoveOnClient.GetComponent<SceneObject>();
        if(so)
            so.lockObject(true);

        bool executeAgain = true;
        while(executeAgain){
            Vector3 start = transformToMoveOnClient.position;
            Vector3 end = start + Vector3.up * 5f;
            float t = 0f;
            while(t<1f){
                t += Time.fixedDeltaTime/3f;
                transformToMoveOnClient.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while(t>0f){
                t -= Time.fixedDeltaTime/3f;
                transformToMoveOnClient.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
            executeAgain = repeat && (!so || !so._lock);
            if(executeAgain)
                yield return new WaitForSeconds(1f);
            executeAgain = repeat && (!so || !so._lock);
        }

        isExecuting = false;
        if(so)
            so.lockObject(false);
    }
    
}
