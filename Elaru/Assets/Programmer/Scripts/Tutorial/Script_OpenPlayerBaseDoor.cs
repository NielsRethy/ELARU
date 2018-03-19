using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Script_OpenPlayerBaseDoor : MonoBehaviour
{
    private Animator _animator = null;
    private Script_Light _lightScript = null;
    private const string AnimationParamName = "Direction";

    public bool Active = false;
    private Transform _player = null;

    [SerializeField]
    private float _openDistance = 11f;
    private Script_AudioManager _audioScript = null;

    public const string OpenSound = "BaseDoorOpen";
    public const string CloseSound = "BaseDoorClose";
    private AudioSource _audioSource = null;
    private AudioClip _clipOpen = null;
    private AudioClip _clipClose = null;

    private bool _stayOpenTillClosed = false;
    private float _waitTimer = 0f;
    private const float DelayTimer = 2f;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _lightScript = GetComponent<Script_Light>();
        _audioScript = Script_AudioManager.Instance;
        _audioSource = GetComponent<AudioSource>();

        if (Active)
        {
            Active = true;
            _lightScript.SetLights(true);
        }

        _clipOpen = Script_AudioManager.SoundEffectsArray.GetAudioClips(OpenSound)[0];
        _clipClose = Script_AudioManager.SoundEffectsArray.GetAudioClips(CloseSound)[0];
    }

    private void Update()
    {
        if (Active && _player != null && _waitTimer <= 0f)
        {
            var pos = _player.position;
            // Debug.Log((pos - transform.position).sqrMagnitude);
            if ((pos - transform.position).sqrMagnitude < _openDistance)
            {
                if (_stayOpenTillClosed)
                    return;

                AnimationOpen(true);
                _waitTimer = DelayTimer;
            }
            else
            {
                AnimationOpen(false);
                _stayOpenTillClosed = false;

                _player = null;
                _waitTimer = DelayTimer;
            }
        }

        if (_waitTimer > 0f)
            _waitTimer -= Time.deltaTime;
    }

    private void AnimationOpen(bool state)
    {
        if (state == _animator.GetFloat(AnimationParamName) > 0 ? true : false)
            return;

        // Reverse animation
        _animator.SetFloat(AnimationParamName, state ? 1 : -1);

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        //var time = _audioSource.time;
        //_audioSource.Stop();

        // Animation needs to close
        if (stateInfo.normalizedTime > 1f)
        {
            _animator.Play("Open", 0, 1f);
            _audioSource.clip = _clipClose;
        }
        // Animation needs to open
        else if (stateInfo.normalizedTime < 0f)
        {
            _animator.Play("Open", 0, 0f);
            _audioSource.clip = _clipOpen;
        }
        else
        {
            _animator.Play("Open", 0, stateInfo.normalizedTime);
            _audioSource.clip = state ? _clipOpen : _clipClose;
            _audioSource.time = stateInfo.normalizedTime % _audioSource.clip.length;
        }
        _audioSource.Play();

        // Play or interrupt sound
        //if (!_audioSource.isPlaying)
        //_audioSource.Play();
    }

    public void OnTriggerStay(Collider other)
    {
        //if (Active)
        //    _animator.SetBool(AnimationOpen, true);
        if (_player != null || !other.CompareTag("Player"))
            return;

        _player = other.transform;
        _waitTimer = 0f;
    }

    public void OpenDoor()
    {
        // Turn on door and update it's lights to reflect it's state
        Active = true;
        _lightScript.SetLights(true);
        _stayOpenTillClosed = true;

        AnimationOpen(true);
    }
}
