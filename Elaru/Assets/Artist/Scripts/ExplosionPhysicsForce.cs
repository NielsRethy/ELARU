using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Effects
{
    public class ExplosionPhysicsForce : MonoBehaviour
    {
        public float ExplosionForce = 4;

        public IEnumerator Play()
        {
            // wait one frame because some explosions instantiate debris which should then
            // be pushed by physics force
            yield return null;

            float multiplier = GetComponent<ParticleSystemMultiplier>().Multiplier;

            float r = 10 * multiplier;
            var cols = Physics.OverlapSphere(transform.position, r);
            var rigidbodies = new List<Rigidbody>();
            foreach (var col in cols)
            {
                if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody))
                {
                    rigidbodies.Add(col.attachedRigidbody);
                }
            }
            foreach (var rb in rigidbodies)
            {
                rb.AddExplosionForce(ExplosionForce * multiplier, transform.position, r, 1 * multiplier, ForceMode.Impulse);
            }
            yield break;
        }
    }
}
