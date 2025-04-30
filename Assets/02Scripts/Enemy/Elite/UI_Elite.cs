using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_Elite : MonoBehaviour
{
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _hpFollowBar;
    public EliteEnemy Enemy;
    private Transform camTransform;
    [SerializeField] private float _followTime= 1;
    
    private float _targetHp;

    void Start()
    {
        _hpBar.maxValue = Enemy.MaxHealth;
        _hpFollowBar.maxValue = Enemy.MaxHealth;
    }
    public void Refresh_HPBar(float currentHP)
    {
        _hpBar.maxValue = Enemy.MaxHealth;
        _hpBar.value = currentHP;
        _targetHp = currentHP;
        StartCoroutine(FollowHP());
    }
   
    private void LateUpdate()
    {
        camTransform = Camera.main.transform;
        transform.LookAt(transform.position + (camTransform.rotation * Vector3.forward));
    }

    

    IEnumerator FollowHP()
    {
        Debug.Log("Follow HP");
        while (_targetHp != _hpFollowBar.value)
        {
            Debug.Log("Minus Elite HP");
            yield return new WaitForSeconds(_followTime / 10f);
            _hpFollowBar.value -= (_hpFollowBar.value - _targetHp)/10f;
        }

    }
}
