using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates icons on minimap
/// </summary>
public class Script_MinimapUpdater : Script_Singleton<Script_MinimapUpdater>
{
    //Player game object to track on the minimap
    [SerializeField]
    private GameObject _trackObject = null;

    private List<Material> _materials = new List<Material>();

    //Scale from world to minimap space
    [SerializeField]
    private float _mapCameraSize = 400;

    [SerializeField] private Vector3 _camOffset;

    [SerializeField]
    private List<GameObject> _miniMapImages = new List<GameObject>();

    [SerializeField]
    private Vector3 _mapCenterWorldSpace;
    
    private void Awake()
    {
        //Get material which contains icons
        foreach (var i in _miniMapImages)
        {
            var img = i.GetComponent<Image>();
            if (img != null)
                _materials.Add(img.material);
            else
            {
                var rnd = i.GetComponent<Renderer>();
                if (rnd != null)
                    _materials.Add(rnd.sharedMaterial);
            }
        }

        Debug.Log(gameObject.name);

        //Disable quest icons on minimap on start
        SetMainQuest(false);
        SetSideQuest(false);
        
        if (_trackObject == null)
        {
            Debug.Log("No track object set on minimap, finding object by tag");
            _trackObject = GameObject.FindWithTag("Player");
        }
    }

    private void Update()
    {
        if (_materials == null)
            return;

        //Flip player position as minimap is rendered upside down
        var playerPos = Script_PlayerInformation.Instance.IsInBase ? _mapCenterWorldSpace : (_trackObject.transform.position - _camOffset);

        //Calculate player position around minimap world space center
        playerPos.y = 0;
        playerPos.x += _mapCameraSize / 2f;
        playerPos.z += _mapCameraSize / 2f;

        //Convert from world space to minimap space
        playerPos /= _mapCameraSize;

        //Update player position in shader
        _materials.ForEach(x => x.SetVector("_Offset", new Vector4(playerPos.x, playerPos.z, 0, 0)));

        //Update show player
        _materials.ForEach(x => x.SetInt("_ShowPlayer", Script_PlayerInformation.Instance.IsInBase ? 0 : 1));
    }

    public void SetMainQuest(bool active, Vector3? worldPosition = null)
    {
        if (_materials == null)
            return;

        //Activate / deactivate main quest icon
        _materials.ForEach(x => x.SetInt("_MainQuestActive", active ? 1 : 0));

        if (!active)
            return;

        if (worldPosition != null)
        {
            //Calculate quest position around minimap world space center
            var pos = (worldPosition.Value - _camOffset);
            pos.y = 0;
            pos.x += _mapCameraSize / 2f;
            pos.z += _mapCameraSize / 2f;

            //Word space to map space
            pos /= _mapCameraSize;

            //Update shader value
            _materials.ForEach(x => x.SetVector("_MainQuestOffset", new Vector4(pos.x, pos.z, 0, 0)));
        }
    }

    public void SetSideQuest(bool active, Vector3? worldPosition = null)
    {
        if (_materials == null)
            return;

        //Activate / deactivate side quest icon
        _materials.ForEach(x => x.SetInt("_SideQuestActive", active ? 1 : 0));
        if (!active)
            return;

        if (worldPosition != null)
        {
            //Calculate quest position around minimap world space center
            var pos = (worldPosition.Value - _camOffset);
            pos.y = 0;
            pos.x += _mapCameraSize / 2f;
            pos.z += _mapCameraSize / 2f;

            //Word space to map space
            pos /= _mapCameraSize;

            //Update shader value
            _materials.ForEach(x => x.SetVector("_SideQuestOffset", new Vector4(pos.x, pos.z, 0, 0)));
        }
    }
}
