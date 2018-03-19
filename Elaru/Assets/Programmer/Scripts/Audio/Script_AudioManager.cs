using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class Script_AudioManager : Script_Singleton<Script_AudioManager>
{
    private const float PitchOffset = 0.08f;

    private float _pitch = 1;

    private GameObject _strayParent = null;
    // Cache
    private AudioSource _targetSource = null;
    private AudioClip _targetClip = null;

    #region Adpative Audio Variables
    private List<AudioSource> _sourceMusic = null;
    private AudioSource _sourceAmbient = null;
    private AudioSource[] _sourcesInScene = null;

    private const string PathClipMusic = "Sounds/Music/Music_";
    private const string PathClipAmbient = "Sounds/Ambient/Ambient_";

    private const string PathClipMusicSneaking = "Sneaking";
    private AudioClip _clipMusicSneaking = null;
    private const string PathClipMusicTension = "Tension";
    private AudioClip _clipMusicTension = null;
    private const string PathClipMusicBase = "PlayerBase";
    private AudioClip _clipMusicBase = null;
    private const string PathClipMusicIntro = "Intro";
    private AudioClip _clipMusicIntro = null;

    private AudioClip _clipAmbientBase = null;
    private const string PathClipAmbientCity = "City";
    private AudioClip _clipAmbientCity = null;

    private Dictionary<AudioClip, int> _bpmDictionary = null;
    private const int BpmMusicSneaking = 120;
    private const int BpmMusicIntro = 118;

    private const float CrossFadeSpeed = 0.35f;

    private const string SoundCoastClear = "CoastClear";
    private AudioSource _sourceSFXCoastClear = null;
    private bool _playCoastClearSound = false;
    private bool _fading = false;

    private const float UpdateDelay = 0.5f;
    private float _updateDelayTimer = 0f;
    private Script_ManagerEnemy _scriptEnemies = null;
    private const string EnemyLayer = "Enemies";
    private LayerMask _maskEnemiesOnly = -1;
    private const string EnemyTag = "Enemy";

    private Coroutine _coroutineCrossFade = null;
    private Coroutine _coroutineCrossFadeAmbient = null;
    private Collider[] _enemyColliders = null;
    private int _enemiesInRange = 0;
    private int _enemiesSearching = 0;
    private int _enemiesAlarmed = 0;
    private const float MaxSearchTime = 4f;
    private float _searchTimer = 0f;
    private const float MuteTime = 3f;
    private float _muteTimer = 0f;

    private const int EnemyOverlapRange = 40;
    private Transform _player = null;

    private enum MusicState
    {
        Mute,
        Overwrite,
        EnemiesAreNear,
        EnemiesAreSearching,
        EnemiesAreAlarmed
    }
    private MusicState _musicState = MusicState.Mute;
    #endregion

    private enum ClipType
    {
        Single,
        Random
    }

    private Dictionary<int, int> _dictionaryIndices = new Dictionary<int, int>();
    private List<AudioSource> _listOfStrayAudioSources = new List<AudioSource>();

    public enum SoundType
    {
        Music,
        Ambient,
        SFX_2D,
        SFX_3D,
    }

    private AudioMixerGroup _mixerGroupMusic = null;
    private AudioMixerGroup _mixerGroupAmbient = null;
    private AudioMixerGroup _mixerGroupSFX = null;

    private AudioMixer _master = null;
    private const string PathMaster = "Sounds/Mixer_GameMaster";
    private const string SubPathMusic = "Music";
    private const string SubPathAmbient = "Ambient";
    private const string SubPathSfx = "SFX";

    private const string PathSoundsArray = "Sounds/SoundEffectsArrayData";
    public static Object_SoundEffectsArray SoundEffectsArray = null;
    private Script_PlayerInformation _scriptPlayer = null;

    private void Awake()
    {
        // Load master mixer
        _master = Resources.Load(PathMaster) as AudioMixer;

#if DEBUG
        if (_master == null)
            Debug.LogWarning("Failed to load master mixer via: " + PathMaster);
#endif

        // Get mixer groups in master mixer
        _mixerGroupMusic = _master.FindMatchingGroups(SubPathMusic)[0];
        _mixerGroupAmbient = _master.FindMatchingGroups(SubPathAmbient)[0];
        _mixerGroupSFX = _master.FindMatchingGroups(SubPathSfx)[0];

        // Load our custom sound effects array scriptable object
        // This will be used to pick random audio clips from
        SoundEffectsArray = Resources.Load(PathSoundsArray) as Object_SoundEffectsArray;

#if DEBUG
        if (SoundEffectsArray == null)
            Debug.LogWarning("Failed to load sound effects array object via: " + PathSoundsArray);
#endif

        // Load audio clips
        _clipMusicSneaking = Resources.Load(PathClipMusic + PathClipMusicSneaking) as AudioClip;
        _clipMusicTension = Resources.Load(PathClipMusic + PathClipMusicTension) as AudioClip;
        _clipMusicBase = Resources.Load(PathClipMusic + PathClipMusicBase) as AudioClip;
        _clipMusicIntro = Resources.Load(PathClipMusic + PathClipMusicIntro) as AudioClip;

        _clipAmbientBase = Resources.Load(PathClipAmbient + PathClipMusicBase) as AudioClip;
        _clipAmbientCity = Resources.Load(PathClipAmbient + PathClipAmbientCity) as AudioClip;

        // Create empty game object to sort stray audio sources into
        _strayParent = new GameObject("StrayAudioSources");

        _sourcesInScene = FindObjectsOfType<AudioSource>();
        // Get and or create music audio sources
        CreateMusicAudioSources();
        // Create ambient audio source
        CreateAmbientAudioSource();

        // Get variables for the adaptive music (enemies, player, ...)
        _scriptEnemies = Script_ManagerEnemy.Instance;
        _player = Script_LocomotionBase.Instance.CameraRig.transform;
        _maskEnemiesOnly = LayerMask.NameToLayer(EnemyLayer);

        // Initialize beat per minute dictionary
        _bpmDictionary = new Dictionary<AudioClip, int>()
        {
            { _clipMusicSneaking, BpmMusicSneaking},
            { _clipMusicTension, BpmMusicSneaking},
            { _clipMusicIntro, BpmMusicIntro},
            {_clipMusicBase, BpmMusicIntro },
        };

        _scriptPlayer = Script_PlayerInformation.Instance;
    }

    #region Adpative Music
    private void LateUpdate()
    {
        if (_fading || _musicState == MusicState.Overwrite || _scriptPlayer.IsInBase)
            return;

        // Update the nearby enemy counts (with a delay for performance)
        if (_updateDelayTimer > 0)
        {
            _updateDelayTimer -= Time.deltaTime;

            // Also keep track of the amount of time the enemies have been searching for
            if (_musicState == MusicState.EnemiesAreSearching)
                _searchTimer += Time.deltaTime;

            return;
        }

        // Get all enemies in range of the player
        _enemyColliders = GetEnemiesInRange();
        _enemiesInRange = _enemyColliders.Length;
        //Debug.Log("In range: " + _enemiesInRange);
        // Get all attacking enemies
        _enemiesAlarmed = _scriptEnemies.GetNrAttackingEnemies();
        //Debug.Log("Alarmed: " + _enemiesAlarmed);
        // Get all searching enemies
        _enemiesSearching = _scriptEnemies.GetNrSearchingEnemies();
        //Debug.Log("Searching: " + _enemiesSearching);

        // Break out into tension music immediately when spotted, regardless of the current state
        if (_enemiesAlarmed > 0 && _musicState != MusicState.EnemiesAreAlarmed)
        {
            StartTensionMusic();
            return;
        }

        // Update the music state
        switch (_musicState)
        {
            // No music is playing
            case MusicState.Mute:

                // Cross fade to search music when enemies are searching
                if (_enemiesSearching > 0)
                {
                    StartSearchingMusic(_clipMusicSneaking);
                    _muteTimer = MuteTime;
                    break;
                }

                // Cross fade to sneak music when enemies are in range
                if (_enemiesInRange > 0)
                {
                    StartSneakingMusic();
                    _muteTimer = MuteTime;
                    break;
                }

                if (_muteTimer <= 0f)
                {
                    StartCityAmbient(0.4f);
                    _muteTimer = 0f;
                }
                else
                    _muteTimer -= Time.deltaTime;

                break;

            // Tension music is playing
            case MusicState.EnemiesAreNear:

                // Stop playing music if their are no enemies nearby
                if (_enemiesInRange <= 0 && _enemiesSearching <= 0)
                {
                    StopMusic();
                    break;
                }

                break;

            // Searching music is playing
            case MusicState.EnemiesAreSearching:

                // Otherwise cross fade to sneaking music if no enemies are searching anymore
                // Or if they've been searching for too long
                if (_searchTimer > MaxSearchTime || _enemiesSearching <= 0 || _enemiesInRange <= 0)
                {
                    StartSneakingMusic();
                    break;
                }

                break;

            // An enemy has seen the player
            case MusicState.EnemiesAreAlarmed:

                // Cross fade to sneak music when enemies aren't alarmed anymore
                if (_enemiesAlarmed <= 0)
                {
                    if (_enemiesSearching > 0)
                    {
                        _searchTimer = 0f;
                        StartSearchingMusic(_clipMusicTension);
                        break;
                    }

                    else if (_enemiesInRange > 0)
                    {
                        StartSneakingMusic();
                        break;
                    }
                }

                // Otherwise stop the music if nothing is nearby or searching anymore
                if (_enemiesAlarmed <= 0 && _enemiesInRange <= 0 && _enemiesSearching <= 0)
                    StopMusic();

                break;
        }

        // Reset timer
        _updateDelayTimer = UpdateDelay;
    }

    private void CrossFadeMusicCoroutine(MusicState state, AudioClip clip, bool randomPos = true)
    {
        // The fading variable already prevents coroutines from overlapping in the late update
        // So just to make sure, stop the remaining fade coroutine
        if (_coroutineCrossFade != null)
            StopCoroutine(_coroutineCrossFade);

        if (_musicState != state)
            _coroutineCrossFade = StartCoroutine(CrossFadeMusic(clip, state, randomPos));
    }

    public void StartTensionMusic(bool startAtRandomPos = true)
    {
        CrossFadeMusicCoroutine(MusicState.EnemiesAreAlarmed, _clipMusicTension, startAtRandomPos);
    }

    public void StartSearchingMusic(AudioClip clip, bool startAtRandomPos = true)
    {
        CrossFadeMusicCoroutine(MusicState.EnemiesAreSearching, clip, startAtRandomPos);
    }

    public void StartSneakingMusic(bool startAtRandomPos = true)
    {
        CrossFadeMusicCoroutine(MusicState.EnemiesAreNear, _clipMusicSneaking, startAtRandomPos);
    }

    public void ForceStartBaseMusic(bool startAtRandomPos = true)
    {
        CrossFadeMusicCoroutine(MusicState.Overwrite, _clipMusicBase, startAtRandomPos);
    }

    public void ForceStartIntroMusic(bool startAtRandomPos = false)
    {
        CrossFadeMusicCoroutine(MusicState.Overwrite, _clipMusicIntro, startAtRandomPos);
    }

    public void StopMusic()
    {
        CrossFadeMusicCoroutine(MusicState.Mute, null);
    }

    public void StartAmbient(AudioClip clip, float volume = 1f)
    {
        if (_coroutineCrossFadeAmbient != null)
            StopCoroutine(_coroutineCrossFadeAmbient);

        _coroutineCrossFadeAmbient = StartCoroutine(FadeIntoNewAmbient(clip, volume));
    }

    public void StopAmbient()
    {
        StartAmbient(null, 0f);
    }

    public void StartPlayerBaseAmbient(float volume = 1f)
    {
        StartAmbient(_clipAmbientBase, volume);
    }

    public void StartCityAmbient(float volume = 1f)
    {
        StartAmbient(_clipAmbientCity, volume);
    }

    private Collider[] GetEnemiesInRange()
    {
        return Physics.OverlapBox(
            // Center
            _player.position + Vector3.up * EnemyOverlapRange,
            // Extent
            Vector3.one * EnemyOverlapRange,
            // Rotation
            Quaternion.identity,
            // Layer Mask
            1 << _maskEnemiesOnly,
            // Hit triggers
            QueryTriggerInteraction.Collide)
            // Filter out limbs
            .Where(coll => coll.CompareTag(EnemyTag)).ToArray();
    }

    /// <summary>
    /// Cross fade from the currently designated music AudioSource (always = _sourceMusic[0]) to the next AudioSource
    /// </summary>
    /// <param name="newClip">The music clip to cross fade into</param>
    /// <param name="newState">The state to set after fading is done</param>
    /// <param name="randomPos">Start song at random position</param>
    /// <returns></returns>
    private IEnumerator CrossFadeMusic(AudioClip newClip, MusicState newState, bool randomPos = true)
    {
        _fading = true;

        // Determine the state of the coast clear sound
        // Play the coast clear sound if we're transitioning from an alarmed state to mute
        if (_musicState == MusicState.EnemiesAreAlarmed)
            _playCoastClearSound = newState != MusicState.EnemiesAreAlarmed;

        // If the clip is already playing- don't crossfade, just update the state
        var identicalClip = newClip == _sourceMusic[0].clip;

        if (!identicalClip)
        {
            _sourceMusic[1].clip = newClip;

            // Randomize the play position if we're transitioning from mute
            if (randomPos && _musicState == MusicState.Mute && newClip.length >= 60f)
            {
                var startPosInSec = (60f / _bpmDictionary[newClip]) * UnityEngine.Random.Range(0, _bpmDictionary[newClip]);
                if (startPosInSec > newClip.length)
                    startPosInSec = 0f;

                _sourceMusic[1].time = startPosInSec;
            }
            // Ohterwise match the currently playing time
            // (we can do this because the sneaking and tension music have the same BPM)
            else
                _sourceMusic[1].time = _sourceMusic[0].time;

            if (!_sourceMusic[1].isPlaying)
                _sourceMusic[1].Play();
            else
                _sourceMusic[1].UnPause();

            // Crossfade
            while (_sourceMusic[1].volume < 1f)
            {
                _sourceMusic[0].volume -= CrossFadeSpeed * Time.deltaTime;
                _sourceMusic[1].volume = Mathf.Clamp(1f - _sourceMusic[0].volume, 0f, 1f);
                yield return new WaitForSeconds(0.001f);
            }
        }

        // Update the music state
        _musicState = newState;

        // Play but don't spam the coast clear sound (shouldn't be possible considering the fade speed, but check anyway)
        if (_playCoastClearSound && (_sourceSFXCoastClear == null || !_sourceSFXCoastClear.isPlaying) && _musicState == MusicState.Mute)
        {
            _sourceSFXCoastClear = PlaySFX(SoundCoastClear, _player.position, UnityEngine.Random.Range(0.5f, 1f), 1f, 0.04f, false, SoundType.SFX_2D);
            _playCoastClearSound = false;
        }

        // Place the faded in track at index 0
        if (!identicalClip)
            _sourceMusic.Reverse();
        // Ensure the swapped track is paused
        _sourceMusic[1].volume = 0f;
        _sourceMusic[1].Pause();

        // Play some ambient so the game doesn't go silent
        if (_musicState == MusicState.Mute && _sourceAmbient.volume <= 0f)
            FadeIntoNewAmbient(_clipAmbientCity, 0.4f);

        _fading = false;
        yield break;
    }

    /// <summary>
    /// Ambient is a suddle extra layer of audio, so we just fade this in and out instead
    /// </summary>
    /// <param name="newClip">The ambient clip to play after fading out and fading back in</param>
    /// <returns></returns>
    private IEnumerator FadeIntoNewAmbient(AudioClip newClip, float volume)
    {
        // Break if the ambient is already playing
        if (_sourceAmbient.clip == newClip)
            yield break;

        // Fade out the current ambient
        while (_sourceAmbient.volume > 0f)
        {
            _sourceAmbient.volume -= CrossFadeSpeed * 2f * Time.deltaTime;
            yield return null;
        }

        // Set and play the new ambient parallel to the music
        _sourceAmbient.clip = newClip;
        _sourceAmbient.time = _sourceMusic[0].time;
        _sourceAmbient.Play();

        // And fade back in
        while (_sourceAmbient.volume < volume)
        {
            _sourceAmbient.volume += CrossFadeSpeed * 2f * Time.deltaTime;
            yield return null;
        }

        yield break;
    }
    #endregion

    #region Sound Effect Methods
    /// <summary>
    /// Shorthand for performing a myriad of necessary null checks
    /// </summary>
    /// <param name="source">AudioSource component to check the state of</param>
    private bool IsNull(AudioSource source)
    {
        if (source == null)
        {
#if DEBUG
            Debug.LogWarning("Audio source was null");
#endif
            return true;
        }

        if (!source.enabled)
        {
#if DEBUG
            Debug.LogWarning("Audio source was disabled on: " + source.name, source.gameObject);
#endif
            return true;
        }

        return false;
    }

    /// <summary>
    /// Play a single or random audio clip on the designated audio source
    /// </summary>
    /// <param name="soundEffectName">The name of the array of audio clips in the sound effects array</param>
    /// <param name="source">The audio source to play the audio clip on</param>
    /// <param name="dontInterrupt">Prevent the next clip from interrupting this clip by creating new audio sources in the current scene</param>
    /// <param name="volume">Overwrite the audio source volume</param>
    /// <param name="pitchBase">Overwrite the base pitch to randomize from</param>
    /// <param name="pitchOffset">Overwrite the pitch offset to randomize the pitch by</param>
    /// <param name="loop">Overwrite the AudioSource loop setting</param>
    /// <param name="type">The type of sound to play. Adjusts the audio source settings</param>
    public AudioSource PlaySFX(string soundEffectName, AudioSource source, bool dontInterrupt = true,
        float volume = 1f, float pitchBase = 1f, float pitchOffset = PitchOffset, bool loop = false,
        SoundType type = SoundType.SFX_3D)
    {
        // Attempt to set a clip (return null if failed)
        if (!SetClip(soundEffectName, pitchBase, PitchOffset))
            return null;

        // Play on existing audio source if the sound should not be interrupted
        if (dontInterrupt && source != null && source.isPlaying)
            _targetSource = GetExistingAudioSource(source.transform.position);
        else if (source == null)
        {
#if DEBUG
            Debug.LogWarning("SFX source was null");
#endif
            return null;
        }
        else
            _targetSource = source;

        if (IsNull(_targetSource))
            return null;

        // Reuse an audio source
        SetAudioSourceSettings(ref _targetSource, type);

        Play(_targetSource, _targetClip, volume, loop);

        // Return the target audio source
        return _targetSource;
    }

    /// <summary>
    /// Play a single or random audio clip on the given position (the audio source is created by the manager)
    /// </summary>
    /// <param name="soundEffectName">The name of the array of audio clips in the sound effects array</param>
    /// <param name="position">The position to play the sound from</param>
    /// <param name="volume">Overwrite the audio source volume</param>
    /// <param name="pitchBase">Overwrite the base pitch to randomize from</param>
    /// <param name="pitchOffset">Overwrite the pitch offset to randomize the pitch by</param>
    /// <param name="loop">Overwrite the AudioSource loop setting</param>
    /// <param name="type">The type of sound to play. Adjusts the audio source settings</param>
    /// <returns></returns>
    public AudioSource PlaySFX(string soundEffectName, Vector3 position,
        float volume = 1f, float pitchBase = 1f, float pitchOffset = PitchOffset, bool loop = false,
        SoundType type = SoundType.SFX_3D)
    {
        // Attempt to set a clip (return null if failed)
        if (!SetClip(soundEffectName, pitchBase, PitchOffset))
            return null;

        // Reuse an audio source
        _targetSource = GetExistingAudioSource(position);

        if (IsNull(_targetSource))
            return null;

        // Set relevant audio source settings
        SetAudioSourceSettings(ref _targetSource, type);

        Play(_targetSource, _targetClip, volume, loop);

        // Return the target audio source
        return _targetSource;
    }

    /// <summary>
    /// Play a single or random audio clip on the given position (the audio source is created by the manager)
    /// </summary>
    /// <param name="soundEffectName">The name of the array of audio clips in the sound effects array</param>
    /// <param name="position">The position to play the sound from</param>
    /// <param name="byPassReverb">Ignore any reverb zones</param>
    /// <param name="minDistance">The maximum distance for the 3D sound to travel</param>
    /// <param name="maxDistance">The minimum distance for the 3D sound to travel</param>
    /// <param name="volume">Overwrite the audio source volume</param>
    /// <param name="pitchBase">Overwrite the base pitch to randomize from</param>
    /// <param name="pitchOffset">Overwrite the pitch offset to randomize the pitch by</param>
    /// <param name="loop">Overwrite the AudioSource loop setting</param>
    /// <returns></returns>
    public AudioSource PlaySFX(string soundEffectName, Vector3 position, bool byPassReverb,
        float minDistance = 1f, float maxDistance = 500f, float volume = 1f, float pitchBase = 1f,
        float pitchOffset = PitchOffset, bool loop = false)
    {
        // Attempt to set a clip (return null if failed)
        if (!SetClip(soundEffectName, pitchBase, PitchOffset))
            return null;

        // Reuse an audio source
        _targetSource = GetExistingAudioSource(position);

        if (IsNull(_targetSource))
            return null;

        // Set relevant audio source settings
        SetAudioSourceSettings(ref _targetSource, SoundType.SFX_3D, minDistance, maxDistance, byPassReverb);

        Play(_targetSource, _targetClip, volume, loop);

        // Return the target audio source
        return _targetSource;
    }

    private IEnumerator ResetDistance(float delayInSeconds, AudioSource source, float minDistance, float maxDistance)
    {
        yield return new WaitForSeconds(delayInSeconds);

        // Reset
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    /// <summary>
    /// Set the target clip according to the array of audio clips
    /// </summary>
    /// <param name="soundEffectName">The name of the array of audio clips in the sound effects array</param>
    /// <param name="pitchBase">Overwrite the base pitch to randomize from</param>
    /// <param name="pitchOffset">Overwrite the pitch offset to randomize the pitch by</param>
    /// <returns></returns>
    private bool SetClip(string soundEffectName, float pitchBase = 1f, float pitchOffset = PitchOffset)
    {
        if (SoundEffectsArray == null)
            return false;

        var clips = SoundEffectsArray.GetAudioClips(soundEffectName);

        // Skip if an empty audio clip array was passed
        if (clips == null)
            return false;

        // Randomize pitch
        _pitch = UnityEngine.Random.Range(pitchBase - pitchOffset, pitchBase + pitchOffset);

        // Generate random index
        GetTargetClip(clips);
        if (_targetClip == null)
            return false;

        return true;
    }

    private void Play(AudioSource source, AudioClip clip, float volume = 1f, bool loop = false)
    {
        source.clip = clip;
        source.pitch = _pitch;
        source.volume = volume;
        source.loop = loop;
        source.Play();
    }

    /// <summary>
    /// Get a random or single clip from the array of clips according to it's length
    /// </summary>
    /// <param name="clips"></param>
    /// <returns></returns>
    private void GetTargetClip(AudioClip[] clips)
    {
        // Determine the type to be played
        var type = clips.Length > 1 ? ClipType.Random : ClipType.Single;

        // Determine which random clip should be played
        var index = (type == ClipType.Random) ? UnityEngine.Random.Range(0, clips.Length) : 0;

        if (type == ClipType.Random)
            // Prevent the method from playing the same index in succesion
            if (_dictionaryIndices.ContainsKey(clips.GetHashCode()))
            {
                //Keep trying randomizing the index until the index isn't repeated
                while (_dictionaryIndices[clips.GetHashCode()] == index)
                    index = UnityEngine.Random.Range(0, clips.Length);

                // Update the new index to be used for future comparison
                _dictionaryIndices[clips.GetHashCode()] = index;
            }
            else
                _dictionaryIndices.Add(clips.GetHashCode(), index);

        // Play sound according to type
        switch (type)
        {
            case ClipType.Single:
                _targetClip = clips.First(clip => clip != null);
                break;
            case ClipType.Random:
                _targetClip = clips[index];
                break;
        }

#if DEBUG
        if (_targetClip == null)
            Debug.LogError("Assignment of target clip was null");
#endif
    }
    #endregion

    #region Audio Source Methods
    /// <summary>
    /// Get all the audio sources in the current scene running on the music mixer group.
    /// Create new or delete excess audio sources if necessary
    /// </summary>
    private void CreateMusicAudioSources()
    {
        // Get the music audio sources in the scene
        _sourceMusic = _sourcesInScene.Where(source => source.outputAudioMixerGroup == _mixerGroupMusic).ToList();

        // Fill music source list with 2 audio sources to crossfade between
        while (_sourceMusic.Count < 2)
        {
            var newSource = new GameObject().AddComponent<AudioSource>();
            _sourceMusic.Add(newSource);
        }

        // Remove excess music audio sources
        if (_sourceMusic.Count > 2)
        {
            foreach (AudioSource source in _sourceMusic.GetRange(2, _sourceMusic.Count - 2))
                Destroy(source.gameObject);

            _sourceMusic.RemoveRange(2, _sourceMusic.Count - 2);
        }

        // Mute everything initially
        for (int i = 0; i < _sourceMusic.Count; ++i)
        {
            var sourceRef = _sourceMusic[i];

            // Give a neat name
            if (_sourceMusic[0].gameObject != _sourceMusic[1].gameObject)
            {
                var name = i == 0 ? "MusicAudioSource" : "MusicCrossFadeAudioSource";
                sourceRef.name = name;
            }

            // Set relevant audio source settings
            SetAudioSourceSettings(ref sourceRef, SoundType.Music, 1f, 500f, true);

            sourceRef.clip = null;
            sourceRef.Stop();
        }

        // Fix for instant initial fade glitch
        _sourceMusic[0].volume = 1f;
        _sourceMusic[1].volume = 0f;
    }

    /// <summary>
    /// Do the same as the music audio source creation method, but for ambient
    /// </summary>
    private void CreateAmbientAudioSource()
    {
        // Get the ambient audio source (there should only be one)
        _sourceAmbient = new GameObject("GlobalAmbientAudioSource").AddComponent<AudioSource>();

        // Set relevant audio source settings
        SetAudioSourceSettings(ref _sourceAmbient, SoundType.Ambient, 1f, 500f, true);

        _sourceAmbient.clip = null;
        _sourceAmbient.Stop();
    }

    /// <summary>
    /// Creata a new audio source if the sound effect should not be interrupted.
    /// Similar to AudioSource.PlayClipAtPoint, but we save the stray sources for later use
    /// </summary>
    /// <param name="position">Position for the AudioSource to be moved to</param>
    /// <returns></returns>
    private AudioSource GetExistingAudioSource(Vector3 position)
    {
        var playing = false;
        var newSourceListIndex = 0;

        // Try to play clip on any existing stray audio sources
        if (_listOfStrayAudioSources.Capacity > 0)
        {
            foreach (AudioSource straySource in _listOfStrayAudioSources)
            {
                if (straySource == null || straySource.isPlaying)
                    continue;

                // Move the stray position to the desired position
                straySource.transform.position = position;
                newSourceListIndex = _listOfStrayAudioSources.IndexOf(straySource);

                playing = true;
                break;
            }
        }

        // If the foreach failed to find an unoccupied stray audio source, create a new one
        if (!playing)
        {
            var newSource = new GameObject(
                "StrayAudioSource (" + _listOfStrayAudioSources.Count + ')').AddComponent<AudioSource>();

            newSource.transform.SetParent(_strayParent.transform);

            // Move the new source to the desired position
            newSource.transform.position = position;

            // Add to list of strays for reuse
            _listOfStrayAudioSources.Add(newSource);
            newSourceListIndex = _listOfStrayAudioSources.IndexOf(newSource);
        }

        return _listOfStrayAudioSources[newSourceListIndex];
    }

    /// <summary>
    /// Set the settings of the audio source according to the type
    /// </summary>
    /// <param name="source">The audio source to change the settings of</param>
    /// <param name="type">The type of the sound</param>
    private void SetAudioSourceSettings(ref AudioSource source, SoundType type, float minDistance = 1f, float maxDistance = 500f, bool byPassReverb = false)
    {
        switch (type)
        {
            case SoundType.Music:
            case SoundType.Ambient:
            case SoundType.SFX_2D:

                source.outputAudioMixerGroup = type == SoundType.Music ? _mixerGroupMusic : _mixerGroupAmbient;
                source.spatialBlend = 0f;

                if (type == SoundType.Ambient)
                    source.loop = true;

                break;

            case SoundType.SFX_3D:

                source.outputAudioMixerGroup = _mixerGroupSFX;
                source.spatialBlend = 1f;

                break;
        }

        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.bypassReverbZones = byPassReverb;

        source.playOnAwake = false;
    }

    #endregion
}
