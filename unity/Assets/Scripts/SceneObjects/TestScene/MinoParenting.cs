using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoLocalParenting : MonoBehaviour{

    //Works in network only for SceneObjectMino that uses >jitterHotFixedUsed<
    //because they update their parent on their own

    [Tooltip("if empty, use itself")]
    public Transform parentingTarget;

    public List<string> tagsToAllow = new();

    private SceneObjectMino somToParent = null;

    void OnCollisionEnter(Collision other){
        if(IsSceneObjectMino(other.gameObject))
            somToParent.transform.parent = parentingTarget == null ? transform : parentingTarget;
    }
    void OnCollisionExit(Collision other){
        if(IsSceneObjectMino(other.gameObject))
            somToParent.transform.parent = null;
    }

    void OnTriggerEnter(Collider other){
        if(IsSceneObjectMino(other.gameObject))
            somToParent.transform.parent = parentingTarget == null ? transform : parentingTarget;
    }
    void OnTriggerExit(Collider other){
        if(IsSceneObjectMino(other.gameObject))
            somToParent.transform.parent = null;
    }

    private bool IsSceneObjectMino(GameObject check){
        if(tagsToAllow.Count != 0 && !tagsToAllow.Contains(check.tag)){
            somToParent = null;
            return false;
        }
            
        somToParent = check.GetComponentInParent<SceneObjectMino>();
        return somToParent != null;
    }

}
