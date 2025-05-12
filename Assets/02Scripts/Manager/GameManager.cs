using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MySingleton<GameManager>
{
    public enum GameState
    {
        Start,
        Wait,
        Play,
        Over
    }

    public enum EGameState
    {
        Ready,
        Run,
        Pause,
        Over
    }
    
    


    public GameState_Start Start;
    public GameState_Wait Wait;
    public GameState_Play Play;
    public GameState_Over Over;
    
    public GameState CurrentState = GameState.Start;
    public EGameState CurrentGameState = EGameState.Ready;
    
    
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

    public void Restart()
    {
        
       // int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(0);
    }

    public void Pause()
    { 
        PopupManager.Instance.PopUpOpen(PopupType.UI_OptionPopUp, closeCallback: Continue);
        
        //TODO
        //게임 상태를 Pause로 변환한다.
        CurrentGameState = EGameState.Pause;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        //옵션 팝업을 활성화한다.
        

    }

    public void Continue()
    {
        CurrentGameState = EGameState.Run;
        Time.timeScale = 1;
        
        Cursor.lockState = CursorLockMode.Confined;
    }
    
}
