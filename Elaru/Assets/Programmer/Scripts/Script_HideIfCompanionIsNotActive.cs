using UnityEngine;

public class Script_HideIfCompanionIsNotActive : Script_Singleton<Script_HideIfCompanionIsNotActive>
{
    private MeshRenderer _mesh;

    void Start()
    {
        _mesh = GetComponent<MeshRenderer>();
        _mesh.enabled = false;
    }

    public void UnhideMesh()
    {
        _mesh.enabled = true;
    }
}
