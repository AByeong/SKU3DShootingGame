using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameState_Wait : GameState
{
    [SerializeField] private int WaitTime = 3;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Canvas _canvas;
    
    private bool _started = false;
    private int _count = 0;
    public override void Excute()
    {
        if (!_started)
        {
            _canvas.gameObject.SetActive(true);
            _count = WaitTime;
            _started = true;
            StartCoroutine(Countdown());
        }
        
    }

    IEnumerator Countdown()
    {
        while (_count > 0)
        {
            _timerText.text = _count.ToString();
            yield return new WaitForSeconds(1.0f);
            _count--;
        }
        ChangeState(GameManager.GameState.Play);
        _canvas.gameObject.SetActive(false);
    }
    




}
