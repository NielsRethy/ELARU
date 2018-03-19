using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Script_Destructible : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> _explodedObjects = new List<GameObject>();
    [SerializeField]
    private float _explosionForce = 10f;
    [SerializeField]
    private float _upBias = 1f;
    [SerializeField]
    private float _explodedObjectsLifeTime = 5f;

    [Space(5)]
    [SerializeField]
    private string _explosionSound = "Explosion";
    [SerializeField]
    private float _minDistance = 1f;
    [SerializeField]
    private float _maxDistance = 500f;

    [Space(5)]
    [SerializeField]
    private GameObject _particleSystem = null;
    [SerializeField]
    private MeshRenderer _renderer = null;
    private Rigidbody _rigidbody = null;
    private Script_PickUpEffects _scriptEffects = null;
    private Script_PickUpObject _scriptObject = null;
    [SerializeField]
    private float _destroyDelay = 5f;

    [Space(5)]
    //[SerializeField]
    //private float _noiseRange = 1f;
    private Script_ManagerEnemy _scriptEnemy = null;
    private const float NoiseAlarmThreshold = 10f;
    private Collider _collider = null;

    [SerializeField]
    private bool _damagePropsInRange = false;
    [SerializeField]
    private bool _damageEnemiesInRange = false;

    private const string LayerInteract = "Interactable";
    private LayerMask _layerInteract;
    private const string TagPickUp = "PickUp";
    private const string LayerEnemies = "Enemies";
    private LayerMask _layerEnemies;
    private const string TagEnemy = "Enemy";
    private string _initTag = "";

    [HideInInspector]
    public bool Destroyed = false;

    public void Start()
    {
        _collider = GetComponent<Collider>();
        var childColliders = GetComponentsInChildren<Collider>();
        foreach (var obj in _explodedObjects)
        {
            obj.SetActive(false);

            var objCollider = obj.GetComponent<Collider>();
            if (objCollider == null)
                continue;

            // Ignore collision with self
            Physics.IgnoreCollision(objCollider, _collider);
            // And with children, for each exploded object
            if (childColliders != null && childColliders.Length > 0)
                foreach (var childCollider in childColliders)
                    if (childCollider.gameObject != objCollider.gameObject)
                        Physics.IgnoreCollision(objCollider, childCollider);
        }

        if (_particleSystem != null)
            _particleSystem.SetActive(false);

        _rigidbody = GetComponent<Rigidbody>();
        _scriptEffects = GetComponent<Script_PickUpEffects>();
        _scriptObject = GetComponent<Script_PickUpObject>();

        _scriptEnemy = Script_ManagerEnemy.Instance;
        _layerInteract = LayerMask.NameToLayer(LayerInteract);
        _layerEnemies = LayerMask.NameToLayer(LayerEnemies);

        _initTag = tag;
    }

    public void DestroyBlock()
    {
        // Play particle
        if (_particleSystem != null)
        {
            _particleSystem.SetActive(true);
            _particleSystem.transform.SetParent(null);
        }

        if (_rigidbody != null)
            _rigidbody.isKinematic = true;

        // Explode
        foreach (var obj in _explodedObjects)
        {
            obj.SetActive(true);
            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            //find direction away from all other objects
            Vector3 explodeDirection = _explodedObjects.Where(o => o != obj)
                .Aggregate(Vector3.zero, (current, o) => current + (o.transform.position - obj.transform.position));
            //Debug.Log("Explode direction " + explodeDirection.normalized);
            var rand = Random.onUnitSphere;
            rand.y = Mathf.Abs(rand.y);
            rand.x /= 2f;
            rand.z /= 2f;
            explodeDirection += rand;

            explodeDirection.y = Mathf.Abs(explodeDirection.y) + _upBias / 10f;

            explodeDirection.Normalize();

            rb.AddForce(explodeDirection * _explosionForce, ForceMode.Impulse);

            obj.transform.parent = null;
        }

        if (_explosionSound.Length > 0)
            Script_AudioManager.Instance.PlaySFX(_explosionSound, transform.position, false, _minDistance, _maxDistance);

        gameObject.tag = "Untagged";
        Invoke("HideGameObject", 0.02f);
        StopAllCoroutines();
        StartCoroutine(DisableExplodedObjects());
        Destroyed = true;

        // Play a sound for all surrounding enemies
        _scriptEnemy.Sound(transform.position, _explosionForce > NoiseAlarmThreshold ? Script_EnemyBase.SoundType.Alarm : Script_EnemyBase.SoundType.Other, _explosionForce);

        if (_damageEnemiesInRange)
        {
            // Hurt enemies nearby explosion
            var collidersInRange = GetCollidersInExplosionRange(_layerEnemies, TagEnemy);

            if (collidersInRange.Length <= 0)
                return;

            foreach (Collider coll in collidersInRange)
            {
                var enemy = coll.GetComponent<Script_EnemyBase>();
                if (enemy != null && enemy.CanTakeDamage)
                    enemy.DealDamage(_explosionForce, transform.position);
            }
        }

        if (_damagePropsInRange)
        {
            // Blow up things nearby
            var collidersInRange = GetCollidersInExplosionRange(_layerInteract, TagPickUp);

            if (collidersInRange.Length <= 0)
                return;

            foreach (Collider coll in collidersInRange)
            {
                var destr = coll.GetComponent<Script_Destructible>();
                if (destr != null)
                    destr.DestroyBlock();
            }
        }
    }

    private Collider[] GetCollidersInExplosionRange(LayerMask mask, string tag)
    {
        return Physics.OverlapSphere(transform.position, _explosionForce / 2f, 1 << mask.value, QueryTriggerInteraction.Collide).Where(obj => obj.CompareTag(tag)).ToArray();
    }

    private void HideGameObject()
    {
        // Hide the mesh
        _renderer.enabled = false;
        // And prevent the player from interacting with it
        if (_scriptEffects != null)
            _scriptEffects.enabled = false;
        if (_scriptObject != null)
            _scriptObject.enabled = false;

        Invoke("Inactive", _destroyDelay);
    }

    private void Inactive()
    {
        //ToggleContent(false);
        gameObject.SetActive(false);
    }

    public void ResetContent()
    {
        // Disable or reenable the particle system
        if (_particleSystem != null)
        {
            _particleSystem.transform.position = transform.position;
            _particleSystem.transform.SetParent(transform);
            _particleSystem.SetActive(false);
        }

        // Hide or show the mesh
        if (_renderer != null)
            _renderer.enabled = true;

        // Disable or reenable the collision
        if (_collider != null)
            _collider.enabled = true;
        if (_rigidbody != null)
            _rigidbody.isKinematic = false;

        if (_initTag.Length > 0)
            tag = _initTag;

        // Disable interactivity with the object
        if (_scriptEffects != null)
            _scriptEffects.enabled = true;
        if (_scriptObject != null)
            _scriptObject.enabled = true;

        // Teleport any debris back to the center of this object
        ReenableExplodedObjects();

        Destroyed = false;
    }

    private IEnumerator DisableExplodedObjects()
    {
        yield return new WaitForSeconds(_explodedObjectsLifeTime);

        foreach (var obj in _explodedObjects)
            obj.gameObject.SetActive(false);

        yield break;
    }

    private void ReenableExplodedObjects()
    {
        foreach (var obj in _explodedObjects)
        {
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.GetComponent<Rigidbody>().isKinematic = false;
            obj.gameObject.SetActive(false);
        }
    }
}
