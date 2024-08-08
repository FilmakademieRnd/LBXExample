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
