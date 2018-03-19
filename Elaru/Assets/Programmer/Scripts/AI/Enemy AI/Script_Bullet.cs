using System.Collections;
using UnityEngine;

public class Script_Bullet : MonoBehaviour
{
    private float _speed = 1500f;
    private float _radius = 1f;
    private int _damage = 20;
    private float _maxRot = 15f;
    
    void Start()
    {
        var player = Camera.main.transform;
        var dir = (player.position - transform.position).normalized;
        dir = Vector3.Lerp(transform.forward, dir, _maxRot);
        transform.Rotate(90, 0, 0);
        GetComponent<Rigidbody>().AddForce(/*transform.forward*/dir * _speed);
        Invoke("EnableCol", .15f);
        Invoke("Kill", 5f);
    }

    void EnableCol()
    {
        GetComponent<CapsuleCollider>().enabled = true;
    }

    private void Kill()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider collision)
    {
        //Debug.Log(collision.collider.name);
        Script_ManagerEnemy.Instance.AreaDamage(/*collision.contacts[0].point*/transform.position, _radius, _damage);
        Destroy(gameObject);
    }
}
