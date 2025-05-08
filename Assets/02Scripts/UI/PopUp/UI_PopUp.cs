using System;
using UnityEngine;

public class UI_PopUp : MonoBehaviour
{
    //콜백함수 : 기억해뒀다가 특정 시점 작업이 완료된 이후에 호출하는 함수
    private Action _closeCallback;
    public void Open(Action closeCallback = null)
    {//팝업이 열릴 때, Action을 주며, 닫힐 때, 그 함수를 호출한다.
        _closeCallback = closeCallback;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        _closeCallback?.Invoke();
        gameObject.SetActive(false);
    }
}
