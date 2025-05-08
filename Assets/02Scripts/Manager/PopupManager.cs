using System;
using System.Collections.Generic;
using UnityEngine;
public enum PopupType
{
    UI_OptionPopUp,
    CreditImage
}
public class PopupManager : MySingleton<PopupManager>
{
    [Header("팝업 UI 참조")] public List<UI_PopUp> PopUps;
    
    public Stack<UI_PopUp> PopupStack = new Stack<UI_PopUp>();


    
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (PopupStack.Count == 0)
            {
                GameManager.Instance.Pause();
            }
            else
            {
                while (true)
                {

                    bool opend = PopupStack.Peek().isActiveAndEnabled;
                    PopupStack.Pop().Close();

                    if (opend || PopupStack.Count == 0)
                    {
                        break;
                    }
                    
                }
            }
        }
    }

    public void PopUpOpen(PopupType popup, Action closeCallback = null)
    {
        Debug.Log(popup.ToString());
        Open(popup.ToString(), closeCallback);
    }

    private void Open(string popUpName, Action closeCallback = null)
    {
        foreach (UI_PopUp popUp in PopUps)
        {
            if (popUp.name == popUpName)
            {
                Debug.Log($"{popUp.ToString()}입니다.");
                popUp.Open(closeCallback);
                PopupStack.Push(popUp);
                return;
            }
        }
        Debug.Log($"{popUpName}은 찾지 못했습니다.");
    }
    
    
    
    
    
}
