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
