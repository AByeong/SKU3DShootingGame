using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCore : MonoBehaviour, IDamagable
{
    public PlayerDataSO playerData;
    public UI_PlayerHP UIHP;
    
    [SerializeField] private Image _bloodImage;
    [SerializeField] private Image _coinImage;
    public float BasicSpeed;
    public float DashSpeed;
    public float MaxStamina;
    public float NeedStamina2Roll;
    public float NormalStamina;
    public float WallStamina;
    public float RunningStamina;
    public float RollPower;
    public int MaxHealth;
    public float JumpPower;
    [SerializeField] private int _currentHealth;
    private Coroutine _bleed;
    private Coroutine _coin;
    public int CurrentHealth{
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }


    private void Start()
    {
        UIHP.Refresh_HPBar(_currentHealth);
    }

    public void GetCoin()
    {
        if (_coin != null)
        {
            StopCoroutine(_coin);
        }
        
        
        
        _coin = StartCoroutine(CoinGet());
    }

    public void TakeDamage(Damage damage)
    {
        _currentHealth -= damage.Value;

        if (_bleed != null)
        {
            StopCoroutine(_bleed);
        }
        
        
        
        _bleed = StartCoroutine(Bleed());
        UIHP.Refresh_HPBar(_currentHealth);

        if (_currentHealth <= 0)
        {
            GameManager.Instance.CurrentState = GameManager.GameState.Over;
        }
        
    }

    IEnumerator Bleed()
    {
        float bloodAlpha = 1f;
        _bloodImage.color = new Color(_bloodImage.color.r, _bloodImage.color.g, _bloodImage.color.b, bloodAlpha);

        while (_bloodImage.color.a > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            bloodAlpha -= 0.1f;
            _bloodImage.color = new Color(_bloodImage.color.r, _bloodImage.color.g, _bloodImage.color.b, bloodAlpha);
        }
    }
    
    IEnumerator CoinGet()
    {
        float coinAlpha = 1f;
        _coinImage.color = new Color(_coinImage.color.r, _coinImage.color.g, _coinImage.color.b, coinAlpha);

        while (_coinImage.color.a > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            coinAlpha -= 0.1f;
            _coinImage.color = new Color(_coinImage.color.r, _coinImage.color.g, _coinImage.color.b, coinAlpha);
        }
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
        RollPower = playerData.RollPower;
        MaxHealth = playerData.MaxHealth;
        _currentHealth = MaxHealth;
        JumpPower = playerData.JumpPower;
        _animator = GetComponentInChildren<Animator>();
    }
private Animator _animator;
    private void Update()
    {
         _animator.SetLayerWeight(0, (float)_currentHealth /(float) MaxHealth);
         _animator.SetLayerWeight(2, 1 -(float) _currentHealth / (float)MaxHealth);
         //Debug.Log($"0Layer : {_currentHealth / MaxHealth}," +
         //          $"1Layer : {1 - _currentHealth / MaxHealth}");
        
    }
}