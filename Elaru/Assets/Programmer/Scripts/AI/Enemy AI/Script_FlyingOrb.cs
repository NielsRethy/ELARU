using UnityEngine;

public class Script_FlyingOrb : MonoBehaviour
{
    [SerializeField]
    Script_BlindAI _parentBlindAi;

    void Start()
    {
        if (_parentBlindAi == null)
            _parentBlindAi = transform.parent.GetComponent<Script_BlindAI>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        _parentBlindAi.Die(collision.contacts[0].point);
    }
}
