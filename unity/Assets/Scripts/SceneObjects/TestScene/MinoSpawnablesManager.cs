using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoSpawnablesManager : SceneObjectMino{
    
    public float debugSpawnRepeating = -1f;
    public int debugSpawnIndex = -1;

    public GameObject spawnParticle;
    public List<SceneObjectMino> spawnableUniqueObjects;
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


        spawnIndexAndId.setValue(
            new Vector2(index, MinoGameManager.Instance.AddObjectAndInit_Master(spawnableUniqueObjects[index], GetSpawnPos()) )
        );
        
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

        MinoGameManager.Instance.AddObjectAndInit_Client(spawnableUniqueObjects[(int)_spawnIndexAndId.x], (int)_spawnIndexAndId.y, GetSpawnPos());
        if(spawnParticle)
            Destroy( Instantiate(spawnParticle, transform.position, Quaternion.identity), 3f);
    }
}
