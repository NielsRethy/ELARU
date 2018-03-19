using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Script_WaterCurrent : MonoBehaviour
{
    [SerializeField]
    private Vector3 _direction = Vector3.forward;
    [SerializeField]
    private float _force = 20f;
    private const string TagPickUp = "PickUp";
    private const string LayerInteract = "Interactable";

    private List<Rigidbody> _rigidbodiesInCollider = new List<Rigidbody>();
    private List<float> _initDragValues = new List<float>();
    private const int ArrowHeadAngle = 20;
    private const float ArrowHeadLength = 0.25f;

    //private const float FloatHeight = 2f;
    private const float FloatDamp = .05f;
    private const float BuoyancyForce = 30f;

    private List<float> _enterY = new List<float>();
    //private const float _desDepth = 0.5f;
    private void Start()
    {
        // Ensure the game object has the right layer
        gameObject.layer = LayerMask.NameToLayer(LayerInteract);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(TagPickUp))
            return;

        var rigid = other.GetComponent<Rigidbody>();
        _initDragValues.Add(rigid.drag);
        // Use large drag value to fake water
        rigid.drag = 10f;
        _rigidbodiesInCollider.Add(rigid);

        _enterY.Add(rigid.position.y);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_rigidbodiesInCollider == null || _rigidbodiesInCollider.Count <= 0)
            return;

        var rigidbody = _rigidbodiesInCollider.Find(rigid => rigid.gameObject == other.gameObject);
        var index = _rigidbodiesInCollider.IndexOf(rigidbody);
        rigidbody.drag = _initDragValues[index];


        if (rigidbody != null)
        {
            _rigidbodiesInCollider.Remove(rigidbody);
            _enterY.RemoveAt(index);
        }
    }

    private void LateUpdate()
    {
        if (_rigidbodiesInCollider == null)
            return;

        //foreach (Rigidbody rigidbody in _rigidbodiesInCollider)
        //{
        //    rigidbody.AddForce(Vector3.up * BuoyancyForce);
        //    rigidbody.AddForce(-rigidbody.velocity * FloatDamp * rigidbody.mass);
        //    rigidbody.AddForce(_direction * _force * rigidbody.mass);
        //    Debug.Log("Pushing: " + rigidbody.name);
        //}
        for (int i = 0; i < _rigidbodiesInCollider.Count; i++)
        {
            var diff = _enterY[i]/*-_desDepth)*/ - _rigidbodiesInCollider[i].position.y;
            //Debug.Log(diff);
            //var force = BuoyancyForce * Mathf.Pow(((diff)*10),3) /*/ (_desDepth*2)*/;
            var force = BuoyancyForce * diff/*/ _desDepth*/ + -Physics.gravity.y;
            force *= (force / 3.5f);
            Debug.Log(force);
            _rigidbodiesInCollider[i].AddForce(Vector3.up * force,ForceMode.Force);
            _rigidbodiesInCollider[i].AddForce(-_rigidbodiesInCollider[i].velocity * FloatDamp * _rigidbodiesInCollider[i].mass);
            _rigidbodiesInCollider[i].AddForce(_direction * _force * _rigidbodiesInCollider[i].mass);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, _direction);

        Vector3 right = Quaternion.LookRotation(_direction) * Quaternion.Euler(0, 180 + ArrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(_direction) * Quaternion.Euler(0, 180 - ArrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(transform.position + _direction, right * ArrowHeadLength);
        Gizmos.DrawRay(transform.position + _direction, left * ArrowHeadLength);
    }
}
