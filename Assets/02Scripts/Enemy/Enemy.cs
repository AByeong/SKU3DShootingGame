using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization; // NavMeshAgent 사용을 위해 추가
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random; // Random 사용을 위해 추가

public class Enemy : MonoBehaviour
{

    public EnemyPool Pool;
    
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
    public List<Transform> PatrolPoints = new List<Transform>(); // 자식 오브젝트 Transform 리스트
    private List<Vector3> _patrolWorldPositions = new List<Vector3>(); // 시작 시 계산될 고정 월드 좌표
    private int _patrolTargetPointIndex;

    [Header("상태 및 기본 설정")]
    [Tooltip("현재 적의 상태 (읽기 전용)")]
    [SerializeField] // Inspector에서 보기 위해 SerializeField 추가
    private EnemyState _currentState = EnemyState.Idle; // 내부 상태 변수
    public EnemyState CurrentState => _currentState; // 외부 읽기 전용 프로퍼티
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

    // 내부 변수
    private GameObject _player;
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
    }

    private void Start()
    {
        Initialize();
        
        // 이동 모드 설정 (Awake에서 가져온 컴포넌트 기반)
        SetupMovementMode();

        // 정찰 지점 월드 좌표 저장 (자식 오브젝트 기준)
        SetupPatrolPositions();

        // EnemyType에 따른 초기 상태 설정
        if (Type == EnemyType.Normal)
        {
            // 정찰 지점이 있으면 Patrol, 없으면 Idle 시작
            _currentState = (_patrolWorldPositions.Count > 0) ? EnemyState.Patrol : EnemyState.Idle;
            if (_currentState == EnemyState.Patrol) _patrolTargetPointIndex = 0; // Patrol 시작 시 인덱스 초기화
        }
        else if (Type == EnemyType.Follow)
        {
            _currentState = EnemyState.Trace; // Follow 타입은 바로 추적 시작
        }
         Debug.Log($"{gameObject.name} 초기 상태: {_currentState}");
    }

    public void Initialize()
    {
        _currentHealth = MaxHealth;
        _currentState = EnemyState.Idle;
        _moveMode = MovementMode.CharacterController;
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
                Debug.Log("NavMeshAgent 모드로 시작", this);
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

    // 정찰 지점 월드 좌표 저장 함수
    private void SetupPatrolPositions()
    {
        _patrolWorldPositions.Clear(); // 리스트 초기화
        if (PatrolPoints != null)
        {
            foreach (Transform point in PatrolPoints)
            {
                if (point != null)
                {
                    // 자식 Transform의 현재 월드 위치를 저장
                    _patrolWorldPositions.Add(point.position);
                    // 필요하다면 정찰 지점 마커를 비활성화
                    // point.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("PatrolPoints 리스트에 null 항목이 있습니다.", this);
                }
            }
        }
        if (_patrolWorldPositions.Count == 0)
        {
            Debug.LogWarning("유효한 정찰 지점(PatrolPoints)이 설정되지 않았거나 없습니다.", this);
        }
    }


    private void Update()
    {
        // 죽었거나 피격 상태일 때는 상태 업데이트 로직 실행 안 함
        if (_currentState == EnemyState.Die || _currentState == EnemyState.Damaged) return;

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
            ChangeState(EnemyState.Die); // 상태 변경 함수 사용
            StartCoroutine(Die_Coroutine());
        }
        else
        {
            ChangeState(EnemyState.Damaged); // 상태 변경 함수 사용
            _damageCoroutine = StartCoroutine(Damaged_Coroutine());
        }
    }

    // 상태 변경 함수 (디버깅 및 관리 용이)
    private void ChangeState(EnemyState newState)
    {
        if (_currentState == newState) return; // 같은 상태로 변경 방지

        Debug.Log($"{gameObject.name}: {_currentState} -> {newState}");
        _currentState = newState;

        // 상태 변경 시 필요한 초기화 로직 (예: 타이머 초기화)
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
        // NavMeshAgent 사용 시 이동 멈춤 보장
        if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled && !_agent.isStopped)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        _idleTimer += Time.deltaTime;

        // 플레이어 탐지 시 Trace 상태로 전환
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            ChangeState(EnemyState.Trace);
            return;
        }

        // 대기 시간 초과 및 정찰 지점 존재 시 Patrol 상태로 전환
        if (_patrolWorldPositions.Count > 0 && _idleTimer >= IdleTime)
        {
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

        // 정찰 지점 없으면 Idle로 전환 (Start에서 이미 체크했지만 안전하게)
        if (_patrolWorldPositions.Count == 0)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // 목표 정찰 지점 설정
        Vector3 targetPosition = _patrolWorldPositions[_patrolTargetPointIndex];

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
                    _patrolTargetPointIndex = (_patrolTargetPointIndex + 1) % _patrolWorldPositions.Count;
                    // 다음 목적지 설정은 다음 Update에서 SetDestination 호출 시 자동으로 처리됨
                     // Debug.Log($"Patrol: 다음 목표 {_patrolTargetPointIndex}");
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
                     _patrolTargetPointIndex = (_patrolTargetPointIndex + 1) % _patrolWorldPositions.Count;
                     // Debug.Log($"Patrol: 다음 목표 {_patrolTargetPointIndex}");
                     // 위치 보정 (선택 사항)
                     // transform.position = targetPosition;
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

        // 공격 가능 거리 진입 또는 복귀 거리 이탈 확인 변수
        bool isInAttackRange = false;
        bool shouldReturn = (Type == EnemyType.Normal) && (distanceToPlayer >= ReturnDistance); // Follow 타입은 복귀 안 함

        // --- 이동 및 상태 전환 판정 ---
        if (_moveMode == MovementMode.NavMeshAgent)
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.stoppingDistance = AttackDistance * 0.8f; // 추적/공격용 멈춤 거리
                _agent.isStopped = false;
                _agent.SetDestination(_player.transform.position); // 항상 플레이어 추적
                isInAttackRange = (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance);
            }
        }
        else // CharacterController 모드
        {
            if (_characterController != null && _characterController.enabled)
            {
                isInAttackRange = (distanceToPlayer <= AttackDistance);
                if (!isInAttackRange) // 공격 범위 밖일 때만 이동
                {
                    Vector3 dir = (_player.transform.position - transform.position).normalized;
                    _characterController.Move(dir * MoveSpeed * Time.deltaTime);
                    if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
                }
                 else // 공격 범위 안이면 이동 멈춤 (선택 사항)
                 {
                     // 필요 시 여기에 멈춤 로직 추가 (CC는 자동으로 멈추지 않음)
                 }
            }
        }

        // 상태 전환
        if (isInAttackRange)
        {
            ChangeState(EnemyState.Attack);
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled)
            {
                _agent.isStopped = true; // 공격 상태 전 이동 멈춤
                _agent.ResetPath();
            }
            return;
        }
        else if (shouldReturn)
        {
            ChangeState(EnemyState.Return);
            if (_moveMode == MovementMode.NavMeshAgent && _agent != null && _agent.enabled) _agent.ResetPath();
            return;
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
                // Warp으로 NavMesh 위치 동기화 (더 안전)
                if (_agent.isOnNavMesh) _agent.Warp(_startPosition);
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
            ChangeState(EnemyState.Trace);
            // 이동 재개는 Trace()에서 처리됨
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
        Debug.Log("플레이어 공격 실행!");
        // TODO: 애니메이션 재생, 투사체 발사, 데미지 적용 등 실제 공격 내용 구현
        // 예시: 플레이어에게 데미지 주기
        // PlayerHealth playerHealth = _player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        // {
        //     playerHealth.TakeDamage(attackPower); // attackPower 변수 필요
        // }
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

        while (elapsedKnockbackTime < knockbackDuration && _knockbackForce > 0.01f) // 아주 작은 힘은 무시
        {
            // 프레임 이동량 계산 (넉백 힘은 시간에 따라 감소시키지 않음 - 원하면 추가 가능)
            float moveAmount = _knockbackForce * Time.deltaTime;

            if (_moveMode == MovementMode.CharacterController && _characterController != null && _characterController.enabled)
            {
                _characterController.Move(_knockbackDirection * moveAmount);
            }
            else // NavMeshAgent 모드 (비활성화 상태) 또는 CC 없음/비활성
            {
                // transform 직접 이동
                transform.position += _knockbackDirection * moveAmount;
            }

            elapsedKnockbackTime += Time.deltaTime;
            yield return null;
        }

        // NavMeshAgent 재활성화 (넉백 적용 후, 경직 시간 전)
        if (agentWasEnabled)
        {
            _agent.enabled = true;
            // 위치 동기화 (중요) - Warp 사용 추천
            if (_agent.isOnNavMesh)
                 _agent.Warp(transform.position);
            else
                 Debug.LogWarning("넉백으로 인해 Enemy가 NavMesh 밖으로 이동했을 수 있습니다!", this);
        }

        // 남은 경직 시간 대기
        yield return new WaitForSeconds(Mathf.Max(0, DamagedTime - elapsedKnockbackTime));

        // 상태 복귀 (Trace) - Die 상태가 아닐 경우
        if (_currentState == EnemyState.Damaged)
        {
             ChangeState(EnemyState.Trace);
             // 이동 재개는 Trace()에서 처리됨
        }
        _damageCoroutine = null; // 코루틴 참조 해제
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
        // 콜라이더 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // TODO: 죽음 애니메이션, 파티클 등 연출

        yield return new WaitForSeconds(DeathTime);

        Debug.Log($"{gameObject.name} Deactivated");
        Pool.EnemyDie(this);
    }
}