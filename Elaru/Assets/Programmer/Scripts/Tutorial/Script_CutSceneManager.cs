using UnityEngine;
using UnityEngine.UI;

public class Script_CutSceneManager : MonoBehaviour
{
    //Game objects for black bars
    [SerializeField]
    private Image _bot = null;
    [SerializeField]
    private Image _top = null;
    [SerializeField]
    private GameObject _botEnd = null;
    [SerializeField]
    private GameObject _topEnd = null;

    private Vector3 _topStart = Vector3.zero;
    private Vector3 _botStart = Vector3.zero;

    private bool _isOn = false;

    private void Start()
    {
        //Save start positions of bars
        _topStart = _top.rectTransform.localPosition;
        _botStart = _bot.rectTransform.localPosition;
    }

    //Used in inspector to enable / disable bars
    public void TurnOnOffBars(bool onOff)
    {
        _isOn = onOff;
    }

    private void Update()
    {
        if (_isOn)
        {
            //Move bars to their end position
            _top.transform.localPosition = Vector3.Lerp(_top.transform.localPosition, _topEnd.transform.localPosition, Time.deltaTime * 2.0f);
            _bot.transform.localPosition = Vector3.Lerp(_bot.transform.localPosition, _botEnd.transform.localPosition, Time.deltaTime * 2.0f);
        }
        else
        {
            //move bars back to start position
            _top.transform.localPosition = Vector3.Lerp(_top.transform.localPosition, _topStart, Time.deltaTime * 0.5f);
            _bot.transform.localPosition = Vector3.Lerp(_bot.transform.localPosition, _botStart, Time.deltaTime * 0.5f);
        }
    }
}
