using UnityEngine;

public class Script_AITest : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
            Script_ManagerEnemy.Instance.Sound(transform.position, Script_EnemyBase.SoundType.Alarm);
    }
}
