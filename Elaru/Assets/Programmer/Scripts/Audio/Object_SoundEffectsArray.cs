using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundEffectsArrayData", menuName = "ELARU/Sound Effects Array", order = 1)]
public class Object_SoundEffectsArray : ScriptableObject
{
    [Serializable]
    public struct SoundEffectsArray
    {
        public string Name;
        public AudioClip[] AudioClips;
    }

    public SoundEffectsArray[] SoundEffects = new SoundEffectsArray[0];

    /// <summary>
    /// Get the array of audio clips according to the string used in the sound effect array object. Returns null if something went wrong
    /// </summary>
    /// <param name="soundEffectName">The name given to the array of audio clips in the sound effect array object</param>
    /// <returns></returns>
    public AudioClip[] GetAudioClips(string soundEffectName)
    {
        if (soundEffectName.Length <= 0)
            return null;

        var matchIndex = SoundEffects.ToList().FindIndex(x => x.Name == soundEffectName);
        if (matchIndex < 0)
            return null;

        AudioClip[] clips = SoundEffects[matchIndex].AudioClips;

        if (clips == null)
        {
            Debug.LogWarning("Sound effect: '" + soundEffectName + "' does not exist in the Sound Effects Array! Returning null");
            return null;
        }

        if (clips.Length <= 0)
        {
            Debug.LogWarning("Sound effect: '" + soundEffectName + "' contains no Audio Clips in the Sound Effects Array! Returning null");
            return null;
        }

        return clips;
    }
}
