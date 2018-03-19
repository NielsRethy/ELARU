using UnityEngine;

public class Script_ShieldControl : Script_Singleton<Script_ShieldControl>
{
    private Material _mat;
    private float _openValue = 0f;
    [SerializeField]
    private float _openSpeed = 2f;

    private float _shieldTime = 0f;
        
    private void Awake()
    {
        //Cache material
        _mat = GetComponent<Renderer>().sharedMaterial;

        //Start shield closed
        _openValue = 0;
    }
    
    public void ActivateShield()
    {
        //Reset values and update them in shader
        _openValue = 0f;
        _shieldTime = 0f;
        _mat.SetFloat("_OpenValue", _openValue);
        _mat.SetFloat("_ShieldTime", _shieldTime);

        //Activate shield in scene
        gameObject.SetActive(true);
    }

    public void DeactivateShield()
    {
        //Reset values
        _openValue = 0f;
        _shieldTime = 0f;

        //Deactivate shield in scene
        gameObject.SetActive(false);
    }

    private void Update()
    {
        //Update value in script and shader
        _openValue += Time.deltaTime * _openSpeed;
        _shieldTime += Time.deltaTime * .1f;
        _mat.SetFloat("_OpenValue", _openValue);
        _mat.SetFloat("_ShieldTime", _shieldTime);
    }
}
