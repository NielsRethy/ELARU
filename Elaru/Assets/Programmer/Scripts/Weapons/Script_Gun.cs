using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_Gun : Script_Weapon
{
    //Visual mod types
    public enum ModificationType
    {
        FlashLight,
        Sight,
        Laser,
        None
    }

    //Different types of gun
    [Serializable]
    public enum GunType
    {
        Gun,
        Light,
        Sound,
        None,
    }

    [SerializeField]
    private GunType _activeType;
    public GunType Type
    {
        get { return _activeType; }
        set { _activeType = value; UpdateGunTypeVisuals(); }
    }

    //Order -> Normal damage, light, sound
    [SerializeField]
    private List<GameObject> _gunTypeVisuals;

    public ModificationType VisualModType { get; set; }

    //Place where shoot raycast shoots from
    [SerializeField]
    private Transform _barrelEnd = null;

    //Order -> Flashlight, sight, laser
    [SerializeField]
    private List<GameObject> _visualMods = new List<GameObject>();

    private bool _isLaserActive = true;
    private LineRenderer _laserLine = null;
    private Light _flashLight = null;

    //Gun shooting rate
    [SerializeField]
    private float _shootInterval = .25f;

    private float _shootTimer = 0f;

    private float _baseRange = 20f;

    //only update laser point if necessary
    private bool _laserIdle = true;

    [SerializeField]
    private ushort _hapticIntensity = 3000;

    [Header("Effects")]
    [SerializeField]
    private GameObject _objGun = null;
    private Vector3 _initLocalPosition = Vector3.zero;
    private static float GunShotOffset = 0.05f;
    private Quaternion _initLocalRotation = Quaternion.identity;
    private static float GunShotRotateOffsetZ = -7f;
    private bool _gunResetPosition = false;
    [SerializeField]
    private LineRenderer _lineRenderer = null;
    private Transform _initLineRenderParent = null;
    private float _initLineRenderPosition = 0f;
    [SerializeField]
    private Light _lightMuzzle = null;
    [SerializeField]
    private AudioSource _audioSource = null;
    private Script_AudioManager _scriptAudio = null;
    [SerializeField]
    private Light[] _lightsGun = null;
    [SerializeField]
    private MeshRenderer[] _emissiveRenderers = null;
    private Material _materialGunBase = null;
    private Material _materialGunBarrel = null;
    private const string ShaderID = "_EmissionColor";

    [SerializeField]
    private float _projectileExplosionTime = 2f;
    private Script_GunProjectile _projectilePrefab = null;
    private List<Script_GunProjectile> _projectilePool = new List<Script_GunProjectile>();

    private Script_LocomotionBase _locoInstance = null;

    [SerializeField]
    private GameObject _muzzleFlashParticles = null;
    [SerializeField]
    private GameObject _hitParticlePrefab = null;
    private List<GameObject> _hitParticlePool = new List<GameObject>();

    private const string GunFireWeak = "GunFireWeak";
    private const string GunFireMedium = "GunFireMedium";
    private const string GunFireStrong = "GunFireStrong";
    private const string ProjectileFire = "GunFireProjectile";
    private const string GunCooling = "WeaponCooling";
    private const string GunEmpty = "GunEmpty";

    public void Start()
    {
        //Check if gun can be held
#if DEBUG
        if (!PickUpObjectScript)
            Debug.LogError("Sword has no pick up object script");
#endif

        //Get stuff for effects
        _scriptAudio = Script_AudioManager.Instance;
        _materialGunBase = _emissiveRenderers[0].material;
        _materialGunBarrel = _emissiveRenderers[0].materials[2];

        //Turn off effects
        ToggleEffects(false);
        _initLocalPosition = _objGun.transform.localPosition;
        _initLocalRotation = _objGun.transform.localRotation;

        //Init effect values
        _initLineRenderParent = _lineRenderer.transform.parent;
        _lineRenderer.transform.SetParent(null);
        _initLineRenderPosition = _lineRenderer.GetPosition(0).z;
        UpdateLightAndMaterialColors();
        _lineRenderer.enabled = false;

        //Turn off all visual mods
        VisualModType = ModificationType.None;
        ShowVisualMod(false);

        _laserLine = _visualMods[(int)ModificationType.Laser].GetComponent<LineRenderer>();
        _flashLight = _visualMods[(int)ModificationType.FlashLight].GetComponentInChildren<Light>();
        ShowVisualModEffects(true);

        //Set default gunt type
        if (Type == GunType.None)
            Type = GunType.Gun;

        UpdateGunTypeVisuals();

        _shootTimer = _shootInterval;

        //Load projectile prefab
        _projectilePrefab = Resources.Load<Script_GunProjectile>("Prefabs/GunProjectile");

        Script_WeaponManager.Instance.RegisterWeapon(this);

        _locoInstance = Script_LocomotionBase.Instance;

        _muzzleFlashParticles.SetActive(false);
    }

    private void Update()
    {
        //TODO: remove testing keys
#if DEBUG
        if (Input.GetKey(KeyCode.T))
            Shoot();
        if (Input.GetKeyUp(KeyCode.T))
            _shootTimer = _shootInterval;
#endif

        //Check for press / release trigger
        var trg = _locoInstance.TriggerButton;
        var hs = PickUpObjectScript.ControlHandSide;
        if (_locoInstance.GetPress(trg, hs))
        {
            Shoot();
            // Play empty clip sound
            if (_isOverHeated && _locoInstance.GetPressDown(trg, hs))
                _scriptAudio.PlaySFX(GunEmpty, _audioSource.transform.position);
        }

        if (_locoInstance.GetPressUp(trg, hs))
            _shootTimer = _shootInterval; //Reset shoot timer

        //Update laser to point in correct aim direction
        if (VisualModType == ModificationType.Laser && _isLaserActive)
            LaserUpdate();

        //Teleport to free dock / location if too far from player
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

            ShowVisualModEffects(false);
        }

        UpdateLightAndMaterialColors();

        //Cool down gun
        if (_overHeating > 0)
        {
            _overHeating -= Time.deltaTime * RechargeSpeed;

            if (_overHeating <= 0)
            {
                //Cooled down
                _isOverHeated = false;
                _overHeating = 0f;
            }
        }

        // Reset gun jolt position and move line renderer positions
        if (_gunResetPosition)
            LerpResetGunPosition();
    }

    public void ShowVisualMod(bool value)
    {
        //Disable all mods
        foreach (var m in _visualMods)
            m.SetActive(false);

        if (VisualModType == ModificationType.None)
            return;

        _visualMods[(int)VisualModType].SetActive(value);
    }

    private void UpdateGunTypeVisuals()
    {
        foreach (var v in _gunTypeVisuals)
            v.SetActive(false);

        if (_activeType == GunType.None)
            return;

        _gunTypeVisuals[(int)_activeType].SetActive(true);
    }

    private void Shoot()
    {
        //Can't shoot when overheated
        if (_isOverHeated)
            return;

        //Shoot in interval
        _shootTimer += Time.deltaTime;
        if (_shootTimer < _shootInterval)
            return;
        _shootTimer = 0f;

        PlayFireSound();

        //Heat up when shooting
        if (_overHeating < 1f)
        {
            if (Type == GunType.Gun)
                _overHeating += _heatFactor;
            //Sound and light gun heat up faster
            else if (Type == GunType.Sound || Type == GunType.Light)
                _overHeating += _heatFactor * 5;

            if (_overHeating >= 1f)
            {
                _isOverHeated = true;
                // Play overheating sound
                _scriptAudio.PlaySFX(GunCooling, _audioSource);
            }
        }

        //Muzzle flash
        if (_muzzleFlashParticles != null)
        {
            _muzzleFlashParticles.SetActive(false);
            _muzzleFlashParticles.SetActive(true);
        }

        // Play effects
        ToggleEffects(true);
        Invoke("DisableEffects", 0.05f);

        //Call shoot method depending on type
        switch (Type)
        {
            case GunType.Gun:
                GunShoot();
                break;
            case GunType.Sound:
                SoundShoot();
                break;
            case GunType.Light:
                LightShoot();
                break;
        }

        //Vibrate controller
        Script_TactileFeedback.Instance.SendLongVibration(_shootInterval * 2, _hapticIntensity, PickUpObjectScript.ControlHandSide);
    }

    private void UpdateLightAndMaterialColors()
    {
        var colorGreenRed = Color.Lerp(Color.green, Color.red, _isOverHeated ? 1f : Mathf.Clamp(_overHeating, 0f, 1f));

        // Update each light's color
        if (_lightsGun != null)
            foreach (var l in _lightsGun)
                l.color = colorGreenRed;
#if DEBUG
        else
            Debug.LogWarning("No lights set on gun!");
#endif

        // Update the base emissive color
        if (_materialGunBase != null)
            _materialGunBase.SetColor(ShaderID, colorGreenRed);
#if DEBUG
        else
            Debug.LogWarning("No base material found on gun");
#endif

        // Update the gun muzzle emissive color (overheating effect)
        if (_materialGunBarrel != null)
            _materialGunBarrel.SetColor(ShaderID, Color.Lerp(Color.black, Color.red, _overHeating));
#if DEBUG
        else
            Debug.LogWarning("No gun barrel material found on gun");
#endif

        // Update additional lights
        _emissiveRenderers[1].material.SetColor(ShaderID, colorGreenRed);
    }

    public void ToggleEffects(bool state)
    {
        // Effects
        if (Type == GunType.Gun)
        {
            if (_lineRenderer != null)
                _lineRenderer.enabled = state;
#if DEBUG
            else
                Debug.LogWarning("No line renderer set!");
#endif

            if (_lightMuzzle != null)
                _lightMuzzle.enabled = state;
#if DEBUG
            else
                Debug.LogWarning("No ligt for muzzle flash set!");
#endif
        }

        // Jolt gun backwards if state is true
        if (state)
        {
            var nwPos = _objGun.transform.localPosition;
            nwPos.x = _initLocalPosition.x + GunShotOffset;
            _objGun.transform.localPosition = nwPos;
            _objGun.transform.localRotation = Quaternion.Euler(_initLocalRotation.x, _initLocalRotation.y, GunShotRotateOffsetZ);
            _gunResetPosition = true;
            // Set line renderer
            if (_lineRenderer != null)
            {
                _lineRenderer.transform.position = _initLineRenderParent.position;
                _lineRenderer.transform.rotation = _initLineRenderParent.rotation;
                _lineRenderer.SetPosition(0, new Vector3(0f, 0f, _initLineRenderPosition));
            }
        }
    }

    private void PlayFireSound()
    {
        // Choose sound according to type and damage
        string sound = "";
        switch (_activeType)
        {
            case GunType.Gun:
                sound = Damage <= 10f ? GunFireWeak : (Damage < 20f ? GunFireMedium : GunFireStrong);
                break;

            case GunType.Light:
            case GunType.Sound:
                sound = ProjectileFire;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        // Play gun shot sound
        _scriptAudio.PlaySFX(sound, _audioSource.transform.position);
    }

    private void LerpResetGunPosition()
    {
        _gunResetPosition = _objGun.transform.localPosition.x >= _initLocalPosition.x;
        if (!_gunResetPosition)
            return;

        // Move position
        var newX = Mathf.Lerp(_objGun.transform.localPosition.x, _initLocalPosition.x, Time.deltaTime * 10f);
        var nwPos = _objGun.transform.localPosition;
        nwPos.x = newX;
        _objGun.transform.localPosition = nwPos;
        // Move rotation
        _objGun.transform.localRotation = Quaternion.Lerp(_objGun.transform.localRotation, _initLocalRotation, Time.deltaTime * 10f);
    }

    private void DisableEffects()
    {
        ToggleEffects(false);
    }

    private void GunShoot()
    {
        //Check for hit
        RaycastHit hit;
        if (Physics.Raycast(_barrelEnd.position, _barrelEnd.forward, out hit, _baseRange))
        {
            //Show hit particle
            if (_hitParticlePrefab != null)
            {
                var hitEffect = GetFreeHitParticle();
                hitEffect.transform.position = hit.point - _barrelEnd.forward * .1f;
                StartCoroutine(DeactivateObject(hitEffect, .5f));
            }

            //Disable cameras that are shot
            if (hit.collider.CompareTag("DisableCamera"))
                hit.collider.gameObject.GetComponent<Script_CameraControl>().TurnOffCamera();

            //Destroy destructibles when shot
            else if (hit.collider.CompareTag("Destructible"))
            {
                var destr = hit.collider.gameObject.GetComponent<Script_Destructible>();
                if (destr != null)
                    destr.DestroyBlock();
                else
                {
                    var pickUp = hit.collider.gameObject.GetComponent<Script_PickUpEffects>() ?? hit.collider.gameObject.transform.parent.GetComponent<Script_PickUpEffects>();
                    if (pickUp != null)
                        pickUp.Damage(Damage, _barrelEnd.forward);
                }
            }

            //Damage enemy if hit one
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemies"))
                return;

            var enemyScript = hit.transform.GetComponent<Script_Limbs>();
            if (enemyScript != null)
                enemyScript.Attacked(Damage);
            else
            {
                var dummy = hit.transform.gameObject.GetComponent<Script_DummyAI>();
                if (dummy != null)
                    dummy.DealDamage((int)Damage);
            }
        }
    }

    private void SoundShoot()
    {
        //Get a sound projectile and shoot it
        var p = GetFreeProjectile();
        p.Type = Script_GunProjectile.ProjectileType.Sound;
        p.ExplosionRange = Damage / 2f; //Damage == range for sound gun
        p.Shoot(_barrelEnd.position, _barrelEnd.forward * _baseRange / 2f * RangeFactor * 1.5f, _projectileExplosionTime);
    }

    private void LightShoot()
    {
        //Get a light projectile and shoot it
        var p = GetFreeProjectile();
        p.Type = Script_GunProjectile.ProjectileType.Light;
        p.ExplosionRange = Damage / 2f; //Damage == range for ligt gun
        p.Shoot(_barrelEnd.position, _barrelEnd.forward * _baseRange / 2f * RangeFactor * 1.5f, _projectileExplosionTime);
    }

    private Script_GunProjectile GetFreeProjectile()
    {
        //Look in pool for available projectiles
        foreach (var p in _projectilePool)
        {
            if (!p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(true);
                return p;
            }
        }

        //Create new projectile and add it to pool if none are available
        var pj = Instantiate(_projectilePrefab);
        _projectilePool.Add(pj);
        return pj;
    }

    private void LaserUpdate()
    {
        //Check where hit
        RaycastHit hit;
        if (Physics.Raycast(_barrelEnd.position, _barrelEnd.forward, out hit, _baseRange * RangeFactor))
        {
            //Point to hit position
            var pos = _visualMods[(int)ModificationType.Laser].transform.worldToLocalMatrix.MultiplyPoint(hit.point);
            _laserLine.SetPosition(1, pos);
            _laserIdle = false;
        }
        //Not hit anything
        else if (!_laserIdle)
        {
            //Point to max range
            _laserIdle = true;
            _laserLine.SetPosition(1, _visualMods[(int)ModificationType.Laser].transform.worldToLocalMatrix.MultiplyPoint(_barrelEnd.position + _baseRange * _barrelEnd.forward));
        }
    }

    public void ShowVisualModEffects(bool value)
    {
        if (VisualModType == ModificationType.Laser)
            _isLaserActive = value;
        _laserLine.enabled = value;
        _flashLight.enabled = value;
    }

    private GameObject GetFreeHitParticle()
    {
        foreach (var hp in _hitParticlePool)
        {
            if (!hp.activeSelf)
            {
                hp.SetActive(true);
                return hp;
            }
        }

        var newHit = Instantiate(_hitParticlePrefab);
        _hitParticlePool.Add(newHit);
        return newHit;
    }

    private IEnumerator DeactivateObject(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        obj.SetActive(false);
    }
}
