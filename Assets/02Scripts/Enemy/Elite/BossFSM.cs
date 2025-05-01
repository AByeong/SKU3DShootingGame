using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

public class BossFSM : MonoBehaviour, IDamagable
{
    public enum BossState
    {
        Deactive,
        Active,
        Idle,
        Trace,
        Summon,
        Rush,
        Attack,
        RushAttack,
        Damaged,
        Die
    }

    [Header("현재 상태")]
    public BossState CurrentState = BossState.Deactive;

    [Header("활성화 파라미터")]
    [SerializeField] private float _activateRange = 10f;

    [Header("추적모드의 파라미터")]
    [SerializeField] private float _walkingSpeed = 3.0f;

    [Header("소환 파라미터")]
    [SerializeField] private GameObject _ghost;
    [SerializeField] private int _summonCount;
    [SerializeField] private Transform _ghostTransform;

    [Header("돌진")]
    [SerializeField] private bool _rushable = true;
    [SerializeField] private float _rushSpeed = 10.0f;
    public float RushRange;
    public Vector3 RushTransform; // 돌진 목표 위치 (추적 시작 시점의 플레이어 위치)
    [SerializeField] private int _rushDamage = 20;
    

    

    [Header("보스 정보")]
    [SerializeField] private int _maxHealth;

    [Header("공격")]
    [SerializeField] private int _damageValue = 10;
    [SerializeField] private float _attackDelayTime = 3f;
    [SerializeField] private float _attackRange = 10f;

    [Header("코인")]
    public GameObject coinPrefab;
    public float spawnRadius = 5f;
    public int numberOfCoinsToSpawn = 10;
    public float jumpHeight = 2f;
    public float bezierDuration = 0.8f;

    private NavMeshAgent _agent;
    private GameObject _player;
    private int _currentHealth;
    private Animator _animator;
    private float _attackDelayTimer = 0f;

    public enum Trigger
    {
        Activate,
        Rush,
        RushAttack,
        Idle,
        Walking,
        Attack,
        Spell
    }
    private Trigger _trigger;
    public string[] Triggers = {
        "Activate",
        "Rush",
        "RushAttack",
        "Idle",
        "Walking",
        "Attack",
        "Spell"
    };

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case BossState.Deactive:
                if (Vector3.Distance(transform.position, _player.transform.position) < _activateRange)
                {//인식범위에 들어오면 활성화
                    Activate();
                    ChangeState(BossState.Active);
                }
                break;

            case BossState.Trace:

                
                _agent.speed = _walkingSpeed;
                _agent.SetDestination(_player.transform.position);

                if (Vector3.Distance(this.transform.position, _player.transform.position) < _attackRange)
                {
                    
                    ChangeState(BossState.Attack);
                }
                break;
            

            //case BossState.AttackDelay:
                // _agent.isStopped = true;
                //
                // _attackDelayTimer += Time.deltaTime;
                //
                // if (Vector3.Distance(transform.position, _player.transform.position) >= _attackRange)
                // {
                //     _agent.isStopped = false;
                //     ChangeState(BossState.Trace);
                // }
                //
                // if (_attackDelayTimer > _attackDelayTime && Vector3.Distance(transform.position, _player.transform.position) < _attackRange)
                // {_agent.isStopped = false;
                //     Debug.Log("BOSS : Attack!");
                //     _attackDelayTimer = 0f;
                //     ChangeState(BossState.Attack);
                // }
             //   break;
            
            case BossState.Attack:

                break;
            
            case BossState.Rush:
                if (Vector3.Distance(transform.position, RushTransform) < 0.1f)
                {
                    ChangeState(BossState.RushAttack);
                }
                break;
            case BossState.Idle:
                
