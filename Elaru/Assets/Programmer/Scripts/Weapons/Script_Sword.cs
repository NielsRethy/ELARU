using System;
using System.Linq;
using UnityEngine;

public class Script_Sword : Script_Weapon
{
    private bool _isOn = false;

    //Swing distance check vars
    private Vector3 _prevPos = Vector3.zero;
    [SerializeField]
    private float _posCheckInterval = .1f;
    private float _posCheckTimer = 0f;
    [SerializeField]
    private float _shakeDistance = .2f;
    [SerializeField]
    private ushort _hapticIntensity = 3000;

    [SerializeField]
    private GameObject _blade = null;

    [SerializeField]
    private float _baseActivationSoundRange = 5f;

    [SerializeField]
    private MeleeWeaponTrail _swordTrail = null;

    private Script_LocomotionBase _locoInstance = null;

    #region Effect variables
    [Header("Effects")]
    [SerializeField]
    private AudioSource _audioSourceSwing = null;
    [SerializeField]
    private AudioSource _audioSourceIdle = null;
    private Vector3 _audioPos = Vector3.zero;
    private Script_AudioManager _scriptAudio = null;

    [SerializeField]
    private Light[] _lightsSword = null;
    [SerializeField]
    private ParticleSystem[] _particlesActive = null;
    [SerializeField]
    private ParticleSystem[] _particlesOverheat = null;

    [SerializeField]
    private SkinnedMeshRenderer _meshRenderer = null;
    [SerializeField]
    private Animator _animator = null;

    // Sound array names
    private const string SoundActivate = "SwordActivate";
    private const string SoundDeactivate = "SwordDeactivate";
    private const string SoundCooled = "SwordCooled";
    private const string SoundOverheated = "WeaponCooling";
    private const string SoundTriggerPress = "TriggerPress";
    private const string SoundTriggerRelease = "TriggerRelease";

    #endregion

    private const int _materialGripLightsID = 0;
    private const int _materialBladeLightsID = 2;
    private const int _materialOverheatID = 3;
    private const int _materialBladeID = 4;
    private const string _shaderID = "_EmissionColor";
    [SerializeField]
    private Color _colorWhilstActive = Color.cyan;
    private Action<bool> _actionOverheat = null;

    private const string _animTriggerID = "Trigger";
    private const string _animCoolingID = "Cooling";
    private const string _animHoldingID = "Holding";
    private const string _animBladeID = "Blade";

    [SerializeField]
    private GameObject _swordOnParticle = null;
    [SerializeField]
    private GameObject _swordOverheatParticle = null;
    [SerializeField]
    private ParticleSystem _swordSlashParticle = null;

    private Script_ManagerEnemy _scriptEnemy = null;
    private Script_TactileFeedback _scriptFeedback = null;

    private const string TagDestructible = "Destructible";
    private const string TagPickUp = "PickUp";

    private void Start()
    {
        //Cache components
        PickUpObjectScript = GetComponent<Script_PickUpObject>();
#if DEBUG
        if (!PickUpObjectScript)
            Debug.LogError("Sword has no pick up object script", gameObject);
#endif

        if (_blade != null)
        {
            var bladeCollision = _blade.GetComponent<Script_CollisionArea>();
            if (bladeCollision != null)
                bladeCollision.TriggerEnterAction += BladeCollisionEnter;
#if DEBUG
            else
                Debug.LogError("No collision area on sword blade");
#endif

            _blade.SetActive(false);
        }
#if DEBUG
        else
            Debug.LogError("No blade assigned to sword prefab! Update will be skipped", gameObject);
#endif

        Script_WeaponManager.Instance.RegisterWeapon(this);
        //_blade = transform.GetChild(0).gameObject;
        _locoInstance = Script_LocomotionBase.Instance;

        // Get miss effect variable dependencies
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _scriptAudio = Script_AudioManager.Instance;

        // Add light and sounds to pick up actions
        PickUpObjectScript.OnGrab += TurnOnEffects;
        PickUpObjectScript.OnRelease += TurnOffEffects;

        _actionOverheat += ToggleCooldownEffects;

        // Initital effects state
        ToggleEffects(false);

        if (_swordTrail == null)
            _swordTrail = GetComponentInChildren<MeleeWeaponTrail>();

        if (_swordTrail != null)
            _swordTrail.Emit = false;

        //Deactivate particles on start
        if (_swordOnParticle != null)
            _swordOnParticle.SetActive(false);

        if (_swordOverheatParticle != null)
            _swordOverheatParticle.SetActive(false);

        if (_swordSlashParticle != null)
            _swordSlashParticle.Stop();

        _scriptEnemy = Script_ManagerEnemy.Instance;
        _scriptFeedback = Script_TactileFeedback.Instance;

        _audioPos = _audioSourceIdle.transform.position;

        PickUpObjectScript.OnGrab += PlayIdleSound;
        PickUpObjectScript.OnRelease += StopIdleSound;
    }

