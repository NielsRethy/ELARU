using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Script for handling the start screen tutorial behaviour
/// </summary>
public class Script_Tutorial : MonoBehaviour
{
    /// <summary>
    /// Enumeration tracking the progress of the tutorial sequence
    /// </summary>
    public enum Progress
    {
        /// <summary>
        /// Start of the game (all game objects off)
        /// </summary>
        Default,
        /// <summary>
        /// Check if both controllers are on
        /// </summary>
        ControllerCheck,
        /// <summary>
        /// Make the player hold their controller at waist height
        /// </summary>
        SetHeight,
        /// <summary>
        /// Make the player grip their controller to set their height
        /// </summary>
        GripHeight,
        /// <summary>
        /// Teleport tutorial
        /// </summary>
        Teleport,
        /// <summary>
        /// After a slight delay, show the gun
        /// </summary>
        ShowGun,
        /// <summary>
        /// After the player picks up the gun, show the target dummy
        /// </summary>
        PickUpGun,
        /// <summary>
        /// After the player kills the target dummy, show the sword
        /// </summary>
        ShowSword,
        /// <summary>
        /// After the player picks up the sword
        /// </summary>
        PickUpSword,
        /// <summary>
        /// Load the next scene (called by the height level script)
        /// </summary>
        NextScene
    }

    private Progress _progressState = Progress.Default;
    public Progress ProgressState { set { _progressState = value; } }
    private bool _tutorialCompleted = false;
    [SerializeField]
    private bool _asyncLoad = true;

    /// <summary>
    /// Used for determining and setting the initial state of the tutorial
    /// </summary>
    public bool TutorialCompleted
    {
        get { return _tutorialCompleted; }
        set
        {
            _tutorialCompleted = value;

            //Reset progress
            _progressState = Progress.Default;

            // Turn everything off
            ToggleSpotlights(false);
            _weaponGun.gameObject.SetActive(false);
            _weaponSword.gameObject.SetActive(false);

            // Initial setup
            _textBillboard.text = "";

            //Go to next step
            Invoke("NextStep", _delayStartup);
        }
    }

    #region Game objects and components
    [Space(10)]
    [Header("Variables")]
    [SerializeField]
    private AudioSource _audioSpotlight = null;
    [SerializeField]
    private Light[] _lightsSpotlights = null;

    [SerializeField]
    private GameObject[] _weaponTargets = null;
    private Script_Destructible[] _targetScripts = null;

    [SerializeField]
    private Script_Gun _weaponGun = null;
    private Rigidbody _weaponGunRigid = null;

    [SerializeField]
    private Script_Sword _weaponSword = null;
    private Rigidbody _weaponSwordRigid = null;


    [SerializeField]
    private GameObject _billboard = null;
    [SerializeField]
    private Text _textBillboard = null;
    private Image _imageBillboardBG = null;
    [SerializeField]
    private AudioSource _audioBillboard = null;
    [SerializeField]
    private Transform _transformBillboard = null;
    [SerializeField]
    private Light[] _lightsBillboard = null;
    private float[] _lightsBillboardInitIntensity = null;
    [SerializeField]
    private Script_PickUpEffects _scriptEffectsBillboard = null;
    [SerializeField]
    private Image _billboardButtonImage = null;
    [SerializeField]
    private GameObject _billboardControllerImageObj = null;
    [SerializeField]
    private Sprite _spriteGrip = null;
    [SerializeField]
    private Sprite _spriteTouchPad = null;
    [SerializeField]
    private Sprite _spritePower = null;
    [SerializeField]
    private Sprite _spriteTrigger = null;
    [SerializeField]
    private Script_Light _scriptLightsCase = null;
    [SerializeField]
    private Script_Box _scriptBox = null;
    [SerializeField]
    private float _delayStartup = 1f;
    #endregion

