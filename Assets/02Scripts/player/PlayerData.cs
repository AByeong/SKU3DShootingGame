using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public PlayerDataSO playerData;

    public float BasicSpeed;
    public float DashSpeed;
    public float MaxStamina;
    public float NeedStamina2Roll;
    public float NormalStamina;
    public float WallStamina;
    public float RunningStamina;
    public float RollStamina;
    public float RollPower;
    public int MaxHealth;
    
    
    [SerializeField] private int _currentHealth;
    public int CurrentHealth{
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }


    public void TakeDamage(Damage damage)
    {
        _currentHealth -= damage.Value;
    }
    
    private void Awake()
    {
        BasicSpeed = playerData.BasicSpeed;
        DashSpeed = playerData.DashSpeed;
        MaxStamina = playerData.MaxStamina;
        NeedStamina2Roll = playerData.NeedStamina2Roll;
        NormalStamina = playerData.NormalStamina;
        WallStamina = playerData.WallStamina;
        RunningStamina = playerData.RunningStamina;
        RollStamina = playerData.RollStamina;
        RollPower = playerData.RollPower;
        MaxHealth = playerData.MaxHealth;
        _currentHealth = MaxHealth;
    }
}