    private void PlayIdleSound(GameObject obj = null)
    {
        if (_audioSourceIdle != null && !_audioSourceIdle.isPlaying)
        {
            _audioSourceIdle.loop = true;
            _audioSourceIdle.Play();
        }
    }

    private void Update()
    {
        if (_blade == null)
            return;

        //Teleport to free dock or position if too far from player
        if (IsTooFarFromPlayer())
        {
            if (!_teleportToLocationInsteadOfDocks)
                Script_WeaponDocking.Instance.TeleportToFreeDock(gameObject);
            else
            {
                var rb = GetComponent<Rigidbody>();
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                transform.position = _teleportToLocation;
                transform.rotation = Quaternion.identity;
            }
        }

        var hs = PickUpObjectScript.ControlHandSide;

        var trg = _locoInstance.TriggerButton;

        // Update animation
        UpdateAnimationVariables(hs);

        // Update overheat material according to overheat float
        //if (_isOverHeated)
        if (_meshRenderer != null)
            _meshRenderer.materials[_materialOverheatID].SetColor(_shaderID, Color.Lerp(Color.black, Color.red, _overHeating));

        //Press trigger
        if (_locoInstance.GetPressDown(trg, hs))
        {
            // Play trigger release sound
            _scriptAudio.PlaySFX(SoundTriggerPress, _audioPos);

            if (!_isOverHeated)
            {
                // Activate particles
                ToggleParticles(true, _particlesActive);

                //Update press start position for shake distance check
                _prevPos = transform.position;

                //Activate sword
                _isOn = true;
                _blade.SetActive(true);

                //Activate sword on particles
                if (_swordOnParticle != null)
                    _swordOnParticle.SetActive(true);

                //Play activation sound TODO: implemenent actual sound as well
                _scriptEnemy.Sound(transform.position, Script_EnemyBase.SoundType.Player,
                    _baseActivationSoundRange / RangeFactor);

                //Activate sword cut trail
                if (_swordTrail != null)
                    _swordTrail.Emit = true;
            }
            else
            {
                // Sound cooled
                _scriptAudio.PlaySFX(SoundCooled, _audioPos);
            }
        }
        //Release trigger
        if (_locoInstance.GetPressUp(trg, hs))
        {
            //Deactivate sword
            _isOn = false;
            _blade.SetActive(false);

            //Deactivate sword on particles
            if (_swordOnParticle != null)
                _swordOnParticle.SetActive(false);

            if (_swordSlashParticle != null)
                _swordSlashParticle.Stop();

            _posCheckTimer = 0f;
            ToggleParticles(false, _particlesActive);

            //Stop cutting trail emmision
            if (_swordTrail != null)
                _swordTrail.Emit = false;

            // Play trigger release sound
            _scriptAudio.PlaySFX(SoundTriggerRelease, _audioPos);
        }

        //Shake controller when moving activated sword
        if (_isOn)
        {
            //Update position check
            _posCheckTimer += Time.deltaTime;

            // Loop swing sound
            if (!_audioSourceSwing.isPlaying)
            {
                _audioSourceSwing.Play();
                _audioSourceSwing.loop = true;
            }

            //Time to check for shake
            if (_posCheckTimer >= _posCheckInterval)
            {
                _posCheckTimer -= _posCheckInterval;

                //Distance big enough -> Shake holding controller
                var deltaMagnitude = (transform.position - _prevPos).sqrMagnitude;
                if (deltaMagnitude > _shakeDistance * _shakeDistance)
                {
                    if (_swordSlashParticle != null && !_swordSlashParticle.isEmitting)
                        _swordSlashParticle.Play();
                    _scriptFeedback.SendLongVibration(_posCheckInterval * 3, _hapticIntensity, hs);

                    // Set swing volume (louder when you swing harder)
                    _audioSourceSwing.volume = Mathf.Clamp01(deltaMagnitude * 20f);
                }
                else
                {
                    if (_swordSlashParticle != null)
                        _swordSlashParticle.Stop();

                    if (_audioSourceSwing.isPlaying)
                        _audioSourceSwing.Stop();
                }

                _prevPos = transform.position;
            }
        }

        //Cool down weapon
        if (!_isOn && _overHeating > 0)
        {
            _overHeating -= Time.deltaTime * RechargeSpeed;
            if (_overHeating <= 0)
            {
                //Reset overheating
                _isOverHeated = false;
                _overHeating = 0f;
                _actionOverheat.Invoke(false);

                //Stop overheat particles
                if (_swordOverheatParticle != null)
                    _swordOverheatParticle.SetActive(false);

                PlayIdleSound();
            }
        }

        //Heat up weapon when in use
        else if (_isOn && _overHeating < 1f)
        {
            _overHeating += Time.deltaTime * (1f / _heatFactor);
            if (_overHeating >= 1f)
            {
                //Weapon is overheated
                _isOverHeated = true;
                _isOn = false;
                _blade.SetActive(false);
                _overHeating = 1f;
                _actionOverheat.Invoke(true);

                //Activate overheat particles
                if (_swordOverheatParticle != null)
                    _swordOverheatParticle.SetActive(true);

                //Deactivate sword on particle
                if (_swordOnParticle != null)
                    _swordOnParticle.SetActive(false);

                //Deactivate slash trail
                if (_swordSlashParticle != null)
                    _swordSlashParticle.Stop();

                if (_swordTrail != null)
                    _swordTrail.Emit = false;

                // Stop idle sound
                StopIdleSound();
                _scriptAudio.PlaySFX(SoundOverheated, _audioPos);
            }
        }
    }

