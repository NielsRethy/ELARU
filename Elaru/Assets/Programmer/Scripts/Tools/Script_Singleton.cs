using UnityEngine;

public abstract class Script_Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;

    public static T Instance
    {
        get
        {
            //return instance if it is already set
            if (_instance != null)
                return _instance;

            //Find occurrence of script in scene (does not work on inactive objects)
            var typeObjects = FindObjectsOfType(typeof(T));
            if (typeObjects.Length > 0)
                _instance = typeObjects[0] as T;
            if (typeObjects.Length > 1)
            {
                Debug.LogWarning("More than one instance of singleton: " + typeof(T));
                return _instance;
            }

            //if no instance exists create one
            if (_instance == null)
            {
                var singletonHolder = GameObject.FindGameObjectWithTag("Singletons");
                if (singletonHolder == null)
                {
                    Debug.Log("No " + typeof(T) + " instance yet, creating one");
                    singletonHolder = new GameObject("Singletons") {tag = "Singletons"};
                }
                _instance = singletonHolder.AddComponent<T>();
            }
            return _instance;
        }
    }

    private void OnEnable()
    {
        //Safety set singleton instance if singleton was disabled while requesting
        if (_instance == null)
        {
            var i = Instance;
        }
    }
}
