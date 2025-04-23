using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Enemy : MonoBehaviour
{
    
    public enum EnemyState
    {
        //0 : 대기
        //1 : 추적
        //2 : 복귀
        //3 : 공격
        //4 : 피격
        //5 : 사망
         
        Idle,
        Patrol,
        Trace,
        Return,
        Attack,
        Damaged,
        Die
    }
    
    public List<Transform> PatrolPoints = new List<Transform>();
    private int _patrolTargetPointIndex;
    
    public EnemyState CurrentState = EnemyState.Idle;
    
    private GameObject _player;                             //플레이어
    private CharacterController _characterController;       //캐릭터 컨트롤러
    
    public float MoveSpeed = 3.3f;                          //이동속도
    public float FindDistance = 7f;                         //시야거리
    public float AttackDistance = 2.5f;                     //공격범위
    public float ReturnDistance = 10f;                      // 돌아갈 경계
    public float AttackCoolTime = 1f;                       //공격 쿨타임
    private float _attackTimer = 0f;
    
    public int Health = 100;
    public float DamagedTime = 0.5f;                        //경직시간
    public float DeathTime = 2f;
    
    public float IdleTime = 1f;
    private float _idleTimer = 0f;
    
    
    private Vector3 _startPosition;
    
    private Vector3 _knockbackDirection;
    private float _knockbackForce;
    
    
    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _characterController = GetComponent<CharacterController>();
        _startPosition = transform.position;
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
            {
                Idle();
                break;
            }

            case EnemyState.Patrol:
            {
                Patrol();
                break;
            }

            case EnemyState.Trace:
            {
                Trace();
                break;
            }
            case EnemyState.Return:
            {
                Return();
                break;
            }
            case EnemyState.Attack:
            {
                Attack();
                break;
            }
            case EnemyState.Damaged:
            {
                
                break;
            }
            case EnemyState.Die:
            {
                break;
            }
            
            
        }
    }


    public void TakeDamage(Damage damage)
    {
        if (CurrentState == EnemyState.Damaged || CurrentState == EnemyState.Die)
        {
            return;
        }
        Health -= damage.Value;
        _knockbackDirection = -damage.HitDirection.normalized; 
        _knockbackForce = damage.KnockBackPower;

        if (Health <= 0)
        {
            CurrentState = EnemyState.Die;
            StartCoroutine(Die_Coroutine());
        }
        else
        {
            Debug.Log($"{CurrentState}-> Damaged");

            CurrentState = EnemyState.Damaged;
            StopCoroutine("Damaged_Coroutine");

            StartCoroutine(Damaged_Coroutine());
        }
    }

    
    
    
    private void Idle()
    {
        _idleTimer += Time.deltaTime;
        //행동 : 가만히 있으
        
        if(Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            Debug.Log("Idle -> Trace");
            CurrentState = EnemyState.Trace;
        }

        if (_idleTimer >= IdleTime)
        {
            Debug.Log("Idle -> Patrol");
            CurrentState = EnemyState.Patrol;
            _idleTimer = 0f;
            _patrolTargetPointIndex = 0;
        }
    }

    private void Patrol()
    {
        if(Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            Debug.Log("Patrol -> Trace");
            CurrentState = EnemyState.Trace;
            return;
        }

        
        PatrolMove();
    }

    private void PatrolMove()
    {
        
        if (Vector3.Distance(transform.position, PatrolPoints[_patrolTargetPointIndex].position) <= 0.1f)
        {
            Debug.Log($"{_patrolTargetPointIndex}에 도착");
            this.transform.position = PatrolPoints[_patrolTargetPointIndex].position;
            _patrolTargetPointIndex = (_patrolTargetPointIndex+1)%PatrolPoints.Count;
        }
        
        Vector3 dir = (PatrolPoints[_patrolTargetPointIndex].position - transform.position).normalized;
        _characterController.Move(dir * MoveSpeed * Time.deltaTime);
        
        Debug.Log(Vector3.Distance(transform.position, PatrolPoints[_patrolTargetPointIndex].position));
        //Debug.Log($"{_patrolTargetPointIndex}로 움직이는 중");
        
        
        
    }
    
    private void Trace()
    {
        
        
        //전이 : 공격 사정거리에 들어온다면 ->Attack
        if(Vector3.Distance(transform.position, _player.transform.position) < AttackDistance)
        {
            Debug.Log("Trace -> Attack");
            CurrentState = EnemyState.Attack;
            return;
        }
        
        
        
        //전이 : 플레이어와 멀어지면 -Return
        if(Vector3.Distance(transform.position, _player.transform.position) >= ReturnDistance)
        {
            Debug.Log("Trace -> Return");
            CurrentState = EnemyState.Return;
            return;
        }
        
        
        //행동 : 플레이어를 추적한다
        Vector3 dir = (_player.transform.position - transform.position).normalized;
        _characterController.Move(dir * MoveSpeed * Time.deltaTime);
        
       
    }

    private void Return()
    {
        if (Vector3.Distance(transform.position, _startPosition) <= 0.1f)
        {
            Debug.Log("Return -> Idle");
            transform.position = _startPosition;
            CurrentState = EnemyState.Idle;
            return;
        }
        if(Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            Debug.Log("Idle -> Trace");
            CurrentState = EnemyState.Trace;
            return;
        }
        //해동 : 처음 자리로 돌아간다
        Vector3 dir = (_startPosition - transform.position).normalized;
        _characterController.Move(dir * MoveSpeed * Time.deltaTime);
    }

    private void Attack()
    {
        
        if(Vector3.Distance(transform.position, _player.transform.position) >= AttackDistance)
        {
            Debug.Log("Attack -> Trace");
            CurrentState = EnemyState.Trace;
            _attackTimer = 0;
            return;
        }
        
        
        
        //행동 : 플레잉를 공격한다
        
        _attackTimer += Time.deltaTime;

        if (_attackTimer > AttackCoolTime)
        {
            Debug.Log("플레이어 공격");
            _attackTimer = 0;
        }
    }

    private IEnumerator Damaged_Coroutine()
    {

        while (_knockbackForce > 0)
        {
            _characterController.Move(_knockbackDirection * _knockbackForce * Time.deltaTime);
            _knockbackForce -= Time.deltaTime;
        }
        
        yield return new WaitForSeconds(DamagedTime);
            Debug.Log("Damaged -> Trace");
            CurrentState = EnemyState.Trace;
        
        
    }

    private IEnumerator Die_Coroutine()
    {
        //행동 : 죽음
        yield return new WaitForSeconds(DeathTime);
        this.gameObject.SetActive(false);
    }
   
    
}
