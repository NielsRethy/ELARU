using UnityEngine;
public class Script_DummyAI : MonoBehaviour
{
    private int _health = 160;
    private bool _ableToTakeDamage = true;
    [SerializeField]
    private MeshRenderer _renderer = null;
    private static float _rotationAngleMax = 10f;
    private static string _colorShaderId = "_EmissionColor";
    private bool _takingDmg = false;

    private void Start()
    {
        if (_renderer == null)
        {
            _renderer.GetComponentInChildren<MeshRenderer>();
            if (_renderer == null)
                _renderer.GetComponent<MeshRenderer>();
            if (_renderer == null)
                Debug.LogWarning("No mesh renderer found in gameobject");
        }
    }

    public void DealDamage(int amount)
    {
        if (_takingDmg)
            return;

        _takingDmg = true;
        _renderer.material.SetColor(_colorShaderId, Color.red);

        //Substract damage from healthpool
        if ((_health -= amount) <= 0)
        {
            transform.Rotate(Vector3.forward, -_rotationAngleMax * 3f);
            Invoke("Destroy", 0.25f);
            return;
        }

        transform.Rotate(Vector3.forward, -_rotationAngleMax);

        // Subtle hit effect
        Invoke("ResetColorAndRotation", 0.1f);
    }

    private void ResetColorAndRotation()
    {
        _takingDmg = false;
        _renderer.material.SetColor(_colorShaderId, Color.black);
        transform.rotation = Quaternion.identity;
    }

    private void Destroy()
    {
        CancelInvoke();
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Blade"))
            return;

        var sword = other.transform.parent.GetComponent<Script_Sword>();
        if (_ableToTakeDamage && sword != null)
            DealDamage((int)sword.Damage);

        //Player has to pull out sword before dealing damage again
        _ableToTakeDamage = false;
    }

    private void OnTriggerExit(Collider other)
    {
        //Reset damageability
        _ableToTakeDamage = true;
    }
}
