using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization; // NavMeshAgent 사용을 위해 추가
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random; // Random 사용을 위해 추가

public class EliteEnemy : MonoBehaviour, IDamagable
{
    [Header("근접공격 파라미터")]
    public GameObject AttackEffect;
    public Transform AttackTransform;
    public float DamageRange;
    
    [Header("죽음 이벤트 파라미터")]
    public GameObject DeathEffect;
    public float DeathZoneRange;
    public float DeathDamage;
    
    
private Animator _animator;
    public EnemyPool Pool;

    public UI_Elite UIEnemy;
    public ParticleSystem BloodParticles;
    
    // 이동 방식 선택을 위한 Enum
    public enum MovementMode
    {
        CharacterController,
        NavMeshAgent
    }

    // 적의 타입 정의
    public enum EnemyType
    {
        Normal, // 일반적인 적 (정찰, 추적, 복귀, 공격)
        Follow  // 플레이어를 계속 따라오는 적 (복귀 안 함)
    }

    // 적의 상태 정의
    public enum EnemyState
    {
        Idle, Patrol, Trace, Return, Attack, Damaged, Die
    }

    [Header("타입 및 이동 방식")]
    [Tooltip("적의 타입을 선택합니다.")]
    public EnemyType Type = EnemyType.Normal;
    [Tooltip("적의 이동 방식을 선택합니다.")]
    public MovementMode _moveMode = MovementMode.CharacterController;

    [Header("정찰")]
    [Tooltip("정찰 지점으로 사용할 자식 오브젝트들의 Transform 목록")]
    public PatrolPoints PatrolPoints;
    private int _patrolTargetPointIndex;

    [Header("상태 및 기본 설정")]
    [Tooltip("현재 적의 상태 (읽기 전용)")]
    [SerializeField] // Inspector에서 보기 위해 SerializeField 추가
    private EnemyState _currentState = EnemyState.Idle; // 내부 상태 변수
    public EnemyState CurrentState => _currentState; // 외부 읽기 전용 프로퍼티
    [SerializeField] private int _damage;
    [Tooltip("이동 속도")]
    public float MoveSpeed = 3.3f;
    [Tooltip("최대 체력")]
    public int MaxHealth = 100; // 최대 체력 변수 추가
    private int _currentHealth; // 현재 체력 (private)
    public int CurrentHealth => _currentHealth; // 외부 읽기 전용 프로퍼티

    [Header("탐지 및 공격 범위")]
    [Tooltip("플레이어 탐지 거리")]
    public float FindDistance = 7f;
    [Tooltip("공격 가능 거리")]
    public float AttackDistance = 2.5f;
    [Tooltip("초기 위치로 복귀를 시작하는 거리 (Normal 타입만 해당)")]
    public float ReturnDistance = 10f;

    [Header("타이머 및 시간")]
    [Tooltip("공격 쿨타임")]
    public float AttackCoolTime = 1f;
    [Tooltip("피격 시 경직 시간")]
    public float DamagedTime = 0.5f;
    [Tooltip("사망 후 오브젝트 비활성화까지 시간")]
    public float DeathTime = 2f;
    [Tooltip("대기 상태 지속 시간")]
    public float IdleTime = 1f;
    //코인 관련 변수
    [Header("코인")]
    public GameObject coinPrefab;
    public float spawnRadius = 5f;
    public int numberOfCoinsToSpawn = 10;
    public float jumpHeight = 2f;
    public float bezierDuration = 0.8f;
    // 내부 변수
    [SerializeField] private GameObject _player;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private Vector3 _startPosition;
    private float _attackTimer = 0f;
    private float _idleTimer = 0f;

    // 넉백 관련 변수
    private Vector3 _knockbackDirection;
    private float _knockbackForce;
    private Coroutine _damageCoroutine;
    
   


    private void Awake() // Start 대신 Awake에서 컴포넌트 가져오기 (다른 스크립트의 Start에서 참조 시 안전)
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _characterController = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        _startPosition = transform.position;
        _currentHealth = MaxHealth; // 시작 시 체력 초기화

        Pool = FindAnyObjectByType <EnemyPool>();
        
        // 필수 컴포넌트 및 오브젝트 확인
        if (_player == null) { Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그 확인", this); enabled = false; return; }
        // CharacterController와 NavMeshAgent는 모드에 따라 없을 수도 있으므로 경고만 표시
        if (_characterController == null) Debug.LogWarning("CharacterController 컴포넌트 없음", this);
        if (_agent == null) Debug.LogWarning("NavMeshAgent 컴포넌트 없음", this);
        
        DamagedTime = 70 * Time.deltaTime;
    }

