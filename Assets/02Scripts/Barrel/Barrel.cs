using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Barrel : MonoBehaviour
{
    [SerializeField] private int _health = 10;
    [SerializeField] private int _damage = 10;
    private Tweener _tweener;

    public GameObject ExplosionEffect;
    private Rigidbody _rb;
    private bool isExploded = false;
    
    [Header("조절할 파라미터")]
    [SerializeField] private float _radius;
    [SerializeField] private float _time;
    [SerializeField] private float _explosionPower = 2f;
    private void Awake()
    {
        _tweener = transform.DOShakeRotation(0.1f, 1f).SetAutoKill(false).Pause().SetRelative();
        _rb = GetComponent<Rigidbody>();
    }

    private void Explode()
    {
        Debug.Log("EXPLODE");
        Instantiate(ExplosionEffect,this.transform.position,Quaternion.identity);
        
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, _radius);
        
        
        
        foreach (Collider victim in hitColliders)
        {

            isExploded = true;
            if (victim.CompareTag("Player"))
            {
                PlayerData playerData = victim.GetComponent<PlayerData>();
                Damage damage = new Damage();
                damage.Value = _damage;
                
                playerData.TakeDamage(damage);
            }

            if (victim.CompareTag("Enemy"))
            {
                Enemy enemy = victim.GetComponent<Enemy>();
                Damage damage = new Damage();
                damage.Value = _damage;
                
                enemy.TakeDamage(damage);
            }

            if (victim.CompareTag("Barrel"))
            {
                Barrel barrel = victim.GetComponent<Barrel>();
                Damage damage = new Damage();
                damage.Value = _damage;
                barrel.TakeDamage(damage);
            }
        }
        _rb.AddForce(
            new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized * _explosionPower,
            ForceMode.Impulse
        );
        _rb.AddTorque(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized * _explosionPower,
            ForceMode.Impulse);

        StartCoroutine(Disappear());

    }

    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(_time);
        this.gameObject.SetActive(false);
    }
    
    
    public void TakeDamage(Damage damage)
    {
        _health -= damage.Value;
        _tweener.Restart();
        Debug.Log(_health);
        
        if(_health <= 0 && !isExploded) Explode();
        
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 붉은색, 반투명
        Gizmos.DrawSphere(transform.position, _radius);
    
        Gizmos.color = Color.red; // 테두리 선은 불투명한 빨간색
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
