using UnityEngine;

public class Script_CompanionObstacleCheck : MonoBehaviour
{
    public float Distance;

    public bool CheckInFront()
    {
        Debug.DrawRay(transform.position, transform.right * Distance , Color.green);
        return Physics.Raycast(transform.position, transform.right, Distance);
    }
}
