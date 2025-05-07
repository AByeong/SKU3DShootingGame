using System;
using System.Collections;
using Unity.VisualScripting; // 만약 Visual Scripting을 사용하지 않는다면 이 줄은 삭제해도 됩니다.
using UnityEngine;
using UnityEngine.Serialization; // 만약 FormerlySerializedAs 등을 사용하지 않는다면 이 줄은 삭제해도 됩니다.
using UnityEngine.UI; // 만약 UI 요소를 코드에서 직접 제어하지 않는다면 이 줄은 삭제해도 됩니다.

public class PlayerMove : MonoBehaviour
{
    public PlayerCore playerCore;
    private Animator _animator;
    [SerializeField]
    private float _moveSpeed = 5f;

    public float BasicSpeed = 5f;
    public float DashSpeed = 10f;

    private CharacterController _characterController;

    private const float GRAVITY = -9.8f; //중력가속도
    private float _yVelocity = 0f; //속도

    public float JumpPower = 10f;
    private int _jumpingCount = 0;

    [SerializeField]
    private float _stamina;
    public float PlayerStamina => _stamina;

    private float _staminaNumber = 3f;
    public float MaxStamina = 100f;
    private float _needStamina2Roll = 10f;

    [SerializeField]
    private float _rollingPower = 0;
    [SerializeField]
    private bool _isWall = false;
    [SerializeField]
    private bool _isGrounded = true; // 이 변수는 Jump()에서 사용되지만, CharacterController.isGrounded를 직접 사용하는 것이 더 일반적입니다.
    [SerializeField]
    private bool _isRunning = false;

    private float _normalStamina = 15f;
    private float _wallStamina = -10f;
    private float _runningStamina = -10f;

    private float _rollPower = 5f;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        // _moveSpeed = BasicSpeed; // Start에서 PlayerCore 값으로 덮어쓰므로 여기서 초기화는 큰 의미 없을 수 있음
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // _stamina = MaxStamina; // PlayerCore에서 MaxStamina를 가져온 후 초기화하는 것이 순서상 맞음

        if (playerCore != null) // Null 체크 추가
        {
            BasicSpeed = playerCore.BasicSpeed;
            DashSpeed = playerCore.DashSpeed;
            MaxStamina = playerCore.MaxStamina;
            _needStamina2Roll = playerCore.NeedStamina2Roll;
            _normalStamina = playerCore.NormalStamina;
            _wallStamina = playerCore.WallStamina;
            _runningStamina = playerCore.RunningStamina;
            _rollPower = playerCore.RollPower;
            JumpPower = playerCore.JumpPower;
        }
        else
        {
            Debug.LogError("PlayerCore가 PlayerMove 스크립트에 할당되지 않았습니다. 기본값을 사용합니다.");
        }
        
        _moveSpeed = BasicSpeed; // PlayerCore 값 할당 후 _moveSpeed 초기화
        _stamina = MaxStamina;   // PlayerCore 값 할당 후 _stamina 초기화

