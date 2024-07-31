using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SceneObjectMino))]
public class SceneObjectMinoEditor : Editor
{
    SceneObjectMino sceneObjectMino;
    
    
    public override void OnInspectorGUI()
    {
        // Assign the target object to the sceneObjectMino variable
        sceneObjectMino = (SceneObjectMino)target;

        // Draw the default inspector
        base.OnInspectorGUI();

        GUILayout.Space(10);

        if (GUILayout.Button("Lock Object"))
        {
            // Access the SceneObjectMino instance
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), "Editor Call", "sceneObjectMino"));
            sceneObjectMino.lockObject(true);
        }
        
        if (GUILayout.Button("Unock Object"))
        {
            // Access the SceneObjectMino instance
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), "Editor Call", "sceneObjectMino"));
            sceneObjectMino.lockObject(false);
        }
    }
    
}
