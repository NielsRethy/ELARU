using UnityEngine;

public class Script_ObjectiveParticleManager : MonoBehaviour {

    public void SetActive(bool OnOff)
    {
        this.transform.GetChild(0).gameObject.SetActive(OnOff);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Equals("QuestTarget") || other.name.Equals("Bomb"))
        {
            this.gameObject.SetActive(false);
        }
    }
}
