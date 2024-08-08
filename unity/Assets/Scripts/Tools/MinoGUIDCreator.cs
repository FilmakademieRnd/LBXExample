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

#if UNITY_EDITOR
using tracer;
using UnityEngine;
using UnityEditor;
using MenuItem = UnityEditor.MenuItem;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class MinoGUIDCreator : IPreprocessBuildWithReport{
    
    [MenuItem("Tools/Create GUIDs")]
    static void CreateGUIDs(){
        //ONLY CREATE/CHANGE IF NOT SET
        int objectsChanged = 0;
        int overallObjects = 0;
        List<string> existentGUIDs = new(); //neccessary if we copy an object, it will have an id!
        foreach(SceneObjectMino som in Object.FindObjectsOfType<SceneObjectMino>()){
            if(string.IsNullOrEmpty(som.ourGUID) || existentGUIDs.Contains(som.ourGUID)){
                som.ourGUID = som.gameObject.name + som.GetInstanceID();
                objectsChanged++;
                EditorUtility.SetDirty(som);
                EditorUtility.SetDirty(som.gameObject);
            }
            existentGUIDs.Add(som.ourGUID);
            overallObjects++;
        }

        Debug.Log("Setup GUID of "+objectsChanged+" new objects. (Of "+overallObjects+" overall objects)");
    }

    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report){
        CreateGUIDs();
    }
}
#endif
