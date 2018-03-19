using UnityEngine;

namespace UnityStandardAssets.Effects
{
    /// <summary>
    /// A simple script to scale the size, speed and lifetime of a particle system
    /// </summary>
    public class ParticleSystemMultiplier : MonoBehaviour
    {
        public float Multiplier = 1;

        public ParticleSystem[] GetParticles()
        {
            var systems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule mainModule = system.main;
                mainModule.startSizeMultiplier *= Multiplier;
                mainModule.startSpeedMultiplier *= Multiplier;
                mainModule.startLifetimeMultiplier *= Mathf.Lerp(Multiplier, 1, 0.5f);
                system.Clear();
                //system.Play();
            }
            return systems;
        }
    }
}
