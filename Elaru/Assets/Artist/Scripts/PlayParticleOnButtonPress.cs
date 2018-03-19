using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class PlayParticleOnButtonPress : MonoBehaviour
{
    private List<ParticleSystem> _particleSystem = new List<ParticleSystem>();
    private bool _isPlaying = true;
    // Use this for initialization
    void Start()
    {
        _particleSystem = GetComponentsInChildren<ParticleSystem>().ToList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchParticleSystem();
        }

    }

    public void SwitchParticleSystem()
    {
        //_isPlaying = !_isPlaying;

        if (_isPlaying)
            _particleSystem.ForEach(x => x.Play());
        else
        {
            _particleSystem.ForEach(x => { x.time = 0f; x.Stop(); });
        }
    }
}
