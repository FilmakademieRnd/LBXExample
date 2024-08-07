using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoObjectRemover : SceneObjectMino{
    
    public InteractableNetworkBehaviourEnum spawnNetworkBehaviour = InteractableNetworkBehaviourEnum.byMaster;

    [Header("REMOVE")]
    public GameObject removeParticle;
    private RPCParameter<int> removeId;


    public override void Awake(){
        base.Awake();

        removeId = new RPCParameter<int>(11, "removeIndex", this);
        removeId.hasChanged += EmitRemoveRPC;
        removeId.setCall(ReceiveRemoveObject);
    }

    public void Event_RemoveID(int somid){
        if(somid < 0)
            return;

        Debug.Log("Event::Remove");

        Vector3 removedAtPos = tr.position;
        switch(spawnNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byEveryone:
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                Debug.Log("..Remove Here");
                removeId.Call(somid, false);
                removedAtPos = MinoGameManager.Instance.RemoveAndDeleteSOMID(somid);
                break;
            case InteractableNetworkBehaviourEnum.byMaster:
                if(MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){
                    Debug.Log("..Remove Here at Master");
                    removeId.Call(somid, false);
                    removedAtPos = MinoGameManager.Instance.RemoveAndDeleteSOMID(somid);
                }else{
                    //should tell master that he removes it
                }
                break;
        }
        
        if(removeParticle)
            Destroy(Instantiate(removeParticle, removedAtPos, Quaternion.identity), 3f);
    }



    void OnTriggerEnter(Collider col){
        //DONT DESTROY CHARACTERS
        if(col.gameObject.GetComponentInParent<MinoCharacter>())
            return;
            
        if(col.gameObject.GetComponentInParent<SceneObjectMino>())
            Event_RemoveID(col.gameObject.GetComponentInParent<SceneObjectMino>().id);
    }

    private void EmitRemoveRPC(object sender, int _removeId){
        emitHasChanged((AbstractParameter)sender);
    }

    private void ReceiveRemoveObject(int _somid){
        if(_somid < 0)
            return;

        Vector3 removedAtPos = MinoGameManager.Instance.RemoveAndDeleteSOMID(_somid);
        if(removeParticle)
            Destroy(Instantiate(removeParticle, removedAtPos, Quaternion.identity), 3f);
    }
}