    private void Start()
    { 
_animator = GetComponentInChildren<Animator>();
        
        // 이동 모드 설정 (Awake에서 가져온 컴포넌트 기반)
        SetupMovementMode();
        

        // EnemyType에 따른 초기 상태 설정
        if (Type == EnemyType.Normal)
        {
            // 정찰 지점이 있으면 Patrol, 없으면 Idle 시작
            _currentState = (PatrolPoints.patrolPoints.Count > 0) ? EnemyState.Patrol : EnemyState.Idle;
            if (_currentState == EnemyState.Patrol) _patrolTargetPointIndex = 0; // Patrol 시작 시 인덱스 초기화
        }
        else if (Type == EnemyType.Follow)
        {
            _currentState = EnemyState.Trace; // Follow 타입은 바로 추적 시작
        }
        
        Initialize();
        
//         Debug.Log($"{gameObject.name} 초기 상태: {_currentState}");
    }

    public void Initialize()
    {
        _currentHealth = MaxHealth;

        switch (_moveMode)
        {
            case MovementMode.CharacterController:
                _characterController = GetComponent<CharacterController>();
                _characterController.enabled = true;
                break;
            case MovementMode.NavMeshAgent:
                _agent = GetComponent<NavMeshAgent>();
                _agent.enabled = true;
                break;
        }

        switch (Type)
        {
            case EnemyType.Follow:
                _currentState = EnemyState.Trace;
                break;
            
            case EnemyType.Normal:
                _currentState = EnemyState.Idle;
                break;
        }
        UIEnemy.Refresh_HPBar(_currentHealth);
        
    }
    