    private void UpdateAnimationVariables(HandSide handSide)
    {
        if (_animator == null)
            return;

        // Let animation play according to pick up state
        _animator.SetBool(_animHoldingID, PickUpObjectScript.BeingHeld);
        // Let animation play according to overheat state
        _animator.SetBool(_animCoolingID, _isOverHeated);
        // Show/hide blade when blade is actually active
        _animator.SetBool(_animBladeID, _blade.activeSelf);
        // Play trigger animation regardless of overheating
        _animator.SetFloat(_animTriggerID, _locoInstance.GetHairTrigger(handSide));
    }

    private void StopIdleSound(GameObject obj = null)
    {
        // Stop idle sound
        if (_audioSourceIdle != null && _audioSourceIdle.isPlaying)
            _audioSourceIdle.Stop();
    }

    private void ToggleEffects(bool state, Color? colorFalse = null) // Color isn't const, using nullable instead
    {
        var targetColor = state ? _colorWhilstActive : colorFalse.GetValueOrDefault(Color.clear);

        // Update all the lights on the blade and hilt
        if (_lightsSword != null)
            foreach (Light l in _lightsSword.Where(x => x != null))
                l.color = targetColor;
#if DEBUG
        else
            Debug.LogWarning("No lights set on sword!", gameObject);
#endif

        // Turn off particles if false
        if (!state)
        {
            // Play particles whilst blade is active
            ToggleParticles(false, _particlesActive);
        }

        // Update the material's emissive parts
        if (_meshRenderer == null)
            return;

        _meshRenderer.materials[_materialBladeLightsID].SetColor(_shaderID, targetColor);
        _meshRenderer.materials[_materialGripLightsID].SetColor(_shaderID, targetColor);
    }

    /// <summary>
    /// Shorthand for toggling all the particles on/off
    /// </summary>
    /// <param name="state">Play/stop the particles according to the state</param>
    void ToggleParticles(bool state, ParticleSystem[] particles)
    {
        if (_particlesActive != null && particles != null && particles.Length > 0)
        {
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null)
                    continue;

                if (state)
                    particle.Play();
                else
                    particle.Stop();
            }
        }
#if DEBUG
        else
            Debug.LogWarning("No particles set on sword!", gameObject);
#endif
    }

    void ToggleCooldownEffects(bool state)
    {
        ToggleEffects(!state, Color.red);

        // Play particle effects
        if (_particlesOverheat == null)
        {
#if DEBUG
            Debug.LogWarning("No overheat particles set on sword!");
#endif
            return;
        }

        ToggleParticles(state, _particlesOverheat);
    }

    private void TurnOnEffects(GameObject o)
    {
        ToggleEffects(true);

        // Play associated sound
        _scriptAudio.PlaySFX(SoundActivate, _audioPos);
    }

    private void TurnOffEffects(GameObject o)
    {
        ToggleEffects(false);

        // Play associated sound as well
        _scriptAudio.PlaySFX(SoundDeactivate, _audioPos);
    }

    private void BladeCollisionEnter(Collider other)
    {
#if DEBUG
        Debug.Log("Blade collision enter: " + other.name);
#endif
        if (other.tag == TagDestructible)
        {
            var destr = other.GetComponent<Script_Destructible>();
            if (destr != null)
                destr.DestroyBlock();
            else
            {
                var pickUp = other.gameObject.GetComponent<Script_PickUpEffects>() ?? other.gameObject.transform.parent.GetComponent<Script_PickUpEffects>();
                if (pickUp != null)
                    pickUp.Damage(Damage, -_blade.transform.up);
            }
        }

        if (other.tag == TagPickUp)
        {
            var destr = other.GetComponent<Script_PickUpEffects>();
            if (destr != null)
                destr.Damage(Damage, transform.forward);
            //destr.DestroyBlock();
        }
    }
}
