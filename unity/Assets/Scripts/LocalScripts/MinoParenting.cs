/*
-----------------------------------------------------------------------------------
TRACER Location Based Experience Example

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://github.com/FilmakademieRnd/LBXExample

TRACER Location Based Experience Example is a development by Filmakademie 
Baden-Wuerttemberg, Animationsinstitut R&D Labs in the scope of the EU funded 
project EMIL (101070533).

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/

using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoLocalParenting : MonoBehaviour{

    //Works in network only for SceneObjectMino that uses >sendPosViaParent<
    //because they update their parent on their own

    [Tooltip("if empty, use itself")]
    public Transform parentingTarget;
    public bool excludePlayer = true;
    public bool useTrigger, useCollision = true;

    //public bool inheritParentLockState = true;    - should not be nec with "sendPosViaParent"

    public List<string> tagsToAllow = new();

    void OnCollisionEnter(Collision other){
        if(useCollision && IsSceneObjectMino(other.gameObject, out SceneObjectMino somToParent))
            somToParent.transform.parent = parentingTarget == null ? transform : parentingTarget;
    }
    void OnCollisionExit(Collision other){
        if(useCollision && IsSceneObjectMino(other.gameObject, out SceneObjectMino somToParent)){
            somToParent.transform.parent = null;    Debug.Log("UNPARENT (COL@"+gameObject.name+") "+somToParent.gameObject.name);
        }
    }

    void OnTriggerEnter(Collider other){
        if(useTrigger && IsSceneObjectMino(other.gameObject, out SceneObjectMino somToParent)){
            somToParent.transform.parent = parentingTarget == null ? transform : parentingTarget;
        }
    }
    void OnTriggerExit(Collider other){
        if(useTrigger && IsSceneObjectMino(other.gameObject, out SceneObjectMino somToParent)){
            somToParent.transform.parent = null;    Debug.Log("UNPARENT (TRIGG@"+gameObject.name+") "+somToParent.gameObject.name);
        }
    }

    private bool IsSceneObjectMino(GameObject check, out SceneObjectMino somToParent){
        if((tagsToAllow.Count != 0 && !tagsToAllow.Contains(check.tag)) || (excludePlayer && check.GetComponentInParent<MinoCharacter>() != null)){
            somToParent = null;
            return false;
        }

        Debug.Log("executing object: "+check.name);
            
        somToParent = check.GetComponentInParent<SceneObjectMino>();
        return somToParent != null;
    }

}
