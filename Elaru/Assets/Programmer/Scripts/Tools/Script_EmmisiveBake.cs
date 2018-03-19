#if UNITY_EDITOR
using UnityEditor; 
#endif
using UnityEngine;

/// <summary>
/// Script to enable emmisive baking on non standard shader materials
/// e.g. can be used to bake billboard emmisive
/// </summary>
public class Script_EmmisiveBake : MonoBehaviour
{
#if UNITY_EDITOR
    public bool Bake = false;

    public void ReBake()
    {
        gameObject.GetComponent<Renderer>().sharedMaterial.globalIlluminationFlags = Bake ?
            MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;

        Lightmapping.Bake();
    }
#endif
}
