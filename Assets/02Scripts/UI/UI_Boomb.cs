using TMPro;
using UnityEngine;

public class UI_Boomb : MonoBehaviour
{

    public TextMeshProUGUI MaxBombCount;
    public TextMeshProUGUI CurrentBombCount;
    public void SetMaxBombCount(int value)
    {
        MaxBombCount.text = $"{value.ToString()}개";
    }
    
    public void ChangeBombCount(int bombCount)
    {
        CurrentBombCount.text = $"{bombCount.ToString()}개";
    }
}
