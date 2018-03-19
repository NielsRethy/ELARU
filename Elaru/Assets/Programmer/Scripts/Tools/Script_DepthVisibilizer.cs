using UnityEngine;

/// <summary>
/// Script used to switch an object material to the see-through-walls material while maintaining properties
/// Code has been directly moved to companion script, so this script is no longer directly used
/// </summary>
[System.Obsolete("Script functionality has been moved directly to companion")]
public class Script_DepthVisibilizer : MonoBehaviour
{
    private Shader _depthShader;
    private Shader _standardShader;
    public Color DepthColor;

    void Start()
    {
        _depthShader = Shader.Find("Custom/Shader_Depth");
        _standardShader = Shader.Find("Standard");
    }

    void ChangeToDepthSeeThrough(GameObject o)
    {
        var ren = o.GetComponent<Renderer>();
        if (!ren)
            return;
        Material mat = ren.material;
        Material newMat = new Material(_depthShader);
        newMat.CopyPropertiesFromMaterial(mat);
        newMat.SetColor("_ZColor", DepthColor);
        ren.material = newMat;
        if (o.transform.childCount > 0)
        {
            foreach (Transform child in o.transform as Transform)
            {
                var cRen = child.GetComponent<Renderer>();
                if (cRen != null)
                {
                    cRen.material = newMat;
                }
            }
        }
    }

    void GetRidOfDepthSeeThrough(GameObject o)
    {
        var ren = o.GetComponent<Renderer>();
        if (!ren)
            return;
        Material mat = ren.material;
        Material newMat = new Material(_standardShader);
        newMat.CopyPropertiesFromMaterial(mat);
        ren.material = newMat;
        if (o.transform.childCount > 0)
        {
            foreach (Transform child in o.transform as Transform)
            {
                var cRen = child.GetComponent<Renderer>();
                if (cRen != null)
                    cRen.material = newMat;
            }
        }
    }
}
