using System;
using System.Collections;
using System.Collections.Generic;
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

    public UI_Boss UI_BOSS;
    

    [Header("현재 상태")]
    public BossState CurrentState = BossState.Deactive;
    public float MaxHealth = 100f;
    public int Defense = 10;

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
    
[Header("피격시 데미지")]
[SerializeField]private List<Material> _materials;
[SerializeField]private List<Color> _originalColors;
private Coroutine _colorChangeCoroutine;
private float _colorChangeTime;
private float _timer;
private bool _isChangingColor = false;
    

    [Header("보스 정보")]
    [SerializeField] private int _maxHealth;

    [Header("공격")]
    [SerializeField] private int _damageValue = 10;
    [SerializeField] private float _attackDelayTime = 3f;
    [SerializeField] private float _attackRange = 10f;

    [Header("죽음")] 
    [SerializeField] private float _disappearTime = 5f;
    
    [Header("코인")]
    public GameObject coinPrefab;
    public float spawnRadius = 5f;
    public int numberOfCoinsToSpawn = 10;
    public float jumpHeight = 2f;
    public float bezierDuration = 0.8f;

    private NavMeshAgent _agent;
    private GameObject _player;
    [SerializeField]private int _currentHealth;
    private Animator _animator;
    private float _attackDelayTimer = 0f;
    private Collider _collider;

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
        _collider = GetComponent<Collider>();
        FindAllMaterials(); // 머티리얼 배열 업데이트 (동적으로 렌더러가 추가/제거될 경우 대비)
    }
    private void Start()
    {
        _currentHealth = _maxHealth;
        UI_BOSS.Refresh_HPBar(_currentHealth);
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
            case BossState.Die:
                if (_agent.enabled == true)
                {
                    _agent.isStopped = true;
                    _collider.enabled = false;
                    _agent.enabled = false;
                }

                break;
        }
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
            
            case BossState.Die:
                _animator.SetTrigger("Die");
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
            GameObject ghost = Instantiate(_ghost, _ghostTransform);
            ghost.transform.SetParent(null);
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
        
       // ChangeState(BossState.Trace); // 공격 후 공격 대기 상태로 전환
    }

    public void TakeDamage(Damage damage)//데미지를 입는다
    {
        if (CurrentState == BossState.Die || CurrentState == BossState.Summon || CurrentState == BossState.Deactive || CurrentState == BossState.Active) return;
        
        Debug.Log($"BOSS : Damaged! Received {damage.Value} damage.");
        _currentHealth -= damage.Value/Defense;


        StartColorChange(0.1f);
        
        
        UI_BOSS.Refresh_HPBar(_currentHealth);
        if (_currentHealth <= 0)
        {
            ChangeState(BossState.Die);
        }
        
    }

    public void Die()
    {
        Debug.Log("BOSS : Died!");
        _animator.SetTrigger("Die");
        
        
    }
    

    
    IEnumerator DieEvent()//죽었을 때 일어나야하는 이벤트
    {
        
            SpawnSomeCoins();
        

        // 보스 사라지는 코루틴 시작 (코인 생성과 동시에 시작)
        StartCoroutine(BossDisappear());

        yield return null; // 코인 생성과 보스 사라짐 코루틴이 시작된 후 즉시 종료
    }

    private void SpawnSomeCoins()
    {
        Coin.SpawnCoinsInArea(new Vector3(transform.position.x,0.1f,transform.position.z), spawnRadius, numberOfCoinsToSpawn, coinPrefab, jumpHeight, bezierDuration);
    }
    
    // 보스를 사라지게 하는 코루틴 (예시)
    IEnumerator BossDisappear()
    {
        // 사라지기 전 대기 시간 (선택 사항)
        yield return new WaitForSeconds(_disappearTime);

        // Fade Out 애니메이션 또는 다른 사라지는 효과 처리 (예시: 알파 값 조절)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // MaterialPropertyBlock을 사용하여 머티리얼의 알파 값을 조절하는 방식 권장
            // 여기서는 단순화를 위해 머티리얼 직접 접근 (주의: 공유 머티리얼 수정은 모든 인스턴스에 영향)
            if (renderer.material.HasProperty("_Color"))
            {
                Color color = renderer.material.color;
                float alpha = 1f;
                while (alpha > 0f)
                {
                    alpha -= Time.deltaTime * 0.5f; // 사라지는 속도 조절
                    color.a = alpha;
                    renderer.material.color = color;
                    yield return null;
                }
            }
        }

        // 모든 Fade Out 효과가 끝난 후 오브젝트 제거
        Destroy(gameObject);
    }
    
    


    // 자신과 자식들의 모든 렌더러에서 모든 머티리얼을 찾아 리스트에 저장
    private void FindAllMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        _materials.Clear();
        _originalColors.Clear();

        foreach (Renderer renderer in renderers)
        {
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                foreach (Material material in renderer.materials)
                {
                    _materials.Add(material);
                    _originalColors.Add(material.color);
                }
            }
        }
    }

    public void StartColorChange(float duration)
    {
        if (_isChangingColor) return;

        FindAllMaterials(); // 머티리얼 리스트 업데이트

        if (_materials.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name} 또는 자식 오브젝트에 렌더러가 없습니다.", this);
            return;
        }

        _colorChangeTime = duration;
        _timer = 0f;
        _isChangingColor = true;

        // 모든 머티리얼의 색상을 빨간색으로 변경하고 코루틴 시작
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null)
            {
                _materials[i].color = Color.red;
            }
        }

        StartCoroutine(ChangeColorCoroutine());
    }

    private System.Collections.IEnumerator ChangeColorCoroutine()
    {
        while (_timer < _colorChangeTime)
        {
            _timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(_timer / _colorChangeTime);

            // 빨간색에서 흰색으로 보간
            Color currentColor = Color.Lerp(Color.red, Color.white, normalizedTime);

            // 모든 머티리얼의 색상 업데이트
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null)
                {
                    _materials[i].color = currentColor;
                }
            }

            yield return null;
        }

        // 색상 변경 완료 후 원래 색상으로 복원 (선택 사항)
        // for (int i = 0; i < _materials.Count; i++)
        // {
        //     if (_materials[i] != null && i < _originalColors.Count)
        //     {
        //         _materials[i].color = _originalColors[i];
        //     }
        // }

        _isChangingColor = false;
    }
    
}