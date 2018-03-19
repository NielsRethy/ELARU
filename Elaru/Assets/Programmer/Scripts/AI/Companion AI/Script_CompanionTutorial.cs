using System;
using System.Collections;
using UnityEngine;

public class Script_CompanionTutorial : MonoBehaviour
{
    [SerializeField]
    private float _heightChangeSpeed = 0.1f;
    [SerializeField]
    private Transform _height = null;
    [SerializeField]
    private Material _matCompanionScreen = null;
    [SerializeField]
    private Texture _startTexture = null;
    [SerializeField]
    private Texture _normalTexture = null;
    [SerializeField]
    private Texture _errorTexture = null;
    [SerializeField]
    private Texture _elaruTexture = null;
    [SerializeField]
    private Texture _blackTexture = null;

    private const string TextureID = "_AlphaTex";

    [SerializeField]
    private GameObject _oldRobot = null;
    [SerializeField]
    private GameObject _newRobot;
    [SerializeField]
    private float _animationWait = 3.0f;
    [SerializeField]
    private Transform _secondPoint = null;
    [SerializeField]
    private bool _firstRobot = false;

    private Animator _animation;
    private Script_SplineController _splineController;

    [SerializeField]
    private GameObject _miniMap = null;

    public bool IsFixed { get; set; }
    private bool _rotate;
    private bool _activeEyes;
    private int maxCaroun = 0;
    private bool stopError = false;
    private bool _goUp = true;


    void Start()
    {
        if (_firstRobot)
        {
            Script_AudioManager.Instance.ForceStartIntroMusic();

            //Cache components
            _splineController = GetComponent<Script_SplineController>();
            _splineController.AutoStart = false;

            if (_matCompanionScreen != null)
            {
                _matCompanionScreen.mainTexture = _blackTexture;
                _matCompanionScreen.SetTexture("_AlphaTex", _blackTexture);
            }

            if (_miniMap != null)
                _miniMap.SetActive(false);

            _newRobot.SetActive(false);
            _oldRobot.SetActive(true);
            _animation = _newRobot.GetComponent<Animator>();
        }
        else
        {
            IsFixed = true;
            _splineController = GetComponent<Script_SplineController>();
            _splineController.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = true;
        }
    }

    void Update()
    {
        if (_firstRobot)
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.B))
            {
                IsFixed = !IsFixed;
            }
#endif

            if (IsFixed && !_activeEyes && maxCaroun < 50)
            {
                maxCaroun++;
                StartCoroutine(BlinkEyes());
                //_activateOnes = true;
            }
            else if (IsFixed && !_activeEyes && !stopError)
            {
                StartCoroutine(Error());
                stopError = true;
            }

            if (IsFixed && _activeEyes && _goUp)
            {
                if (_oldRobot.activeSelf)
                {
                    _matCompanionScreen.mainTexture = _startTexture;
                    _matCompanionScreen.SetTexture(TextureID, _startTexture);
                    _oldRobot.SetActive(false);
                    _newRobot.SetActive(true);
                }
                _matCompanionScreen.mainTexture = _startTexture;
                _matCompanionScreen.SetTexture(TextureID, _startTexture);

                //Move to just above
                var newPos = transform.localPosition;
                newPos.y = _height.position.y;
                transform.localPosition = Vector3.Lerp(transform.localPosition, newPos, _heightChangeSpeed);

                //Check if position above is reached
                if (transform.localPosition.y > _height.position.y - 0.1f)
                {
                    _animation.SetBool("Tutorial", true);
                    Invoke("StartFlying", _animationWait);
                    // _splineController.StartFollow = true;
                    _goUp = false;
                    //_rotate = true;
                }
            }
            if (_rotate)
            {
                Vector3 lTargetDir = _secondPoint.position - transform.position;
                lTargetDir.y = 0.0f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lTargetDir), Time.deltaTime * 100);

                if ((transform.rotation.eulerAngles - Quaternion.LookRotation(lTargetDir).eulerAngles).magnitude < 0.4f * 0.4f)
                {
                    _splineController.StartFollow = true;
                    _animation.SetBool("Fly", true);
                    _rotate = false;
                }
            }
            //if (IsFixed)
            //{
            //    _matCompanionScreen.mainTexture = _startTexture;
            //    _matCompanionScreen.SetTexture("_AlphaTex", _startTexture);
            //}
        }
    }


    private void StartFlying()
    {
        _rotate = true;

    }

    public void ShowMiniMap(bool show)
    {
        if (_miniMap != null)
            _miniMap.SetActive(show);
    }

    private IEnumerator BlinkEyes()
    {
        _matCompanionScreen.mainTexture = _normalTexture;
        _matCompanionScreen.SetTexture(TextureID, _normalTexture);
        yield return new WaitForSeconds(0.05f);
        _matCompanionScreen.mainTexture = _blackTexture;
        _matCompanionScreen.SetTexture(TextureID, _blackTexture);
        yield return new WaitForSeconds(0.1f);
        _matCompanionScreen.mainTexture = _normalTexture;
        _matCompanionScreen.SetTexture(TextureID, _normalTexture);
        yield return new WaitForSeconds(0.05f);
        _matCompanionScreen.mainTexture = _blackTexture;
        _matCompanionScreen.SetTexture(TextureID, _blackTexture);
        yield return new WaitForSeconds(0.1f);
        _matCompanionScreen.mainTexture = _normalTexture;
        _matCompanionScreen.SetTexture(TextureID, _normalTexture);
        yield return new WaitForSeconds(0.05f);
        _matCompanionScreen.mainTexture = _blackTexture;
        _matCompanionScreen.SetTexture(TextureID, _blackTexture);
        yield return new WaitForSeconds(0.1f);
        _matCompanionScreen.mainTexture = _normalTexture;
        _matCompanionScreen.SetTexture(TextureID, _normalTexture);
        yield return new WaitForSeconds(0.05f);
        _matCompanionScreen.mainTexture = _blackTexture;
        _matCompanionScreen.SetTexture(TextureID, _blackTexture);
    }

    private IEnumerator Error()
    {
        yield return new WaitForSeconds(0.4f);
        _matCompanionScreen.mainTexture = _errorTexture;
        _matCompanionScreen.SetTexture(TextureID, _errorTexture);
        yield return new WaitForSeconds(0.5f);
        _matCompanionScreen.mainTexture = _blackTexture;
        _matCompanionScreen.SetTexture(TextureID, _blackTexture);
        yield return new WaitForSeconds(0.5f);
        _matCompanionScreen.mainTexture = _startTexture;
        _matCompanionScreen.SetTexture(TextureID, _startTexture);
        yield return new WaitForSeconds(1f);
        _activeEyes = true;
    }
}
