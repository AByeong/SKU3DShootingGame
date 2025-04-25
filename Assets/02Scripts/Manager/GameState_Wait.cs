using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameState_Wait : GameState
{
    [SerializeField] private float WaitTime = 3f;
    [SerializeField]private float _timer = 0f;

    public override void Excute()
    {
        _timer += Time.deltaTime;

        if (_timer >= WaitTime)
        {
            _timer = 0f;
            ChangeState(GameManager.GameState.Play);
        }
        
    }

    




}
