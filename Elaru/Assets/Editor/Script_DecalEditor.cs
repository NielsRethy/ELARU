using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Script_DecalGenerator))]
public class Script_DecalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Script_DecalGenerator myScript = (Script_DecalGenerator)target;
        if (GUILayout.Button("Build Decal"))
        {
            myScript.UpdateMesh();
        }
        if (GUILayout.Button("Clear Decal"))
        {
            myScript.ClearMesh();
        }
    }
}
