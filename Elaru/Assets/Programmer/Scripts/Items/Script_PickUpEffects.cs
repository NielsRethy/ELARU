using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A script for handling interactable object effects like: the emissive turning on/off and sound effects playing.
/// </summary>
public class Script_PickUpEffects : MonoBehaviour
{
    private Shader _shaderHologram = null;
    private Material _materialHologram = null;
    private List<Shader> _listInitShaders = new List<Shader>();

    [SerializeField]
    private PropType _propType = PropType.None;

    [Space(10)]
    [SerializeField]
    private Renderer _meshRenderer = null;
    [SerializeField]
    private Renderer[] _useArrayOfRenderers = null;
    public Renderer[] Renderers { get { return _meshRenderer != null ? new Renderer[1] { _meshRenderer } : _useArrayOfRenderers; } }

    private bool _firstHover = false;
    private bool _hover = false;

    //Base audio vars
    private Script_AudioManager _scriptAudio = null;
    //private bool _skipAudio = false;
    //Audio on interaction vars
    [Header("Interaction audio (can be empty)")]
    [SerializeField]
    private AudioSource _audioSource = null;
    public AudioSource AudioSource { get { return _audioSource; } set { _audioSource = value; } }

    [SerializeField]
    [Tooltip("Array of random audio clips to play when the player hovers their hand over the object")]
    public string SoundEffectOnHover = "";
    [SerializeField]
    [Tooltip("Array of random audio clips to play when the player picks up the object")]
    public string SoundEffectOnPressed = "PickupPressed";
    [SerializeField]
    [Tooltip("Array of random audio clips to play when the player releases the object")]
    public string SoundEffectOnRelease = "";

    private bool _pressed = false;
    private HandSide _hand = HandSide.None;
    public HandSide HandInPickUp { get { return _hand; } }
    private Script_LocomotionBase _scriptLoco = null;
    public bool BeingHeld = false;
    private Collider _thisTrigger = null;

    private const string TagPickUp = "PickUp";
    private Script_PickUpObject _object = null;
    private Object_PropType.Prop _prop;
    private Script_Destructible _scriptDestructible = null;

    private LayerMask _maskEnemiesOnly = -1;
    private Script_ManagerEnemy _scriptEnemy = null;
    private const string EnemyLayer = "Enemies";

    protected void Awake()
    {
        _scriptAudio = Script_AudioManager.Instance;
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptEnemy = Script_ManagerEnemy.Instance;
    }

    protected void Start()
    {
        _shaderHologram = Shader.Find("Custom/Shader_Hologram");
        if (_shaderHologram == null)
        {
            Debug.LogWarning("Failed to find hologram shader!", gameObject);
            return;
        }

        // Create replacement material
        _materialHologram = new Material(_shaderHologram);
        // Set shader variables
        _materialHologram.SetColor("_Color", Color.yellow);

        // Set
        if (_useArrayOfRenderers != null && _useArrayOfRenderers.Length > 0)
        {
            foreach (Renderer render in _useArrayOfRenderers)
                foreach (Material material in render.materials)
                    _listInitShaders.Add(material.shader);
        }
        else
        {
            // Get mesh renderer if neccesary
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();
                Debug.Log("Had to use get component to get the mesh renderer on: " + name, gameObject);
            }
            if (_meshRenderer == null)
                Debug.LogWarning("Failed to get mesh renderer on: " + name, gameObject);
            else
                foreach (Material material in _meshRenderer.materials)
                    _listInitShaders.Add(material.shader);
        }
        var col = GetComponents<Collider>();

        if (col.Length > 0)
            _thisTrigger = col.FirstOrDefault(coll => coll.isTrigger);

        // Get prop type
        if (_propType == PropType.None)
            return;

        _object = GetComponent<Script_PickUpObject>();
        _prop = Object_PropType.Instance.GetPropByType(_propType);
        if (_object.Rigidbody != null)
            _object.Rigidbody.drag = _prop.Drag;

        _scriptDestructible = GetComponent<Script_Destructible>();
        _maskEnemiesOnly = LayerMask.NameToLayer(EnemyLayer);
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (Hover || BeingHeld)
            return;

