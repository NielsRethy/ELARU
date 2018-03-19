using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Script_FunctionTest))]
public class Script_TestFunctionEditor : Editor
{
    public override void OnInspectorGUI() //Extra stuff in inspector
    {
        base.OnInspectorGUI(); //keep default basic stuff in GUI
        GUILayout.Space(20f);

        if (GUILayout.Button("Execute")) 
        {
            var functionTest = (Script_FunctionTest)target;
            functionTest.Event.Invoke();

            EditorUtility.SetDirty(functionTest);
        }
    }
}
