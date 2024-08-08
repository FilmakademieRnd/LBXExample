using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tracer;

public class SceneObjectLocking : MonoBehaviour{

    public TextMesh debugLockText;

    private bool isMasterCalled = false;

    void Start(){
        //START LOCKED (despite we are the Master - can happen on later spawned objects!)
        if(!MinoGameManager.Instance.AreWeMaster()){
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObjectLocal(true);    
                //use the lockObject so we call the correct physical behaviours due to locking
                //som.lockObject(false);
                //... but its not ready at game start...
        }else{
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObject(true);//som.lockObjectLocal(false); //we do not call lockObject(true), because it can be that we are a spawned object and that would result in an error

            isMasterCalled = true;
        }

        if(debugLockText)
            debugLockText.text = "isMaster/unlocked \n"+isMasterCalled;
        MinoGameManager.Instance.onBecameMasterClient.AddListener(BecameMaster);

        InvokeRepeating("UpdateIfWeLostMaster", 5f, 2f);
    }

    public void BecameMaster(){
        if(!isMasterCalled){
            //we are locked, gain lock over everyone else
            isMasterCalled = true;
            if(debugLockText)
                debugLockText.text = "isMaster/unlocked\n"+isMasterCalled;
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObject(true);
        }
    }

    public void UpdateIfWeLostMaster(){
        isMasterCalled = !GetComponentInChildren<SceneObjectMino>().IsLocked();  //check if one object is still not locked here
        if(debugLockText)
            debugLockText.text = "isMaster/unlocked\n"+isMasterCalled;
    }
}
