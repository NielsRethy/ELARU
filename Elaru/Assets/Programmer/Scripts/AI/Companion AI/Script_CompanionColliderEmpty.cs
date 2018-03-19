using UnityEngine;

/// <summary>
/// Tracks if anything is blocking this collider
/// </summary>
public class Script_CompanionColliderEmpty : MonoBehaviour
{
    public bool Empty { get { return _counter == 0; }}
    private int _counter = 0;
    GameObject _player;

    [SerializeField] private float _height = 5.0f;

    void Start()
    {
        _player = Script_LocomotionBase.Instance.CameraRig;
    }

    void OnTriggerEnter(Collider other)
    {
        _counter++;
    }

    private void OnTriggerExit(Collider other)
    {
        _counter--;
    }

    void Update()
    {

        transform.position = new Vector3(transform.position.x,_player.transform.position.y + _height,transform.position.z);
        transform.rotation = Quaternion.identity;

    }
}
