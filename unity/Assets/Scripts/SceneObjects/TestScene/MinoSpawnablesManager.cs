using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoSpawnablesManager : SceneObjectMino{
    
    public InteractableNetworkBehaviourEnum spawnNetworkBehaviour = InteractableNetworkBehaviourEnum.byMaster;

    [Header("SPAWN")]
    public GameObject spawnParticle;
    public List<SceneObjectMino> spawnableUniqueObjects;

    [Header("DEBUG")]
    public float debugSpawnRepeating = -1f;
    public int debugSpawnIndex = -1;
    private RPCParameter<Vector2> spawnIndexAndId;

    private float spawnTimer = -1f;

    public override void Awake(){
        base.Awake();

        spawnIndexAndId = new RPCParameter<Vector2>(Vector2.zero, "spawnIndex", this);
        spawnIndexAndId.hasChanged += EmitSpawnRPC;
        spawnIndexAndId.setCall(ReceiveSpawnObject);
    }

    public void Event_SpawnAt(int index){
        if(index < 0 || index >= spawnableUniqueObjects.Count)
            return;

        Debug.Log("Event::Spawn");
        switch(spawnNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byEveryone:
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                Debug.Log("..Spawn Here");
                spawnIndexAndId.Call(
                    new Vector2(index, MinoGameManager.Instance.AddObjectAndInit_Sender(spawnableUniqueObjects[index], GetSpawnPos())), 
                    true
                );

                //the below was working, but re-work so it uses the same as in SceneObjectInteractable!

                // spawnIndexAndId.setValue(
                //     new Vector2(index, MinoGameManager.Instance.AddObjectAndInit_Sender(spawnableUniqueObjects[index], GetSpawnPos()) )
                // );
                break;
            case InteractableNetworkBehaviourEnum.byMaster:
                if(MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){
                    //INITIATE SPAWN
                    Debug.Log("..Spawn Here");
                    spawnIndexAndId.setValue(
                        new Vector2(index, MinoGameManager.Instance.AddObjectAndInit_Sender(spawnableUniqueObjects[index], GetSpawnPos()) )
                    );
                }else{
                    //TELL MASTER THAT WE SHOULD SPAWN!
                    Debug.Log("..Tell Master to Spawn");
                    spawnIndexAndId.setValue(new Vector2(index, -1));
                }
                break;
        }
        
        if(spawnParticle)
            Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
    }

    public Vector3 GetSpawnPos(){ return transform.position; }

    protected override void Update(){
        if(spawnTimer > 0f)
            spawnTimer -= Time.deltaTime;

        if(debugSpawnIndex >= 0 && spawnTimer < 0){
            Event_SpawnAt(debugSpawnIndex);
            if(debugSpawnRepeating > 0f){
                spawnTimer = debugSpawnRepeating;
            }else
                debugSpawnIndex = -1;
        }
    }

    private void EmitSpawnRPC(object sender, Vector2 _spawnIndexAndId){
        emitHasChanged((AbstractParameter)sender);
    }

    private void ReceiveSpawnObject(Vector2 _spawnIndexAndId){
        if(_spawnIndexAndId.x < 0 || _spawnIndexAndId.x >= spawnableUniqueObjects.Count)
            return;

        switch(spawnNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byEveryone:
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                Debug.Log("..Spawn Here from elsewhere");
                MinoGameManager.Instance.AddObjectAndInit_Receiver(spawnableUniqueObjects[(int)_spawnIndexAndId.x], (int)_spawnIndexAndId.y, GetSpawnPos());
                if(spawnParticle)
                    Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
                break;
            case InteractableNetworkBehaviourEnum.byMaster:
                if(_spawnIndexAndId.y < 0){ //just a msg that the Master should spawn!
                    if(MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){
                        //SPAWN
                        Debug.Log("..Told to Spawn from elsewhere");
                        Event_SpawnAt((int)_spawnIndexAndId.x);
                    }
                }else{
                    Debug.Log("..Spawn Here from elsewhere");
                    MinoGameManager.Instance.AddObjectAndInit_Receiver(spawnableUniqueObjects[(int)_spawnIndexAndId.x], (int)_spawnIndexAndId.y, GetSpawnPos());
                    if(spawnParticle)
                        Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
                }
                break;
        }
    }
}
