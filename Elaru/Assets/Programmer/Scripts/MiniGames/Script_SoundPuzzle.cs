using UnityEngine;

public class Script_SoundPuzzle : MonoBehaviour
{
    /// <summary>
    /// Call this when the alarm triggers
    /// </summary>
	public void StartPlayingSound()
    {
        //Makes a sound and logs in a manger
        Script_ManagerEnemy.Instance.Sound(transform.position, Script_EnemyBase.SoundType.Alarm);
    }
}
