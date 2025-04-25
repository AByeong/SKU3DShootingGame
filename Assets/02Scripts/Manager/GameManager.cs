using System;
using UnityEngine;

public class GameManager : MySingleton<GameManager>
{
    public enum GameState
    {
        Start,
        Wait,
        Play,
        Over
    }


    public GameState_Start Start;
    public GameState_Wait Wait;
    public GameState_Play Play;
    public GameState_Over Over;
    
    public GameState CurrentState = GameState.Start;

  
    
    private void Update()
    {
        switch (CurrentState)
        {
            case GameState.Start:
            {

                Start.Excute();
                break;
            }
            case GameState.Wait:
            {
                Wait.Excute();

                break;
            }
            case GameState.Play:
            {
                
                
                Play.Excute();

                break;
            }
            case GameState.Over:
            {
                Over.Excute();

                break;
            }

        }
    }
}
