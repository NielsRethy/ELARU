using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Script_CollisionArea : MonoBehaviour
{
    //This script is used to make use of trigger actions from within other objects

    public bool TrackObjectsInCollider = false;
    private List<GameObject> _objectsInCollider = new List<GameObject>();

    //Trigger actions
    public Action<Collider> TriggerEnterAction;
    public Action<Collider> TriggerLeaveAction;
    public Action<Collider> TriggerStayAction;

    //Collision actions
    public Action<Collider> CollisionEnterAction;
    public Action<Collider> CollisionLeaveAction;
    public Action<Collider> CollisionStayAction;

    [SerializeField]
    private bool _useCollisionInsteadOfTrigger = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (_useCollisionInsteadOfTrigger)
            return;

        //Call trigger enter action
        if (TriggerEnterAction != null)
            TriggerEnterAction.Invoke(other);

        //Save entered object if needed
        if (TrackObjectsInCollider)
            _objectsInCollider.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_useCollisionInsteadOfTrigger)
            return;

        //Call trigger leave action
        if (TriggerLeaveAction != null)
            TriggerLeaveAction(other);

        //Remove object from list if needed
        if (TrackObjectsInCollider)
            _objectsInCollider.Remove(_objectsInCollider.First(x => x == other.gameObject));
    }

    private void OnTriggerStay(Collider other)
    {
        if (_useCollisionInsteadOfTrigger)
            return;

        //Call trigger stay action
        if (TriggerStayAction != null)
            TriggerStayAction(other);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!_useCollisionInsteadOfTrigger)
            return;

        //Call collision enter action
        if (CollisionEnterAction != null)
            CollisionEnterAction.Invoke(col.collider);
    }

    private void OnCollisionExit(Collision col)
    {
        if (!_useCollisionInsteadOfTrigger)
            return;

        //Call collision leave action
        if (CollisionLeaveAction != null)
            CollisionLeaveAction(col.collider);
    }

    private void OnCollisionStay(Collision col)
    {
        if (!_useCollisionInsteadOfTrigger)
            return;

        //Call collision stay action
        if (CollisionStayAction != null)
            CollisionStayAction(col.collider);
    }

    /// <summary>
    /// Returns objects that were tracked when entering this trigger
    /// </summary>
    public List<GameObject> GetObjectsInCollider()
    {
        if (!TrackObjectsInCollider)
        {
            Debug.LogWarning("Trying to get objects from collision area that does not track them");
        }
        _objectsInCollider.RemoveAll(x => x == null);
        return _objectsInCollider;
    }
}
