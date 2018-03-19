using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Script_GunProjectile : MonoBehaviour
{
    public enum ProjectileType
    {
        Sound,
        Light
    }

    private ProjectileType _type = ProjectileType.Sound;
    public ProjectileType Type
    {
        get { return _type; }
        set
        {
            _type = value;
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            _meshFilter.mesh = _type == ProjectileType.Sound ? _soundMesh : _lightMesh;
        }
    }

    public float ExplosionRange = 5f;

    private Rigidbody _rb;
    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private GameObject _soundParticles = null;
    [SerializeField]
    private GameObject _lightParticles = null;
    [SerializeField]
    private Mesh _soundMesh = null;
    [SerializeField]
    private Mesh _lightMesh = null;

    private MeshFilter _meshFilter = null;
    private TrailRenderer _tr = null;

    private Color _lightColor = Color.yellow;
    private Color _soundColor = Color.green;

    private Light _light = null;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _meshFilter = GetComponent<MeshFilter>();
        _tr = GetComponent<TrailRenderer>();
        if (_tr != null)
            _tr.enabled = false;
        _soundParticles.SetActive(false);
        _lightParticles.SetActive(false);

        _light = GetComponentInChildren<Light>();
    }

    private void Explode()
    {
        if (Type == ProjectileType.Sound)
        {
            Script_ManagerEnemy.Instance.Sound(transform.position, Script_EnemyBase.SoundType.Player, ExplosionRange);
            Script_AudioManager.Instance.PlaySFX("GunSoundDetonate", transform.position);
            if (_soundParticles != null)
                _soundParticles.SetActive(true);
        }

        else if (Type == ProjectileType.Light)
        {
            Script_ManagerEnemy.Instance.Light(transform.position, ExplosionRange);
            Script_AudioManager.Instance.PlaySFX("GunLightDetonate", transform.position);
            if (_lightParticles != null)
                _lightParticles.SetActive(true);
        }

        //Reset rigidbody for next shooting
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.rotation = Quaternion.identity;
        _rb.isKinematic = true;
        _renderer.enabled = false;
        _tr.enabled = false;
        Invoke("DeactivateObject", .5f);
    }

    private void DeactivateObject()
    {
        if (_soundParticles != null)
            _soundParticles.SetActive(false);
        if (_lightParticles != null)
            _lightParticles.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Shoot(Vector3 origin, Vector3 direction, float explodeTime)
    {
        //Safety check rigidbody for newly spawned projectiles
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        var targetColor = _type == ProjectileType.Light ? _lightColor : _soundColor;

        _renderer.enabled = true;
        _renderer.material.SetColor("_EmissionColor", targetColor);

        if (_tr == null)
            _tr = GetComponent<TrailRenderer>();
        // Reset trails
        if (_tr != null)
        {
            _tr.enabled = false;
            _tr.enabled = true;
            _tr.startColor = targetColor;
            _tr.endColor = targetColor / 10f;
        }

        if (_light == null)
            _light = GetComponentInChildren<Light>();

        _light.color = targetColor;

        //Move to shoot origin and apply force in correct direction
        _rb.isKinematic = false;
        _rb.position = origin;
        _rb.AddForce(direction, ForceMode.Impulse);
        _rb.AddTorque(direction.normalized, ForceMode.Impulse); //A little spin on the object

        //Explode after time
        Invoke("Explode", explodeTime);
    }
}
