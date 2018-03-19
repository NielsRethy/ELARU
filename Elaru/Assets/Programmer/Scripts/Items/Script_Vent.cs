using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Script_Vent : MonoBehaviour
{
    [SerializeField]
    [Range(0f, 1f)]
    private float _randomSpeedMax = 1f;
    [SerializeField]
    [Range(0f, 1f)]
    private float _randomSpeedMin = 0f;

    private const string _animSpeed = "Speed";

    private void Start()
    {
        GetComponent<Animator>().SetFloat(_animSpeed, Random.Range(_randomSpeedMin, _randomSpeedMax));
    }
}