    private Dictionary<Progress, string> _listText = new Dictionary<Progress, string>
    {
        { Progress.Default, "" },
        { Progress.ControllerCheck, "TURN ON\nBOTH CONTROLLERS" },
        { Progress.SetHeight, "HOLD CONTROLLERS\nAT WAIST HEIGHT" },
        { Progress.GripHeight, "PRESS BOTH\nGRIP BUTTONS" },
        { Progress.Teleport, "PRESS THE TOUCHPAD\nTO TELEPORT"},
        { Progress.ShowGun, "OPEN THE CASE AND\nGRAB THE GUN"},
        { Progress.PickUpGun, "SHOOT THE\nBOTTLES AND BRICKS"},
        { Progress.ShowSword, "GRAB THE SWORD\nIN THE CASE" },
        { Progress.PickUpSword, "DESTROY THE\nBOTTLES AND BRICKS" },
        { Progress.NextScene, "LOADING..." }
    };

    private float _timerBillboard = 0f;
    private const float TimerDelayBillboard = 1f;
    private Color _colorCrossFadeTarget = Color.red;
    private bool _crossFadeDirection = false;

    private bool _invoke = true;

    public AsyncOperation Async { get; private set; }
    private string _progress = "0";

    private Script_LocomotionBase _scriptLoco = null;
    private Script_Locomotion_TeleDash _scriptDash = null;
    private Script_TactileFeedback _scriptFeedback = null;
    [SerializeField]
    private AudioClip _ambientClip = null;

    private Script_PickUp _scriptPickUpLeft
    { get { return _scriptLoco.GetPickUpFromHand(HandSide.Left); } }
    private Script_PickUp _scriptPickUpRight
    { get { return _scriptLoco.GetPickUpFromHand(HandSide.Right); } }

    private void Start()
    {
        // Fade out the game initially (fade in after reorienting the player)
        SteamVR_Fade.View(Color.black, 0f);

        // Fetch variables
        _imageBillboardBG = _billboard.GetComponentInChildren<Image>();
        _lightsBillboardInitIntensity = _lightsBillboard.Select(l => l.intensity).ToArray();

        _weaponGunRigid = _weaponGun.GetComponent<Rigidbody>();
        if (_weaponGunRigid == null)
            Debug.LogWarning("Failed to get rigidbody on gun!");
        else //Set weaon kinematic
            _weaponGunRigid.isKinematic = true;

        _weaponSwordRigid = _weaponSword.GetComponent<Rigidbody>();
        if (_weaponSwordRigid == null)
            Debug.LogWarning("Failed to get rigidbody on sword!");
        else //Set weapon kinematic
            _weaponSwordRigid.isKinematic = true;

        // Set lock region for dash locomotion (limits how far the player can teleport)
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptDash = _scriptLoco.ScriptLocomotionDash;
        _scriptDash.LockRegion(true, _scriptLoco.CameraRig.transform.position, 15f);
        _scriptFeedback = Script_TactileFeedback.Instance;

        // Get the destructible scripts
        _targetScripts = _weaponTargets.Select(target => target.GetComponent<Script_Destructible>()).ToArray();

        // Asynchronously load the next level
        if (_asyncLoad)
            StartCoroutine(LoadSceneAsync());

        // Load game state (but don't apply the SaveData)
        var scriptSave = Script_SaveFileManager.Instance;
        scriptSave.LoadSceneData(false, false);

        // Set the tutorial state according to the static save file, triggering the start of the tutorial
        TutorialCompleted = Script_SaveFileManager.SaveData.CompletedTutorial;

        // Reorient the player (fades in when done)
        Invoke("ReorientPlayer", 1f);

        // Play ambient
        Script_AudioManager.Instance.StartAmbient(_ambientClip, 0.4f);
    }

    /// <summary>
    /// Compensate for the camera orientation by rotating the entire camera rig, making sure the player is facing the right way initially
    /// </summary>
    private void ReorientPlayer()
    {
        var angle = Vector3.Angle(Vector3.right, Camera.main.transform.forward.normalized);
        _scriptLoco.CameraRig.transform.Rotate(Vector3.up, angle);

        // Fade in the game after the player has been reoriented
        SteamVR_Fade.View(Color.clear, 2f);
    }

