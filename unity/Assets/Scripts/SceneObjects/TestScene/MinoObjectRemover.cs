using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoObjectRemover : SceneObjectMino{
    
    public InteractableNetworkBehaviourEnum spawnNetworkBehaviour = InteractableNetworkBehaviourEnum.byMaster;

    [Header("SPAWN")]
    public GameObject removeParticle;
    private RPCParameter<int> removeId;


    public override void Awake(){
        base.Awake();

        removeId = new RPCParameter<int>(11, "spawnIndex", this);
        // removeId.hasChanged += EmitRemoveRPC;
        // removeId.setCall(ReceiveRemoveObject);
    }

    public void Event_RemoveID(int somid){
        if(id < 0)
            return;


        //ADJUST TO MinoSpawnablesManager and after that, use here !

        Debug.Log("Event::Remove");
        switch(spawnNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byEveryone:
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                Debug.Log("..Remove Here");
                removeId.setValue(somid);
                MinoGameManager.Instance.RemoveAndDeleteSOMID(somid);
                break;
            case InteractableNetworkBehaviourEnum.byMaster:
                if(MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){
                    //INITIATE SPAWN
                    Debug.Log("..Remove Here");
                    removeId.setValue(somid);
                    MinoGameManager.Instance.RemoveAndDeleteSOMID(somid);
                }else{
                    //TELL MASTER THAT WE SHOULD SPAWN!
                    Debug.Log("..Tell Master to Remove");
                    //spawnIndexAndId.setValue(new Vector2(id, -1));
                }
                break;
        }
        
        if(removeParticle)
            Destroy(Instantiate(removeParticle, transform.position, Quaternion.identity), 3f);
    }



    void OnTriggerEnter(Collider col){
        if(col.gameObject.GetComponentInParent<SceneObjectMino>())
            Event_RemoveID(col.gameObject.GetComponentInParent<SceneObjectMino>().id);
    }

    private void EmitSpawnRPC(object sender, Vector2 _spawnIndexAndId){
        emitHasChanged((AbstractParameter)sender);
    }

    private void ReceiveSpawnObject(int _somid){
        if(_somid < 0)
            return;

        switch(spawnNetworkBehaviour){
            // case InteractableNetworkBehaviourEnum.byEveryone:
            // case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
            //     Debug.Log("..Spawn Here from elsewhere");
            //     MinoGameManager.Instance.AddObjectAndInit_Receiver(spawnableUniqueObjects[(int)_spawnIndexAndId.x], (int)_spawnIndexAndId.y, GetSpawnPos());
            //     if(spawnParticle)
            //         Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
            //     break;
            // case InteractableNetworkBehaviourEnum.byMaster:
            //     if(_spawnIndexAndId.y < 0){ //just a msg that the Master should spawn!
            //         if(MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){
            //             //SPAWN
            //             Debug.Log("..Told to Spawn from elsewhere");
            //             Event_RemoveID((int)_spawnIndexAndId.x);
            //         }
            //     }else{
            //         Debug.Log("..Spawn Here from elsewhere");
            //         MinoGameManager.Instance.AddObjectAndInit_Receiver(spawnableUniqueObjects[(int)_spawnIndexAndId.x], (int)_spawnIndexAndId.y, GetSpawnPos());
            //         if(spawnParticle)
            //             Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
            //     }
            //     break;
        }
    }
}
