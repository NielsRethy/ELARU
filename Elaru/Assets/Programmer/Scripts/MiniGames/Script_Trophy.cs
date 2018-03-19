using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_Trophy : MonoBehaviour {
    [SerializeField]
    private GameObject _trophy = null;
    [SerializeField]
    private List<GameObject> _holograms = new List<GameObject>();
    [SerializeField]
    private ItemType _color = ItemType.None;

    private int _amountFound = 0;
    private Renderer _matTrophy = null;


    // Use this for initialization
    void Start () {
        _trophy.SetActive(false);
        _matTrophy = _trophy.GetComponent<MeshRenderer>();
        if (_matTrophy == null)
            _matTrophy = _trophy.GetComponent<SkinnedMeshRenderer>();
        

        foreach (GameObject obj in _holograms)
        {
            obj.SetActive(false);
        }
	}
	
	public void FoundCollectible()
    {
        _amountFound++;
        _holograms[_amountFound - 1].SetActive(true);
        if (_amountFound >= _holograms.Count)
        {
            _trophy.SetActive(true);
                    var mats = _matTrophy.materials;
            switch (_color)
            {
                case ItemType.Black:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.black);
                    }
                    break;
                case ItemType.Blue:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.cyan);
                    }
                    break;
                case ItemType.DarkBlue:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.blue);
                    }
                    break;
                case ItemType.Green:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.green);
                    }
                    break;
                case ItemType.Orange:
                    foreach (Material m in mats)
                    {
                        SetColor(m, new Color(1, 0.5f, 0.31f));
                    }
                    break;
                case ItemType.Pink:
                    foreach (Material m in mats)
                    {
                        SetColor(m, new Color(1, 0.75f, 0.79f));
                    }
                    break;
                case ItemType.Purple:
                    foreach (Material m in mats)
                    {
                        SetColor(m, new Color(1f, 0f, 1f));
                    }
                    break;
                case ItemType.Red:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.red);
                    }
                    break;
                case ItemType.White:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.white);
                    }
                    break;
                case ItemType.Yellow:
                    foreach (Material m in mats)
                    {
                        SetColor(m, Color.yellow);
                    }
                    break;
            }
        }
    }

    private void SetColor(Material mat, Color clr)
    {
        mat.SetColor("_Fresnel", clr);
    }
}
