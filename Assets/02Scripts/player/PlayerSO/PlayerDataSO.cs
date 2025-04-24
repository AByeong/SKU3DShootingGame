using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Scriptable Objects/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
    public float BasicSpeed = 5f;
    public float DashSpeed = 10f;
    
    public float MaxStamina = 100f;
    public float NeedStamina2Roll = 10f;
    
    public float NormalStamina = 15f;
    public float WallStamina = -10f;
    public float RunningStamina = -10f;
    public float RollStamina = -10f;
    public float RollPower = 5f;

    public int MaxHealth = 100;
}
