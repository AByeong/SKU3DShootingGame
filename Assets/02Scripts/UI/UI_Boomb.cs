using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Boomb : MonoBehaviour
{

    public TextMeshProUGUI MaxBombCount;
    public TextMeshProUGUI CurrentBombCount;
    
    [SerializeField] private Slider _boombSlider;
    public void SetMaxBombCount(int value)
    {
        MaxBombCount.text = $"{value.ToString()}";
    }
    
    public void ChangeBombCount(int bombCount)
    {
        CurrentBombCount.text = $"{bombCount.ToString()}";
    }

    public void ChargeBomb(float percentage)
    {
        _boombSlider.value = percentage;
        
    }

    public void Refresh(int Max, int Current)
    {
        SetMaxBombCount(Max);
        ChangeBombCount(Current);
    }
}