    /// <summary>
    /// Load the scene asynchronous
    /// </summary>
    private IEnumerator LoadSceneAsync()
    {
        // Load and assign the scene
        Async = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);

        // Disable the scene loading when async is done
        Async.allowSceneActivation = false;
        while (Async.progress < 0.9f)
            _progress = (Async.progress * 100f + 10f).ToString("0") + '%';

        // Allow the next scene to load if the lever has been pulled already
        if (_progressState == Progress.NextScene)
            Async.allowSceneActivation = true;

        // Stop the coroutine when loading is done
        while (!Async.isDone)
            yield return null;
    }

    private void Update()
    {
#if DEBUG
        if (Input.GetKeyDown(KeyCode.N))
            NextStep();
#endif

        // Show UI
        CheckProgressEnumeration();

        _colorCrossFadeTarget = _textBillboard.text.Length == 1 ? Color.green : Color.red;
        _textBillboard.color = _colorCrossFadeTarget;

        // Animation pulse background on tv screen
        if ((_timerBillboard -= Time.deltaTime) < 0f)
        {
            _crossFadeDirection = !_crossFadeDirection;
            _timerBillboard = TimerDelayBillboard;
        }

        var lerp = Mathf.Clamp(_crossFadeDirection ? 1 - _timerBillboard / TimerDelayBillboard : _timerBillboard / TimerDelayBillboard, 0f, 0.3f);
        _imageBillboardBG.color = Color.Lerp(Color.black, _colorCrossFadeTarget, lerp);

        if (_billboard != null && _billboard.activeSelf)
        {
            for (var l = 0; l < _lightsBillboard.Length; ++l)
            {
                _lightsBillboard[l].intensity = _lightsBillboardInitIntensity[l] * lerp;
                _lightsBillboard[l].color = _colorCrossFadeTarget;
            }
        }
    }

    private bool ControllersAreAtWaistHeight()
    {
        if (_scriptLoco.LeftControllerTrObj == null || _scriptLoco.RightControllerTrObj == null)
            return false;

        var leftHeight = _scriptLoco.LeftControllerTrObj.transform.position.y;
        var rightHeight = _scriptLoco.RightControllerTrObj.transform.position.y;
        var camHeight = Camera.main.transform.position.y;

        if (leftHeight < camHeight - 0.5f && rightHeight < camHeight - 0.5f)
            return true;

        return false;
    }

    /// <summary>
    /// (for the update) Disable/enable game objects according to the current progress enumeration state and player progress
    /// </summary>
    private void CheckProgressEnumeration()
    {
        // Billboard the screen in front of the player
        switch (_progressState)
        {
            case Progress.Default:
            case Progress.ControllerCheck:
            case Progress.SetHeight:
            case Progress.GripHeight:

                // Lerp the text screen around and in front of the player
                var player = Camera.main.transform;

                _transformBillboard.forward = Vector3.Lerp(
                    _transformBillboard.forward,
                    -player.forward, Time.deltaTime);

                var lerp = _transformBillboard.position;

                if (player.position.y > 1f)
                    lerp.y = Mathf.Lerp(lerp.y, player.position.y, Time.deltaTime);

                lerp.x = player.position.x + 0.5f;
                lerp.z = player.position.z;

                _transformBillboard.position = lerp;

                break;
            case Progress.ShowGun:
                break;
        }

        // Invoke according to enum
        if (!_invoke)
            return;

        var performNextStep = false;
        switch (_progressState)
        {
            // Trigger the next step if both controllers are on
            case Progress.ControllerCheck:

                if (BothControllersExist())
                    performNextStep = true;

                // Keep trying to turn off grabbing
                if (_scriptPickUpLeft != null)
                    _scriptPickUpLeft.enabled = false;
                if (_scriptPickUpRight != null)
                    _scriptPickUpRight.enabled = false;

                break;

            // Make the player hold their controllers are waist height
            case Progress.SetHeight:
                if (_scriptLoco.LeftControllerTrObj == null || _scriptLoco.RightControllerTrObj == null)
                    break;

                if (ControllersAreAtWaistHeight())
                    performNextStep = true;

                break;

            // Perform the next step if both controllers are gripped
            case Progress.GripHeight:
                if (_scriptLoco.LeftController != null && _scriptLoco.LeftController.GetPress(_scriptLoco.GripButton) &&
                    _scriptLoco.RightController != null && _scriptLoco.RightController.GetPress(_scriptLoco.GripButton))
                {
                    Script_SaveFileManager.SaveData.PlayerWaistHeight = _scriptLoco.LeftControllerTrObj.transform.position.y;
                    Script_SaveFileManager.SaveData.PlayerHeight = Camera.main.transform.position.y;
                    Script_WeaponDocking.Instance.UpdateDockingHeight();
                    performNextStep = true;
                }
                // Turn back a step if controllers are not at waist height anymore
                else if (!ControllersAreAtWaistHeight())
                {
                    SetBillboardText(_listText[Progress.SetHeight]);
                    _progressState = Progress.SetHeight;
                }

                break;

            // Teleport tutorial, progress when player moves
            case Progress.Teleport:
                if (_scriptLoco.ScriptCollisionCheck.PlayerHasMoved &&
                    _scriptLoco.GetPressUp(_scriptLoco.TouchPad, HandSide.Left) ||
                    _scriptLoco.GetPressUp(_scriptLoco.TouchPad, HandSide.Right))
                    performNextStep = true;

                break;

            case Progress.ShowGun:
                // Trigger the next step when the gun is picked up
                if (_weaponGun.HasBeenFound)
                {
                    _weaponGunRigid.isKinematic = false;
                    performNextStep = true;
                }

                break;

            // Trigger the next step when all bottles have been destroyed
            case Progress.PickUpGun:
                if (_targetScripts.All(target => target.Destroyed))
                    performNextStep = true;

                break;

            // Trigger the next step when the sword has been picked up
            case Progress.ShowSword:
                if (_weaponSword.HasBeenFound)
                {
                    _weaponSwordRigid.isKinematic = false;
                    performNextStep = true;
                }

                break;

            // Trigger the next step when all bricks are destroyed with the sword
            case Progress.PickUpSword:
                if (_targetScripts.All(target => target.Destroyed))
                    performNextStep = true;

                break;

            // Show loading text if the next scene is still loading
            case Progress.NextScene:
                if (_asyncLoad)
                    if (Async.progress >= 0.9f)
                        Invoke("AllowSceneActivation", 1f);

                _textBillboard.text = _listText[Progress.NextScene] + '\n' +
                    (_asyncLoad ? (Async.progress <= 0.9f ? _progress : "DONE!") : "DISABLED!");

                break;
        }

        if (performNextStep)
        {
            SetBillboardText();

            if (_invoke)
                Invoke("NextStep", 1f);

            _invoke = false;
        }
    }

    // Used for Invoke in the "CheckProgressEnumeration" method
    public void AllowSceneActivation()
    {
        Async.allowSceneActivation = true;
    }

    private bool BothControllersExist()
    {
        return _scriptLoco.LeftControllerTrObj != null && _scriptLoco.RightControllerTrObj != null;
    }

    /// <summary>
    /// Disable/enable game objects according to the current progress enumeration state.
    /// </summary>
    public void NextStep()
    {
        _progressState++;

        if (_progressState < Progress.NextScene)
            Invoke("DelaySetText", 1f);
        else
            _audioBillboard.Play();

        //Update scene for current progress state
        switch (_progressState)
        {
            case Progress.ControllerCheck:
                _scriptEffectsBillboard.enabled = false;
                _scriptDash.enabled = false;
                ToggleSpotlights(true);

                // Show power button location help
                _billboardButtonImage.sprite = _spritePower;

                _scriptLightsCase.SetLights(false);
                _scriptBox.ToggleParticle(false);

                break;

            case Progress.GripHeight:
                // Viberate the controller to let the player know they can do something
                //_scriptFeedback.SendLongVibration(1f, 2f, HandSide.Left);
                //_scriptFeedback.SendLongVibration(1f, 2f, HandSide.Right);
                // Show grip button location help
                _billboardButtonImage.sprite = _spriteGrip;
                break;

            case Progress.Teleport:
                _scriptDash.enabled = true;
                // Show touchpad location help
                _billboardButtonImage.sprite = _spriteTouchPad;

                break;

            case Progress.ShowGun:
                _scriptPickUpLeft.enabled = true;
                _scriptPickUpRight.enabled = true;

                _scriptEffectsBillboard.enabled = true;

                _weaponGun.gameObject.SetActive(true);
                ToggleSpotlights(true);

                _scriptLightsCase.SetLights(true);
                _scriptBox.ToggleParticle(true);

                // Show grip button location help
                _billboardButtonImage.sprite = _spriteGrip;

                break;

            case Progress.PickUpGun:
                // Show next
                //_weaponGunTarget.SetActive(true);
                ToggleSpotlights(false);
                _scriptLightsCase.SetLights(false);

                // Show grip button location help
                _billboardButtonImage.sprite = _spriteTrigger;

                break;

            case Progress.ShowSword:
                // TODO: Docking tutorial?
                _weaponGun.gameObject.SetActive(false);
                DropWeapons();
                // Show next
                _weaponSword.gameObject.SetActive(true);
                ToggleSpotlights(true);

                _scriptBox.enabled = true;
                _scriptBox.Reset();
                _scriptLightsCase.SetLights(true);

                // Show grip button location help
                _billboardButtonImage.sprite = _spriteGrip;

                break;

            case Progress.PickUpSword:
                // Hide previous
                ToggleSpotlights(false);

                _scriptLightsCase.SetLights(false);

                // Show grip button location help
                _billboardButtonImage.sprite = _spriteTrigger;

                // Respawn destructibles for sword
                foreach (var target in _targetScripts)
                {
                    target.gameObject.SetActive(true);
                    target.ResetContent();
                }

                break;

            case Progress.NextScene:
                break;
        }

        _invoke = true;
    }

    /// <summary>
    /// Shorthand for forcing the weapons out of the player's hands
    /// </summary>
    private void DropWeapons()
    {
        //Drop left hand
        var trObj = _scriptLoco.LeftControllerTrObj;
        if (trObj != null)
        {
            var pu = trObj.gameObject.GetComponent<Script_PickUp>();
            if (pu != null)
                pu.Drop();
        }

        //Drop right hand
        trObj = _scriptLoco.RightControllerTrObj;
        if (trObj != null)
        {
            var pu = trObj.gameObject.GetComponent<Script_PickUp>();
            if (pu != null)
                pu.Drop();
        }
    }

    /// <summary>
    /// Shorthand for turning on/off all the spotlights
    /// </summary>
    private void ToggleSpotlights(bool value)
    {
        if (_lightsSpotlights == null)
            return;

        if (_audioSpotlight.gameObject.activeSelf)
            _audioSpotlight.Play();

        foreach (var spotlight in _lightsSpotlights)
            spotlight.gameObject.SetActive(value);
    }

    private void DelaySetText()
    {
        SetBillboardText(_listText[_progressState]);
    }

    /// <summary>
    /// Set the text on the billboard, leave the parameter blank to set a checkmark instead of text
    /// </summary>
    /// <param name="text">Text to display on the billboard</param>
    private void SetBillboardText(string text = "")
    {
        var noText = text.Length <= 0;

        // Show a checkmark if no text was passed
        if (_textBillboard != null)
            _textBillboard.text = noText ? "✓" : text;

        // Show or hide the controller tutorial images according to the text content
        if (_billboardControllerImageObj != null)
            _billboardControllerImageObj.SetActive(!noText);
        if (_billboardButtonImage != null)
            _billboardButtonImage.gameObject.SetActive(!noText);

        // Regulate pitch according to text content
        if (_audioBillboard != null)
        {
            _audioBillboard.pitch = text.Length <= 0 ? 0.75f : 1f;
            _audioBillboard.Play();
        }
    }
}
