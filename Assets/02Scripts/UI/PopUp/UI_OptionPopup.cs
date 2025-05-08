using UnityEngine;

public class UI_OptionPopup : UI_PopUp
{
 
    public  void Open()
    {
        GameManager.Instance.Pause();
        
    }

    public void Close()
    {
        OnClickContinueButton();
    }

    
    public void OnClickContinueButton()
    {
        gameObject.SetActive(false);
        GameManager.Instance.Continue();
    }

    public void OnClickResumeButton()
    {
        gameObject.SetActive(false);
        GameManager.Instance.Restart();
    }

    public void OnClickQuitButton()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }

    public void OnClickCreditButton()
    {
        PopupManager.Instance.PopUpOpen(PopupType.CreditImage);
    }
}
