using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStamina : MonoBehaviour
{
    public PlayerMove PlayerMove;
    
    public Slider StaminaSlider;

    private void Awake()
    {
        StaminaSlider.maxValue = PlayerMove.MaxStamina;
    }

    private void Update()
    {
        StaminaSlider.value = PlayerMove.PlayerStamina;
    }
}
