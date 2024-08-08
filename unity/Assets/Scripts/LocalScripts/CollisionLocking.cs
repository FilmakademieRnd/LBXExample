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
