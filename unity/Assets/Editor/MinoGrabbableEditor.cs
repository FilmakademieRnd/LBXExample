using tracer;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MinoGrabbable))]
public class MinoGrabbableEditor : Editor
{
    private MinoGrabbable _grabbable;

    public override void OnInspectorGUI()
    {
        _grabbable = (MinoGrabbable)target;

        // Draw the default inspector
        base.OnInspectorGUI();

        GUILayout.Space(10);

        if (GUILayout.Button("Lock Object"))
        {
            Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), "Editor Call", "_grabbable"));
            _grabbable.lockObject(true);
            // Access the SceneObjectMino instance
        }
    }
}
