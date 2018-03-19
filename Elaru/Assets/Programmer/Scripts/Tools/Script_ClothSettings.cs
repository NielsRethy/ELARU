using UnityEngine;

public class Script_ClothSettings : MonoBehaviour
{
    void Start()
    {
        var ren = GetComponent<Renderer>();
        if (ren == null)
            return;
        var mat = ren.material;
        if (mat == null)
            return;

        mat.SetInt("_TimeOffset", GenerateId() % 100);
        Destroy(this);
    }

    private int GenerateId()
    {
        //Hash name and position
        var objectNameHash = name.GetHashCode();
        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f));

        //Create ID
        return (int)(objectNameHash + posHash);
    }
}
