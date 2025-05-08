using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHP : MonoBehaviour
{
    [SerializeField] private Slider _healthBar;
    public PlayerCore Player;
    public void Refresh_HPBar(float currentHP)
    {
        _healthBar.maxValue = Player.MaxHealth;
        _healthBar.value = currentHP;

    }
    
}
