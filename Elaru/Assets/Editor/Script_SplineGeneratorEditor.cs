using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Script_SplineGenerator))]
public class Script_SplineGeneratorEditor : Editor
{
    public override void OnInspectorGUI() //Extra stuff in inspector
    {
        base.OnInspectorGUI(); //keep default basic stuff in GUI
        GUILayout.Space(20f);

        if (GUILayout.Button("Refresh")) //using GUILayout = automatic positioning, GUI manual positioning, Editor for making stuff in inspector(no button method)
        {
            var advancedSpline = (Script_SplineGenerator)target; //serializedObject for multiple objets, target for one
            advancedSpline.RefreshSplinePoints();

            EditorUtility.SetDirty(advancedSpline); //Refreshes object
        }
        if (GUILayout.Button("Add Control Point"))
        {
            var advancedSpline = (Script_SplineGenerator)target;
            advancedSpline.AddControlPoint();

            EditorUtility.SetDirty(advancedSpline); //Refreshes object
        }
    }

    private void OnSceneGUI() //Draw Extra stuff in Scene
    {
        var advancedSpline = (Script_SplineGenerator)target;
        var controls = advancedSpline.ControlPoints;

        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < controls.Count; ++i)
        {
            var control = new SplineControl();
            control.Handle = Handles.PositionHandle(controls[i].Handle, Quaternion.identity);
            var offset = control.Handle - controls[i].Point;
            control.Point = Handles.PositionHandle(controls[i].Point, Quaternion.identity);
            control.Handle = control.Point + offset;
            control.Handle2 = control.Point - offset;

            advancedSpline.ControlPoints[i] = control;
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(advancedSpline, "Moved spline");
            advancedSpline.RefreshSplinePoints();
            EditorUtility.SetDirty(advancedSpline);
        }
    }
}