        _yVelocity = GRAVITY; // 사용자의 원래 코드 유지 (시작 시 아래로 중력 적용)
                              // 참고: 일반적으로는 0f나 작은 음수로 시작하는 것이 더 안정적일 수 있습니다.
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play)
        {
            // ★★★ 함수 호출 순서 변경 (미끄러짐 해결에 중요) ★★★
            CheckOnWall();            // 1. 벽 상태 감지 (원래 로직 유지)
            SetGravity();             // 2. 중력 및 착지 시 yVelocity 처리 (핵심 수정)
            Jump();                   // 3. 점프 시 yVelocity 덮어쓰기 (원래 로직 유지)
            
            Run();                    // 4. 달리기 처리 (원래 로직 유지)
            Rolling();                // 5. 구르기 처리 (원래 로직 유지)
            
            BasicCharacterMovement(); // 6. 최종 이동 처리 (계산된 yVelocity 사용)
            
            Stamina();                // 7. 스태미나 처리 (원래 로직 유지)
        }
    }

    public void ShakeCamera()
    {
        // 내용 없음 (원래 코드 유지)
    }

    private void SetGravity()
    {
        // 사용자의 기존 벽타기 관련 yVelocity 처리 유지
        if (_isWall && _stamina > 0)
        {
            _yVelocity = 1;
        }
        else
        {
            // 땅에 있는지 CharacterController.isGrounded로 확인
            if (_characterController.isGrounded)
            {
                // ★★★ 미끄러짐 해결의 핵심 부분 ★★★
                // 땅에 착지했고, yVelocity가 아래로 향하고 있었다면 (즉, 떨어지던 중이었다면)
                // y 속도를 안정적인 작은 음수 값으로 설정하여 지면에 붙도록 함.
                if (_yVelocity < 0.0f)
                {
                    _yVelocity = -2.0f; // 땅에 가볍게 붙어있도록 하는 힘. 이 값을 조절하며 테스트 (-0.5f ~ -5.0f).
                }
                // else if (_yVelocity > 0.0f && _jumpingCount == 0) 
                // {
                //     // 만약 점프 직후가 아닌데 (예: 경사로를 올라간 직후) isGrounded 이고 yVelocity가 양수라면,
                //     // 원치 않는 튀어오름을 방지하기 위해 0 또는 작은 음수로 설정할 수 있습니다.
                //     // _yVelocity = -0.5f;
                // }
                // 참고: 원래 코드의 "//_yVelocity = -1f;" 부분이 이 로직으로 대체됩니다.
            }
            else // 공중에 있을 때
            {
                _yVelocity += GRAVITY * Time.deltaTime;
            }
        }
    }

    private void CheckOnWall()
    {
        // 사용자의 기존 CheckOnWall 로직 유지
        if (_characterController.collisionFlags == CollisionFlags.Sides)
        {
            _isWall = true;
            _staminaNumber = _wallStamina; // 이 값은 Stamina() 함수에서 사용됩니다.
        }
        else
        {
            if (_isWall)
            {
                _isWall = false;
                // 벽에서 떨어졌을 때 _staminaNumber를 _normalStamina로 돌리는 로직은
                // 현재 Stamina() 함수 내의 조건문에서 처리됩니다.
            }
        }
    }

    private void Rolling()
    {
        // 사용자의 기존 Rolling 로직 유지
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_stamina >= _needStamina2Roll)
            {
                _rollingPower = _rollPower;
                _stamina -= _needStamina2Roll;
                // _animator.SetTrigger("Roll"); // 애니메이터 트리거는 원래 코드에 없었으므로 생략 (필요시 추가)
            }
        }

        if (_rollingPower > 0f)
        {
            // 사용자의 기존 _rollingPower 감쇠 로직 유지
            _rollingPower -= Time.deltaTime * _rollPower; // 여기서 _rollPower는 초기 힘(_rollPowerValue)을 의미
            if (_rollingPower < 0) _rollingPower = 0; // 음수가 되지 않도록
        }
    }


    private void Jump()
    {
        // 사용자의 기존 Jump 로직 유지
        _isGrounded = _characterController.isGrounded; // 이 변수는 현재 코드 내 다른 곳에서 사용되지 않음
                                                      // _characterController.isGrounded를 직접 사용하는 것이 좋음
        //점프 적용
        if (Input.GetButtonDown("Jump") && _jumpingCount < 1)
        {
            _animator.SetTrigger("Jump");
            _yVelocity = JumpPower;
            _jumpingCount++;
        }
        else
        {
            // 경고: 이 부분은 Input.GetButtonDown("Jump")가 false일 때 매 프레임 "Ground"를 트리거할 수 있습니다.
            // 이는 애니메이션을 계속해서 처음부터 재생시키거나 의도치 않은 동작을 유발할 수 있습니다.
            // 미끄러짐과 직접 관련은 없으므로 일단 유지하지만, 추후 수정이 필요할 수 있습니다.
            // 해결책 예시: if (!Input.GetButton("Jump") && _characterController.isGrounded && _yVelocity <= 0) { _animator.SetTrigger("Ground"); }
            _animator.SetTrigger("Ground");
        }
    }

    private void Run()
    {
        // 사용자의 기존 Run 로직 유지
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            if (!_isWall)
            {
                _staminaNumber = _runningStamina;
                _moveSpeed = DashSpeed;
                _isRunning = true;
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            if (!_isWall)
            {
                _moveSpeed = BasicSpeed;
                _isRunning = false;
            }
        }
    }

    private void Stamina()
    {
        // 사용자의 기존 Stamina 로직 유지
        _stamina += _staminaNumber * Time.deltaTime;//계속해서 스태미나를 채운다
        _stamina = Mathf.Clamp(_stamina, 0, MaxStamina);


        if (_isGrounded && !_isWall && !_isRunning) // _isGrounded 대신 _characterController.isGrounded 사용 권장
        {
            _staminaNumber = _normalStamina;//일반적인 상태일 때는 스태미나가 일반적으로 오른다.
        }
    }

    private void BasicCharacterMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, 0, v);
        _animator.SetFloat("MoveAmount", dir.magnitude);
        dir.Normalize();

        // 사용자의 기존 _rollingPower 적용 위치 유지
        dir.z += _rollingPower;

        dir = Camera.main.transform.TransformDirection(dir);

        // ★★★ 땅에 닿았을 때 점프 카운트 리셋 (중요) ★★★
        // SetGravity에서 yVelocity가 음수로 설정된 후, 실제로 Move를 통해 땅에 닿았는지 확인하고,
        // y축 속도가 아래를 향할 때 (즉, 안정적으로 착지했을 때) 점프 카운트를 리셋합니다.
        if (_characterController.isGrounded && _yVelocity <= 0.0f) // _yVelocity <= 0 조건 추가
        {
            _jumpingCount = 0;
        }
        
        // 중력(y축 속도) 적용
        dir.y = _yVelocity;

        _characterController.Move(dir * Time.deltaTime * _moveSpeed);
    }
}