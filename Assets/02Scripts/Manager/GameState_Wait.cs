using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameState_Wait : GameState
{
    [SerializeField] private int WaitTime = 3;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private float _textBigSize = 3f;
    [SerializeField] private float _textSmallingTime = 0.2f;
    [SerializeField] private float _shakePower = 5f;
    [SerializeField] private Image _background;
    private bool _started = false;
    private int _count = 0;
    public override void Excute()
    {
        if (!_started)
        {

            _canvas.gameObject.SetActive(true);
            _count = WaitTime;
            _started = true;
            FadeBackgounrd();
            StartCoroutine(Countdown());
        }
        
    }

    IEnumerator Countdown()
    {
        while (_count > 0)
        {
            _timerText.text = _count.ToString();
            TextAnimation(_timerText.gameObject.transform);
            yield return new WaitForSeconds(1.0f);
            _count--;
        }
        ChangeState(GameManager.GameState.Play);
        _canvas.gameObject.SetActive(false);
    }

    private void FadeBackgounrd()
    {
        _background.DOFade(0f, 3f);
    }
    
    private void TextAnimation(Transform textTransform)
    {
        Sequence sequence = DOTween.Sequence();

        sequence.Append(textTransform.DOScale(new Vector3(_textBigSize, _textBigSize, _textBigSize), 0f));
        sequence.Append(textTransform.DOScale(Vector3.one, _textSmallingTime));
        sequence.Append(textTransform.DOShakeRotation(1 - _textSmallingTime, new Vector3(_shakePower,0,_shakePower)));

        sequence.Play();

    }



}
