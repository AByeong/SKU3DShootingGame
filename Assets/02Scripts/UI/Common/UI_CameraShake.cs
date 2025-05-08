using System;
using DG.Tweening;
using UnityEngine;

public class UI_CameraShake : MonoBehaviour
{
    public GameObject Camera;

    private Tweener _tweener;

    public float Duration = 0.1f;
    public float Power = 0.2f;
    private void Start()
    {
        _tweener = this.transform.DOShakePosition(Duration, Power).SetAutoKill(false).Pause().SetRelative();
    }

    public void CameraShake()
    {
        _tweener.Restart();
    }
}
