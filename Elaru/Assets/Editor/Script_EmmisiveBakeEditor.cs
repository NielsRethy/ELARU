using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Script_EmmisiveBake))]
public class Script_EmmisiveBakeEditor : Editor
{
    public override void OnInspectorGUI() //Extra stuff in inspector
    {
        base.OnInspectorGUI(); //keep default basic stuff in GUI
        GUILayout.Space(20f);

        if (GUILayout.Button("Force Rebake"))
        {
            var baker = (Script_EmmisiveBake)target;
            baker.ReBake();
            EditorUtility.SetDirty(baker);
        }
    }
}
