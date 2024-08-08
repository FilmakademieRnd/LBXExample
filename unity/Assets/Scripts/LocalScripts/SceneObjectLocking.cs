using UnityEngine;
using tracer;

public class SceneObjectLocking : MonoBehaviour{

    public TextMesh debugLockText;
    public bool useMaterialColorToShow = false;

    private SceneObjectMino som;
    private bool isMaster = false;
    private MeshRenderer mr;

    void Start(){
        //START LOCKED (despite we are the Master - can happen on later spawned objects!)
        if(!MinoGameManager.Instance.AreWeMaster()){
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObjectLocal(true);    
        }else{
            foreach(SceneObjectMino som in GetComponentsInChildren<SceneObjectMino>())
                som.lockObject(true);

            isMaster = true;
        }

        MinoGameManager.Instance.onBecameMasterClient.AddListener(BecameMaster);

        InvokeRepeating("CheckForMaster", 0, 1f);

        mr = GetComponentInChildren<MeshRenderer>();
        som = GetComponentInChildren<SceneObjectMino>();
    }

    public void BecameMaster(){
        if(!isMaster){
            isMaster = true;
            som.lockObject(true);
            CheckForMaster();
        }
    }

    public void CheckForMaster(){
        isMaster = !som.IsLocked();  //check if one object is still not locked here
        if(debugLockText)
            debugLockText.text = "isMaster/unlocked\n"+isMaster;
        if(useMaterialColorToShow){
            if(mr)
                mr.material.color = isMaster ? Color.green : Color.red;
        }
    }
}