        // Get the hand hovering over the pick up
        _hand = _scriptLoco.GetHandSideFromObject(other.gameObject);
        // Select this pick up
        if (_hand != HandSide.None)
            Hover = true;
    }

    protected void OnTriggerExit(Collider other)
    {
        if (_scriptLoco.GetHandSideFromObject(other.gameObject) != HandSide.None)
        {
            Hover = false;
            _hand = HandSide.None;

            // Retry on trigger enter (avoids using trigger stay)
            if (_thisTrigger == null) return;
            _thisTrigger.enabled = false;
            _thisTrigger.enabled = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_object == null || _object.BeingHeld || !_meshRenderer.enabled)
            return;

        var vel = collision.relativeVelocity.magnitude;

        // Play impact sound (use velocity as volume)
        if (vel > 0.1f)
            _scriptAudio.PlaySFX(_prop.ImpactSFX, transform.position, Mathf.Clamp01(vel / 10f));

        // Check if any enemies nearby hear the sound
        //if (_scriptEnemy.GetNrOfNearbyEnemies(vel * 10f, transform.position) > 0)
        _scriptEnemy.Sound(transform.position, Script_EnemyBase.SoundType.Other, vel * 10f);

        // Use the damage threshold to check if the object should break
        if (_prop.DamageThreshold <= 0 || vel < 0.5f)
            return;

        if (collision.gameObject.CompareTag("Enemy"))
            collision.gameObject.GetComponent<Script_EnemyBase>().DealDamage(vel, transform.position);

        // Break the prop if it's past the damage threshold
        Damage(vel, Vector3.zero);
    }

    public void Damage(float damage, Vector3 collDirection)
    {
        // Break if damage is past the damage threshold
        if (damage > _prop.DamageThreshold)
        {
            _scriptAudio.PlaySFX(_prop.BreakSFX, transform.position);

            // Destroy the prop
            if (_scriptDestructible != null)
                _scriptDestructible.DestroyBlock();
            else
                Destroy(gameObject);

            // TODO: Spawn some particles?

        }
        // Launch the prop in the direction
        else if (_object.Rigidbody != null && collDirection != Vector3.zero)
        {
            _object.Rigidbody.WakeUp();
            _object.Rigidbody.AddForce(collDirection * damage * _object.Rigidbody.mass, ForceMode.Impulse);
        }
    }

    //Properties to update object state
    /// <summary>
    /// Toggle the hologram shader and play hover sound effects (if there are any)
    /// </summary>
    public bool Hover
    {
        get { return _hover; }
        set
        {
            _hover = value;

            if (value)
                _firstHover = true;

            // Play audio on first hover only
            if (_firstHover)
            {
                if (_audioSource != null)
                    _scriptAudio.PlaySFX(SoundEffectOnHover, _audioSource);
                else
                    _scriptAudio.PlaySFX(SoundEffectOnHover, transform.position);
            }

            // Set the shaders
            if (_useArrayOfRenderers != null && _useArrayOfRenderers.Length > 0)
            {
                foreach (Renderer render in _useArrayOfRenderers)
                    for (int i = 0; i < render.materials.Length; ++i)
                        if (render.materials[i] != null)
                            render.materials[i].shader = value ? _shaderHologram : _listInitShaders[i];
            }
            else
            {
                if (_meshRenderer != null && _meshRenderer.materials.Length > 0)
                    for (int i = 0; i < _meshRenderer.materials.Length; ++i)
                        if (_meshRenderer.materials[i] != null)
                            _meshRenderer.materials[i].shader = value ? _shaderHologram : _listInitShaders[i];
            }

            if (!value)
                _firstHover = false;
        }
    }

    /// <summary>
    /// Hide the emissive game object and play pressed sound effects (if there are any)
    /// </summary>
    public bool Pressed
    {
        get { return _pressed; }
        set
        {
            // Play audio clips when the object is picked up
            if (_audioSource != null)
                _scriptAudio.PlaySFX(value ? SoundEffectOnPressed : SoundEffectOnRelease, _audioSource);
            else
                _scriptAudio.PlaySFX(value ? SoundEffectOnPressed : SoundEffectOnRelease, transform.position);

            if (_pressed != value)
                Hover = false;

            // Hide the emissive game object
            _pressed = value;
        }
    }
}