                if (_rushable)
                {
                    _rushable = false;
                    ChangeState(BossState.Rush);
                }
                break;
        }
    }

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void ChangeState(BossState newState)
    {
        Debug.Log($" BOSS : {CurrentState} -> {newState}");
        CurrentState = newState;

        switch (newState)
        {
            case BossState.Idle:
                _animator.SetTrigger("Idle");
                break;

            case BossState.Trace:
                _animator.SetTrigger("Walking");

                

                if (Vector3.Distance(this.transform.position, _player.transform.position) < _attackRange)
                {
                    ChangeState(BossState.Attack);
                }
                
                
                
                break;

            case BossState.Rush:
                _animator.SetTrigger("Rush");
                _agent.speed = _rushSpeed;
                RushTransform = _player.transform.position;
                _agent.SetDestination(RushTransform); // 저장된 위치로 이동
                break;
            
            case BossState.RushAttack:
                _animator.SetTrigger("RushAttack");
                break;
            
            case BossState.Attack:
                _animator.SetTrigger(Triggers[(int)Trigger.Attack]);
                break;
        }
    }

    public void Activate()//처음 비활성화 상태에서 활성화시키기
    {
        _animator.SetTrigger(Triggers[(int)Trigger.Activate]);
    }

    public void Summon()//잡몹을 소환함
    {
        //_animator.SetTrigger(Triggers[(int)Trigger.Spell]);
        for (int i = 0; i < _summonCount; i++)
        {
            Instantiate(_ghost, _ghostTransform);
        }
    }

    public void Rush()//Rush가능한 상태인데 RushRange밖에 있으면 Rush한다
    {
       
        if (Vector3.Distance(transform.position, RushTransform) < 0.5f) // 목표 지점에 충분히 가까워졌으면
        {
            ChangeState(BossState.RushAttack);
        }
    }

    public void RushAttack()//돌진시에 있던 플레이어의 위치에서 공격한다.
    {
        // 공격 전에 플레이어와의 현재 거리를 확인
        if (Vector3.Distance(transform.position, _player.transform.position) < _attackRange)
        {
            Debug.Log("BOSS : Rush Attack!");
            Damage damage = new Damage();
            damage.Value = _rushDamage;
            _player.GetComponent<PlayerCore>().TakeDamage(damage);
        }
        else
        {
            Debug.Log("BOSS : Rush Attack 실패! 공격 범위 벗어남.");
            ChangeState(BossState.Trace); // 공격 실패 시 추격 상태로 전환 (선택 사항)
        }
    }

    public void Trace()
    {
        _agent.speed = _walkingSpeed;
        _agent.SetDestination(_player.transform.position);

        if (Vector3.Distance(this.transform.position, _player.transform.position) < _attackRange)
        {
            ChangeState(BossState.Attack);
        }
    }

    public void Attack()//공격가능 거리에 들어오면 공격한다.
    {
        Debug.Log("BOSS : Normal Attack!");
        if (Vector3.Distance(_player.transform.position, this.transform.position) < _attackRange)
        {
            Damage damage = new Damage();
            damage.Value = _damageValue;
            _player.GetComponent<PlayerCore>().TakeDamage(damage);
        }
        
        ChangeState(BossState.Trace); // 공격 후 공격 대기 상태로 전환
    }

    public void TakeDamage(Damage damage)//데미지를 입는다
    {
        Debug.Log($"BOSS : Damaged! Received {damage.Value} damage.");
        _currentHealth -= damage.Value;
        if (_currentHealth <= 0)
        {
            ChangeState(BossState.Die);
        }
        else
        {
            ChangeState(BossState.Damaged); // 피격 상태로 전환 (애니메이션 등을 위해)
            // 필요하다면 피격 애니메이션 재생 후 이전 상태로 복귀하는 로직 추가
        }
    }

    public void Die()//죽음
    {
        Debug.Log("BOSS : Died!");
        _animator.SetTrigger("Die"); // 죽음 애니메이션 재생 (애니메이션 컨트롤러에 "Die" 트리거 필요)
        StartCoroutine(DieEvent());
        // 죽음 후 추가적인 처리 (코인 생성 등)는 DieEvent 코루틴에서 처리
    }

    IEnumerator DieEvent()//죽었을 때 일어나야하는 이벤트
    {
        // 코인 생성 로직
        for (int i = 0; i < numberOfCoinsToSpawn; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * spawnRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // 코인이 땅 밑으로 파고들지 않도록 y값은 양수로

            Vector3 spawnPosition = transform.position + randomOffset;
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            // 베지어 곡선 이동 로직 (선택 사항)
            if (jumpHeight > 0 && bezierDuration > 0)
            {
                StartCoroutine(MoveCoinWithBezier(coin.transform, spawnPosition, spawnPosition + Vector3.up * jumpHeight, transform.position + Vector3.up * (jumpHeight / 2f) + UnityEngine.Random.insideUnitSphere * (spawnRadius / 2f), bezierDuration));
            }
        }
        yield return null;
        Destroy(gameObject); // 보스 오브젝트 제거
    }

    IEnumerator MoveCoinWithBezier(Transform target, Vector3 p0, Vector3 p1, Vector3 p2, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            Vector3 point1 = Vector3.Lerp(p0, p2, t);
            Vector3 point2 = Vector3.Lerp(p2, p1, t);
            target.position = Vector3.Lerp(point1, point2, t);
            yield return null;
        }
        target.position = p1; // 최종 위치 보정
    }
}