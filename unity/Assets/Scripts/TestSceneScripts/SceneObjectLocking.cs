using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tracer;

public class SceneObjectLocking : MonoBehaviour{

    public TextMesh debugLockText;

    private bool isMasterCalled = false;

    void Start(){
        //START LOCKED
        foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
            /*som.lockObject(true);*/som.lockObjectLocal(true);

        if(debugLockText)
            debugLockText.text = "isMaster (not locked here) = "+isMasterCalled;
        MinoGameManager.Instance.onBecameMasterClient.AddListener(BecameMaster);

        InvokeRepeating("UpdateIfWeLostMaster", 5f, 2f);
    }

    public void BecameMaster(){
        if(!isMasterCalled){
            //we are locked, gain lock over everyone else
            isMasterCalled = true;
            if(debugLockText)
                debugLockText.text = "isMaster (not locked here) = "+isMasterCalled;
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObject(true);
        }
    }

    public void UpdateIfWeLostMaster(){
        isMasterCalled = !GetComponentInChildren<SceneObjectMino>()._lock;  //check if one object is still not locked here
        if(debugLockText)
            debugLockText.text = "isMaster (not locked here) = "+isMasterCalled;
    }
}
