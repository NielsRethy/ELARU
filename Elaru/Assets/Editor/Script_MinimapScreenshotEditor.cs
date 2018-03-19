using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;

/// <summary>
/// Editor utility for taking minimap screenshot using an orthographic camera
/// </summary>
public class Script_MiniMapScreenshotEditor : EditorWindow
{
    [MenuItem("ELARU/Minimap Screenshot Tool")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Script_MiniMapScreenshotEditor), false, "Minimap Tool");
    }

    // Camera variables
    static private Camera _camera = null;
    // Camera settings
    private LayerMask _cullingMask = -1;
    private float _height = 20f;
    private Vector3 _cameraPosition = new Vector3(0f, 500f, 0f);
    private float _orthographicSize = 500;

    // Screenshot variables
    private const string _filePath = "Assets/Programmer/Textures/minimap_screenshot";
    private int _renderSize = 2048;
    private Texture2D _texture = null;
    private bool _applyOnce = false;

    public void OnGUI()
    {
        //  If any of the cameras doesn't exist
        GUILayout.Label("Camera options", EditorStyles.boldLabel);
        GUILayout.Label("Culling mask");
        var layer = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_cullingMask), InternalEditorUtility.layers);
        _cullingMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(layer);

        GUILayout.Space(10);

        // Show the create cameras button

        if (_camera == null)
        {
            if (GUILayout.Button("Create orthographic camera"))
                CreateOrthographicCamera();
        }
        // If the camera exist, allow it to be removed
        else
        {
            // Apply new camerasettings
            // Set orthographic size
            GUILayout.Label("Orthographic size");
            var ortho = _orthographicSize;
            _orthographicSize = EditorGUILayout.Slider(ortho, 0, 10000);
            _camera.orthographicSize = _orthographicSize;
            // Set far clipping plane via height value
            GUILayout.Label("Height difference");
            var height = _height;
            _height = EditorGUILayout.FloatField(height);
            _camera.farClipPlane = _camera.orthographicSize - _height;

            if (GUILayout.Button("Remove orthographic camera"))
                DestroyImmediate(_camera.gameObject);

            // Show screenshot options whilst camera exists
            GUILayout.Space(10);
            GUILayout.Label("Screenshot options", EditorStyles.boldLabel);

            // Set screenshot resolution
            GUILayout.Label("Render size");
            var size = _renderSize;
            _renderSize = EditorGUILayout.IntField(size);

            if (GUILayout.Button("Capture screenshot"))
            {
                // Take the screenshot
                Screenshot();

                // Caculate frustum
                var frustumHeight = 2.0f * _camera.farClipPlane * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                var frustumWidth = frustumHeight * _camera.aspect;
                Debug.Log("h: " + frustumHeight + ", " + "w: " + frustumWidth);
            }
        }

        // Show preview of the screenshot if one has been made
        if (_texture != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Screenshot", EditorStyles.boldLabel);

            // Skip if null
            if (_texture == null)
                return;

            GUILayout.Label("Saved at: " + _filePath);

            if (_applyOnce)
            {
                _texture.Apply();
                _applyOnce = false;
            }

            GUI.DrawTexture(new Rect(0f, EditorGUILayout.GetControlRect().y, 128, 128), _texture);
        }
    }

    private void CreateOrthographicCamera()
    {
        // Create game object
        var obj = new GameObject("Camera (minimap)");
        // Set position and rotation
        obj.transform.position = _cameraPosition;
        obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        // Add camera component
        var camera = obj.AddComponent<Camera>();
        // Settings
        // Set clear flag and culling mask
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.cullingMask = _cullingMask;
        // Set camera to orthographic
        camera.orthographic = true;
        camera.orthographicSize = _orthographicSize;
        // Move far clipping plane further up according to the amount of cameras that should be created
        camera.farClipPlane = camera.orthographicSize - _height;
        // Don't occlude
        camera.useOcclusionCulling = false;
        // Set camera to non-VR
        camera.stereoTargetEye = StereoTargetEyeMask.None;

        // Add the new camera to the array
        _camera = camera;
    }

    private void Screenshot()
    {
        // Create new texture
        var newTexture = new Texture2D(_renderSize, _renderSize, TextureFormat.RGB24, false);
        var target = new RenderTexture(_renderSize, _renderSize, 24);

        // The bit that actually renders the pixels
        _camera.targetTexture = target;
        _camera.Render();
        _camera.targetTexture = null;
        RenderTexture.active = target;

        var rect = new Rect(0, 0, _renderSize, _renderSize);
        newTexture.ReadPixels(rect, 0, 0);

        // Set variable for preview inside Unity
        _texture = newTexture;
        _applyOnce = true;

        // Delete and reset variables
        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(target);

        // Save the texture file
        File.WriteAllBytes(_filePath + ".png", newTexture.EncodeToPNG());
        //Debug.Log(_filePath + " texture saved");
    }
}
