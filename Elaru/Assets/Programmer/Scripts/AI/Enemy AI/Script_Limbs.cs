using UnityEngine;

public class Script_Limbs : MonoBehaviour
{
    [SerializeField]
    Script_EnemyBase _parentEnemy = null;
    [SerializeField]
    float _weakSpotMultiplier = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (_parentEnemy.CanTakeDamage && other.CompareTag("Blade"))
        {
            //if (_weakSpotMultiplier > 1f) Debug.Log("Critical Damage");
            _parentEnemy.DealDamage((float)Script_BehaviorTreeFramework.PBB["SwordDamage"].Value * _weakSpotMultiplier, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _parentEnemy.CanTakeDamage = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_parentEnemy.CanTakeDamage)
        {
            if (collision.collider.CompareTag("Blade") || collision.collider.CompareTag("PickUp"))
            {
                //if (_weakSpotMultiplier > 1f) Debug.Log("Critical Damage");
                _parentEnemy.DealDamage((float)Script_BehaviorTreeFramework.PBB["SwordDamage"].Value * _weakSpotMultiplier, true);
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Blade"))
            _parentEnemy.CanTakeDamage = true;
    }

    public void Attacked(float damage)
    {
        if (_parentEnemy.CanTakeDamage)
        {
            //if (_weakSpotMultiplier > 1f) Debug.Log("Critical Damage");
            _parentEnemy.DealDamage(damage * _weakSpotMultiplier, true);
        }
    }

    private void Start()
    {
        if (_parentEnemy == null)
        {
            Transform parent = transform.parent;
            while (_parentEnemy == null)
            {
                if (parent.GetComponent<Script_EnemyBase>() != null)
                {
                    _parentEnemy = parent.GetComponent<Script_EnemyBase>();
                }
                parent = parent.parent;
            }
        }
    }
}
