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
    public int MaxJumpCount = 2; // ★★★ 2단 점프를 위한 최대 점프 횟수 추가 ★★★

    [SerializeField]
    private float _stamina;
    public float PlayerStamina => _stamina;

    private float _staminaNumber = 3f;
    public float MaxStamina = 100f;
    private float _needStamina2Roll = 10f;

    [SerializeField]
    private float _rollingPower = 1f;
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
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (playerCore != null)
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
            // MaxJumpCount도 PlayerCore에서 가져오도록 설정할 수 있습니다.
            // 예: if (playerCore.MaxJumpCount > 0) MaxJumpCount = playerCore.MaxJumpCount;
        }
        else
        {
            Debug.LogError("PlayerCore가 PlayerMove 스크립트에 할당되지 않았습니다. 기본값을 사용합니다.");
        }
        _moveSpeed = BasicSpeed;
        _stamina = MaxStamina;
        _yVelocity = GRAVITY;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play)
        {
            BasicCharacterMovement();
            Run();
            Jump(); 
            Rolling();
            SetGravity();
            Stamina();
            CheckOnWall();
        }
    }

    public void ShakeCamera()
    {
        // 내용 없음
    }

    private void SetGravity()
    {
        if (_isWall && _stamina > 0)
        {
            _yVelocity = 1;
        }
        else
        {
            if (_characterController.isGrounded)
            {
                if (_yVelocity < 0.0f)
                {
                    _yVelocity = -2.0f;
                }
            }
            else
            {
                _yVelocity += GRAVITY * Time.deltaTime;
            }
        }
    }

    private void CheckOnWall()
    {
        if (_characterController.collisionFlags == CollisionFlags.Sides)
        {
            _isWall = true;
            _staminaNumber = _wallStamina;
        }
        else
        {
            if (_isWall)
            {
                _isWall = false;
            }
        }
    }

    private void Rolling()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_stamina >= _needStamina2Roll)
            {
                _rollingPower = _rollPower;
                _stamina -= _needStamina2Roll;
                // _animator.SetTrigger("Roll"); // 원래 코드에 없었으므로 생략
            }
        }

        if (_rollingPower > 1f)
        {
            _rollingPower -= Time.deltaTime * _rollPower;
            if (_rollingPower < 1) _rollingPower = 1;
        }
    }


    private void Jump()
    {
        // _isGrounded = _characterController.isGrounded; // 이 줄은 _characterController.isGrounded를 직접 사용하는 것이 좋습니다.

        if (Input.GetButtonDown("Jump"))
        {
            if (_characterController.isGrounded) // 1. 땅에 있을 때 (1단 점프)
            {
                _animator.SetTrigger("Jump");
                _yVelocity = JumpPower;
                _jumpingCount = 1; // 점프 횟수를 1로 설정 (첫 번째 점프)
            }
            else if (_jumpingCount < MaxJumpCount) // 2. 공중에 있고, 아직 최대 점프 횟수에 도달하지 않았을 때 (2단 점프 이상)
            {
                _animator.SetTrigger("Jump"); // 2단 점프에도 동일한 애니메이션 또는 다른 애니메이션 사용 가능
                _yVelocity = JumpPower * 0.9f; // 2단 점프는 약간 낮게 하거나 동일하게 (선택 사항)
                _jumpingCount++; // 점프 횟수 증가
            }
        }
        else
        {
            // 기존 else 로직 유지 (애니메이션 관련하여 추후 검토 필요)
            // if (!Input.GetButton("Jump") && _characterController.isGrounded) // 이렇게 조건을 더 명확히 할 수 있음
            // {
            //    _animator.SetTrigger("Ground");
            // }
            _animator.SetTrigger("Ground"); // 사용자의 기존 코드 유지
        }
    }

    private void Run()
    {
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
        _stamina += _staminaNumber * Time.deltaTime;
        _stamina = Mathf.Clamp(_stamina, 0, MaxStamina);

        if (_characterController.isGrounded && !_isWall && !_isRunning) // _isGrounded 대신 _characterController.isGrounded 사용 권장
        {
            _staminaNumber = _normalStamina;
        }
    }

    private void BasicCharacterMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, 0, v);
        _animator.SetFloat("MoveAmount", dir.magnitude);
        dir.Normalize();

        //dir.z += _rollingPower;
        dir *= _rollingPower;
        dir = Camera.main.transform.TransformDirection(dir);

        if (_characterController.isGrounded && _yVelocity <= 0.0f)
        {
            _jumpingCount = 0; // 땅에 착지 시 점프 카운트 초기화
        }

        dir.y = _yVelocity;

        _characterController.Move(dir * Time.deltaTime * _moveSpeed);
    }
}