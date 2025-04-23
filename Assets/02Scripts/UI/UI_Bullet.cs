using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Bullet : MonoBehaviour
{
    public TextMeshProUGUI MaxBulletCount;
    public TextMeshProUGUI CurrentBulletCount;

    public Slider RerollGaze;
    
    private Coroutine _rerollCoroutine;
    public void SetMaxBulletCount(int value)
    {
        MaxBulletCount.text = $"{value.ToString()}개";
    }
    
    public void ChangeBulletCount(int value)
    {
        CurrentBulletCount.text = $"{value.ToString()}개";
    }

    public void StartReroll(float needTime)
    {
        RerollGaze.value = 0;
        _rerollCoroutine = StartCoroutine(RerollCoroutine(needTime));
    }

    public void StopReroll()
    {
        if (_rerollCoroutine != null)
        {
            StopCoroutine(_rerollCoroutine);
        }
        RerollGaze.value = 0;
    }

    IEnumerator RerollCoroutine(float needTime)
    {
        while (RerollGaze.value <1)
        {
            RerollGaze.value += 1 / 100f;


            yield return new WaitForSeconds(needTime/100f);
        }
        RerollGaze.value = 0;
    }
}
