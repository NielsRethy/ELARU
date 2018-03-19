using UnityEngine;

public class Script_ReplaceCompnanion : MonoBehaviour
{
    [SerializeField] private GameObject _normalCompanion;
    [SerializeField] private GameObject _tutorialCompanion;

    public void ReplaceObject()
    {
        //Activate normal companion
        _normalCompanion.transform.parent = null;
        _normalCompanion.SetActive(true);

        //Deactivate tutorial companion
        _tutorialCompanion.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ReplaceObject();
        }
    }
}