    // 이동 모드 설정 함수
    private void SetupMovementMode()
    {
        // NavMeshAgent 모드 설정
        if (_moveMode == MovementMode.NavMeshAgent)
        {
            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.speed = MoveSpeed;
                _agent.stoppingDistance = AttackDistance * 0.8f; // 기본 멈춤 거리 (추적/공격용)
                if (_characterController != null) _characterController.enabled = false; // CC 비활성화
//                Debug.Log("NavMeshAgent 모드로 시작", this);
            }
            else // NavMeshAgent 컴포넌트 없음
            {
                Debug.LogError("NavMeshAgent 모드 선택했으나 컴포넌트 없음! CharacterController 모드로 전환 시도", this);
                _moveMode = MovementMode.CharacterController;
                SetupMovementMode(); // 설정 재귀 호출
            }
        }
        // CharacterController 모드 설정
        else
        {
            if (_characterController != null)
            {
                _characterController.enabled = true;
                if (_agent != null) _agent.enabled = false; // NavMeshAgent 비활성화
                Debug.Log("CharacterController 모드로 시작", this);
            }
            else // CharacterController 컴포넌트 없음
            {
                 Debug.LogError("CharacterController 모드 선택했으나 컴포넌트 없음! NavMeshAgent 모드로 전환 시도", this);
                 _moveMode = MovementMode.NavMeshAgent;
                 SetupMovementMode(); // 설정 재귀 호출
            }
        }
    }

    public void Bleed(Vector3 position, Vector3 direction)
    {
        BloodParticles.transform.position = position;
        BloodParticles.transform.rotation = Quaternion.LookRotation(direction);
        BloodParticles.Play();
    }
 


    private void Update()
    {
  

        // 현재 상태에 따른 함수 호출
        switch (_currentState)
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
                
                break;
            case EnemyState.Die:
                break;
        }
    }

    // 데미지 받는 함수
    public void TakeDamage(Damage damage)
    {
        if (_currentState == EnemyState.Die) return;

        _currentHealth -= damage.Value;
        _knockbackDirection = -damage.HitDirection.normalized;
        _knockbackForce = damage.KnockBackPower;
        Debug.Log($"{gameObject.name} 피격! 현재 체력: {_currentHealth}, 받은 데미지: {damage.Value}, 넉백힘: {_knockbackForce}");


        // 기존 피격 코루틴 중지
        if (_damageCoroutine != null) StopCoroutine(_damageCoroutine);

        // NavMeshAgent 사용 시 현재 이동 중지
        if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        if (_currentHealth <= 0)
        {
            _animator.SetTrigger("Die");
            SpawnSomeCoins();
            ChangeState(EnemyState.Die); // 상태 변경 함수 사용
            StartCoroutine(Die_Coroutine());
        }
        else
        {
            _animator.SetTrigger("Hit");
            ChangeState(EnemyState.Damaged); // 상태 변경 함수 사용
            _damageCoroutine = StartCoroutine(Damaged_Coroutine());
        }
        
        UIEnemy.Refresh_HPBar(_currentHealth);
    }

    // 상태 변경 함수 
    private void ChangeState(EnemyState newState)
    {
        if (_currentState == newState) return; // 같은 상태로 변경 방지

        Debug.Log($"{gameObject.name}: {_currentState} -> {newState}");
        _currentState = newState;

        // 상태 변경 시 필요한 초기화 로직 
        switch (newState)
        {
            case EnemyState.Idle:
                _idleTimer = 0f;
                break;
            case EnemyState.Patrol:
                 _patrolTargetPointIndex = 0; // 순찰 시작 시 인덱스 리셋
                 break;
            case EnemyState.Attack:
                _attackTimer = 0f; // 공격 상태 진입 시 타이머 리셋
                break;
            case EnemyState.Trace:
                // 추적 시작 시 특별히 할 일 (필요 시 추가)
                break;
             case EnemyState.Return:
                // 복귀 시작 시 특별히 할 일 (필요 시 추가)
                break;
        }
    }

    // --- 상태별 함수 ---

    private void Idle()
    {

        _idleTimer += Time.deltaTime;

        // 플레이어 탐지 시 Trace 상태로 전환
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            _animator.SetTrigger("IdleToMove");
            ChangeState(EnemyState.Trace);
            return;
        }

        // 대기 시간 초과 및 정찰 지점 존재 시 Patrol 상태로 전환
        if (PatrolPoints.patrolPoints.Count > 0 && _idleTimer >= IdleTime)
        {
            _animator.SetTrigger("IdleToMove");
            ChangeState(EnemyState.Patrol);
        }
    }

    private void Patrol()
    {
        // 플레이어 탐지 시 Trace 상태로 전환
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            ChangeState(EnemyState.Trace);
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled) _agent.ResetPath();
            return;
        }
        
        if (PatrolPoints.patrolPoints.Count == 0)
        {
            Type = EnemyType.Follow;
        }

        // 목표 정찰 지점 설정
        Vector3 targetPosition = PatrolPoints.patrolPoints[_patrolTargetPointIndex];

        // --- 이동 및 도착 판정 로직 ---
        if (_moveMode == MovementMode.NavMeshAgent)
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.stoppingDistance = 0.1f; // 정찰 시 도착 판정용 멈춤 거리
                _agent.isStopped = false;
                _agent.SetDestination(targetPosition);

                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    // 다음 지점으로 인덱스 변경
                    _patrolTargetPointIndex = (_patrolTargetPointIndex + 1) % PatrolPoints.patrolPoints.Count;
                }
            }
        }
        else // CharacterController 모드
        {
            if (_characterController != null && _characterController.enabled)
            {
                if (Vector3.Distance(transform.position, targetPosition) <= 0.2f) // 도착 판정
                {
                    // 다음 지점으로 인덱스 변경
                     _patrolTargetPointIndex = (_patrolTargetPointIndex + 1) % PatrolPoints.patrolPoints.Count;
                     
                }
                else
                {
                    // 이동 및 바라보기
                    Vector3 dir = (targetPosition - transform.position).normalized;
                    _characterController.Move(dir * MoveSpeed * Time.deltaTime);
                    if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }
    }

    private void Trace()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        bool isInAttackRange = false;
        bool shouldReturn = (Type == EnemyType.Normal) && (distanceToPlayer >= ReturnDistance);

        if (_moveMode == MovementMode.NavMeshAgent)
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.stoppingDistance = AttackDistance * 0.8f;
                _agent.isStopped = false;
                _agent.SetDestination(_player.transform.position);
                isInAttackRange = (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);
            }
        }
        else
        {
            if (_characterController != null && _characterController.enabled)
            {
                isInAttackRange = (distanceToPlayer <= AttackDistance);
                if (!isInAttackRange)
                {
                    Vector3 dir = (_player.transform.position - transform.position).normalized;
                    _characterController.Move(dir * MoveSpeed * Time.deltaTime);
                }
            }
        }

        // **추가: 항상 플레이어 바라보기**
        Vector3 dirToPlayer = (_player.transform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (isInAttackRange)
        {
            _animator.SetTrigger("MoveToAttackDelay");
            ChangeState(EnemyState.Attack);
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled)
            {
                _agent.isStopped = true;
            }
        }
        else if (shouldReturn)
        {
            _animator.SetTrigger("MoveToIdle");
            ChangeState(EnemyState.Return);
        }
    }

    private void Return()
    {
        // 플레이어 재탐지 시 Trace 상태로 전환
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            ChangeState(EnemyState.Trace);
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled) _agent.ResetPath();
            return;
        }

        // --- 이동 및 도착 판정 ---
        bool hasReturned = false;
        if (_moveMode == MovementMode.NavMeshAgent)
        {
            if (_agent != null && _agent.enabled)
            {
                 _agent.stoppingDistance = 0.1f; // 복귀 시 도착 판정용 멈춤 거리
                 _agent.isStopped = false;
                 _agent.SetDestination(_startPosition); // 시작 위치로 이동
                 hasReturned = (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);
            }
        }
        else // CharacterController 모드
        {
             if (_characterController != null && _characterController.enabled)
             {
                 hasReturned = (Vector3.Distance(transform.position, _startPosition) <= 0.2f);
                 if (!hasReturned)
                 {
                     Vector3 dir = (_startPosition - transform.position).normalized;
                     _characterController.Move(dir * MoveSpeed * Time.deltaTime);
                     if(dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
                 }
             }
        }

        // 시작 위치 도착 시 Idle 상태로 전환
        if (hasReturned)
        {
            ChangeState(EnemyState.Idle);
            // 도착 시 위치/회전 보정
            transform.position = _startPosition;
            transform.rotation = Quaternion.identity; // 또는 시작 시 회전값 저장 후 사용
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }
    }

    private void Attack()
    {
        // NavMeshAgent 사용 시 이동 멈춤 보장
        if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled && !_agent.isStopped)
        {
           
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // 플레이어 바라보기
        Vector3 lookDir = (_player.transform.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        // 공격 거리 벗어나면 Trace 상태로 전환
        if (Vector3.Distance(transform.position, _player.transform.position) >= AttackDistance)
        {
            _animator.SetTrigger("AttackDelayToMove");
            ChangeState(EnemyState.Trace);
            return;
        }

        // 공격 쿨타임 및 실행
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= AttackCoolTime)
        {
            _attackTimer = 0;
           PerformAttack(); // 실제 공격 실행 함수 호출
        }
    }

    // 실제 공격 로직 함수 (분리)
    private void PerformAttack()
    {
        _animator.SetTrigger("AttackDelayToAttack");
        
        
        Debug.Log("플레이어 공격 실행!");
    }

    public void Hit()
    {
        Damage damage = new Damage();
        damage.Value = _damage;
        
        Instantiate(AttackEffect, AttackTransform);//공격 이팩트 생성

        if (Vector3.Distance(AttackTransform.position, _player.transform.position) < DamageRange)
        {
            Debug.Log($"{Vector3.Distance(AttackTransform.position, _player.transform.position)}의 공격과의 거리");
            _player.GetComponent<PlayerCore>().TakeDamage(damage);
        }
        
        
        
    }

    // --- 코루틴 ---

    private IEnumerator Damaged_Coroutine()
    {
        // NavMeshAgent 비활성화 (넉백 적용 전)
        bool agentWasEnabled = false;
        if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled)
        {
            agentWasEnabled = true;
            _agent.enabled = false; // 넉백 동안 비활성화
        }

        // 넉백 이동
        float knockbackDuration = 0.2f; // 넉백 지속 시간
        float elapsedKnockbackTime = 0f;
        
            // 프레임 이동량 계산 
            float moveAmount = _knockbackForce * Time.deltaTime;

            if (_moveMode == MovementMode.CharacterController && _characterController != null && _characterController.enabled)
            {
                _characterController.Move(_knockbackDirection * moveAmount);
            }
            else 
            {
                
                transform.position += _knockbackDirection * moveAmount;
            }

            elapsedKnockbackTime += Time.deltaTime;
            

        // 남은 경직 시간 대기
        yield return new WaitForSeconds(Mathf.Max(0, DamagedTime - elapsedKnockbackTime));
        
        if (_currentState == EnemyState.Damaged)
        {
            switch (_moveMode)
            {
                case MovementMode.NavMeshAgent:
                    _agent.enabled = true;
                    break;
                case MovementMode.CharacterController:
                    _characterController.enabled = true;
                    break;
                
            }
            
             ChangeState(EnemyState.Trace);
             // 이동 재개는 Trace()에서 처리됨
        }
        _damageCoroutine = null; // 코루틴 참조 해제
    }
    
    private void SpawnSomeCoins()
    {
        Coin.SpawnCoinsInArea(transform.position, spawnRadius, numberOfCoinsToSpawn, coinPrefab, jumpHeight, bezierDuration);
    }

    private IEnumerator Die_Coroutine()
    {
        Debug.Log($"{gameObject.name} Die");

        // 모든 이동 관련 컴포넌트 비활성화
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;
        }
        if (_characterController != null && _characterController.enabled)
        {
            _characterController.enabled = false;
        }
      

        yield return new WaitForSeconds(DeathTime);

        Debug.Log($"{gameObject.name} Deactivated");
        //Pool.EnemyDie(this);
    }
}