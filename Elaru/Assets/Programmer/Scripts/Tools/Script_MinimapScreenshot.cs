using System.IO;
using UnityEngine;

/// <summary>
/// A script for taking top down screenshots inside the editor (also possible with MinimapScreenshotEditor now)
/// </summary>
[System.Obsolete()]
public class Script_MinimapScreenshot : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    private RenderTexture[] _textures = null;
    private const string filePath = "Assets/Programmer/Textures/minimap_screenshot";

    [SerializeField]
    private Camera[] _cameras = null;
    private bool _takePics = true;

    private float _timer = 0.0f;

    private void Update()
    {
        if (_takePics)
        {
            _timer += Time.deltaTime;
            if (_timer > 0.1f)
            {
                _timer = 0.0f;
                int count = 0;
                for (int i = 0; i < _cameras.Length; ++i)
                    Screenshot(_cameras[i], _textures[i], count++.ToString("00"));
                // Caculate frustum
                var frustumHeight = 2.0f * _cameras[0].farClipPlane * Mathf.Tan(_cameras[0].fieldOfView * 0.5f * Mathf.Deg2Rad);
                var frustumWidth = frustumHeight * _cameras[0].aspect;
                Debug.Log("h: " + frustumHeight + ", " + "w: " + frustumWidth);
                _takePics = false;
            }
        }
    }

    private void Screenshot(Camera camera, RenderTexture texture, string filePathApp)
    {
        // Give Unity time to update the render texture
        var oldRT = RenderTexture.active;

        // _camera.targetTexture.Create();
        var tex = new Texture2D(texture.width, texture.height);
        RenderTexture.active = camera.activeTexture;
        // Check if the camera texture has actually been created before reading it
        Debug.Log(camera.activeTexture);
        var rect = new Rect(0, 0, camera.activeTexture.width, camera.activeTexture.height);
        tex.ReadPixels(rect, 0, 0);

        tex.Apply();

        File.WriteAllBytes(filePath + filePathApp + ".png", tex.EncodeToPNG());
        Debug.Log(filePath + " texture saved");
        RenderTexture.active = oldRT;
    }
#endif
}
