using tracer;
using UnityEngine;

public class CollisionLocking : MonoBehaviour{

    //gain lock of objects we, as a player, collide with, since network chars do not have physics!
    private SceneObjectMino sceneObjectScript;

    void Start(){
        sceneObjectScript = GetComponent<SceneObjectMino>();
    }

    void OnCollisionEnter(Collision col){
        MinoCharacter player = col.gameObject.GetComponentInParent<MinoCharacter>();
        if(player && sceneObjectScript.IsLocked()){
            sceneObjectScript.lockObject(true);
        }
    }
}
