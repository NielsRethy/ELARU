using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor extension script for: viewing, saving, loading and deleting SaveData inside the inspector
/// </summary>
[CustomEditor(typeof(Script_SaveFileManager))]
[CanEditMultipleObjects]
class Script_SaveFileManagerEditor : Editor
{
    private Script_SaveFileManager _instance = null;
    private string _loadLog = "";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Only display the buttons whilst in play mode
        if (!EditorApplication.isPlaying)
            return;

        if (_instance == null)
            _instance = Script_SaveFileManager.Instance;

        GUILayout.Space(10);
        // Log the save data information in a multi-line text area
        GUILayout.Label("Save file info");
        GUILayout.TextArea(_loadLog.Length > 0 ? _loadLog : "No save file loaded yet");

        GUILayout.Space(10);
        // Refresh (load the local SaveData)
        if (GUILayout.Button("Refresh"))
            _loadLog = _instance.GetLogLoadedData();

        GUILayout.Space(10);
        // Load SaveData
        if (GUILayout.Button("Load save file"))
        {
            _instance.LoadSceneData();
            // Set load log string
            _loadLog = "Save data loaded at: " +
                System.DateTime.Now.ToLongTimeString() +
                '\n' + _instance.GetLogLoadedData();
        }

        // Save SaveData
        if (GUILayout.Button("Save save file"))
        {
            _instance.SaveSceneData();
            // Set load log string
            _loadLog = "Save data saved at: " +
                System.DateTime.Now.ToLongTimeString();
        }

        // Delete SaveData
        if (GUILayout.Button("Delete save file"))
        {
            _instance.ClearSaveData();
            // Set load log string
            _loadLog = "Save data removed at: " +
                System.DateTime.Now.ToLongTimeString() +
                '\n' + _instance.GetLogLoadedData();
        }
    }
}
