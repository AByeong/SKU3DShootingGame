using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization; // FormerlySerializedAs 등 사용 시
// using Unity.VisualScripting; // Visual Scripting 사용 시
// using UnityEngine.UI; // UI 요소 코드 제어 시

public class PlayerMove : MonoBehaviour
{
    public PlayerCore playerCore;
    private Animator _animator;
    [SerializeField]
    private float _moveSpeed = 5f;

    public float BasicSpeed = 5f;
    public float DashSpeed = 10f;

    private CharacterController _characterController;

    private const float GRAVITY = -9.8f * 6f;
    private float _yVelocity = 0f;

    public float JumpPower = 10f;
    private int _jumpingCount = 0;
    public int MaxJumpCount = 2;

    [SerializeField]
    private float _stamina;
    public float PlayerStamina => _stamina;

    private float _staminaNumber = 3f;
    public float MaxStamina = 100f;
    private float _needStamina2Roll = 10f;

    [SerializeField]
    private float _rollingPower = 1f; // 현재 구르기 힘/속도 (감소하는 값)
    private float _baseRollPowerFromCore = 5f; // PlayerCore에서 가져온 구르기 기본 힘/속도

    [SerializeField]
    private bool _isWall = false;
    // [SerializeField] private bool _isGrounded = true; // CharacterController.isGrounded를 직접 사용
    [SerializeField]
    private bool _isRunning = false;

    private float _normalStamina = 15f;
    private float _wallStamina = -10f;
    private float _runningStamina = -10f;

    // CameraManager 참조
    public CameraManager cameraManager;
    public float RotationSpeed = 10f; // 캐릭터 회전 속도

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        // CameraManager 찾기 (Inspector에서 할당되지 않은 경우)
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }
        if (cameraManager == null)
        {
            Debug.LogError("PlayerMove: CameraManager를 찾을 수 없습니다. 카메라 시점별 이동이 올바르게 동작하지 않을 수 있습니다.");
        }
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
            _baseRollPowerFromCore = playerCore.RollPower; // PlayerCore에서 RollPower를 가져와 _baseRollPowerFromCore에 저장
            JumpPower = playerCore.JumpPower;
            // RotationSpeed = playerCore.RotationSpeed; // PlayerCore에 RotationSpeed가 있다면 가져옵니다.
        }
        else
        {
            Debug.LogError("PlayerCore가 PlayerMove 스크립트에 할당되지 않았습니다. 기본값을 사용합니다.");
            _baseRollPowerFromCore = 5f; // PlayerCore 없을 시 기본값
        }
        _moveSpeed = BasicSpeed;
        _stamina = MaxStamina;
        // _yVelocity = GRAVITY; // 시작 시 바로 중력 적용보다는 isGrounded 상태에 따라 결정되는 것이 좋음
        _yVelocity = -2.0f; // CharacterController의 일반적인 초기값
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play)
        {
            HandleMovement(); // BasicCharacterMovement 대신 새로운 통합 함수 호출
            Run();
            Jump();
            Rolling();
            SetGravity();
            Stamina();
            CheckOnWall();
        }
    }

    

    private void SetGravity()
    {
        if (_isWall && _stamina > 0)
        {
            
           // _yVelocity += GRAVITY * Time.deltaTime * 0.1f; // 중력 영향 감소
            //if (_yVelocity > 2.0f) _yVelocity = 2.0f; // 너무 빠르게 떨어지지 않도록
            _yVelocity = _moveSpeed;
        }
        else
        {
            if (_characterController.isGrounded)
            {
                if (_yVelocity < 0.0f)
                {
                    _yVelocity = -2.0f; // 땅에 닿아있을 때 안정적인 Y 속도 (CharacterController 권장사항)
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
        
        if (!_characterController.isGrounded && (_characterController.collisionFlags & CollisionFlags.Sides) != 0)
        {
            _isWall = true;
            _staminaNumber = _wallStamina; 
        }
        else
        {
            if (_isWall) // 벽에서 떨어진 경우
            {
                _isWall = false;

            }
        }
    }

    private void Rolling()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_stamina >= _needStamina2Roll && !_isWall) // 벽에서는 구르기 방지
            {
                _rollingPower = _baseRollPowerFromCore; // 구르기 시작 시 PlayerCore에서 설정된 값으로 초기화
                _stamina -= _needStamina2Roll;
                _animator.SetTrigger("Roll"); // 구르기 애니메이션 트리거
            }
        }

        // _rollingPower는 구르기 지속 시간 동안 점차 감소하거나, 구르기 상태를 나타내는 플래그로 사용될 수 있습니다.
        // 여기서는 구르기 시 일시적으로 이동 속도를 _baseRollPowerFromCore로 설정하고,
        // _rollingPower를 구르기 상태 및 감쇠 효과로 사용합니다 (기존 로직 유지).
        if (_rollingPower > 1f) 
        {
            
            _rollingPower -= Time.deltaTime * _baseRollPowerFromCore; // 감소율은 _baseRollPowerFromCore에 비례하게
            if (_rollingPower < 1f) _rollingPower = 1f;
        }
        else
        {
            _rollingPower = 1f; // 구르기가 아닐 때는 1로 유지
        }
    }

    private void Jump()
    {
        if (Input.GetButtonDown("Jump") && !_isWall) // 벽에서는 점프 방식이 달라질 수 있음 (월점프 등)
        {
            if (_characterController.isGrounded)
            {
                _animator.SetTrigger("Jump");
                _yVelocity = JumpPower;
                _jumpingCount = 1;
                _isWall = false; // 점프 시 벽 상태 해제
            }
            else if (_jumpingCount < MaxJumpCount)
            {
                _animator.SetTrigger("Jump");
                _yVelocity = JumpPower * 0.9f; // 2단 점프는 약간 낮게
                _jumpingCount++;
                _isWall = false; // 점프 시 벽 상태 해제
            }
        }
        // 기존 `_animator.SetTrigger("Ground");`는 매 프레임 호출될 수 있어 부적절합니다.
        // 착지 애니메이션은 isGrounded 상태 변화를 감지하여 처리하는 것이 좋습니다.
        // 예: if (!_wasGroundedLastFrame && _characterController.isGrounded) _animator.SetTrigger("Landed");
    }

    private void Run()
    {
        // 쉬프트 키 입력 감지
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isShiftPressed && !_isWall && _stamina > 0) // 달리기 조건: 쉬프트 누름, 벽 아님, 스테미나 있음
        {
            if (!_isRunning) // 달리기를 시작하는 순간
            {
                 _moveSpeed = DashSpeed;
                _isRunning = true;
                _staminaNumber = _runningStamina; // 달리기 중 스테미나 소모율
            }
        }
        else // 쉬프트를 뗐거나, 벽에 있거나, 스테미나가 없을 때
        {
            if (_isRunning) // 달리기를 멈추는 순간
            {
                _moveSpeed = BasicSpeed;
                _isRunning = false;
                // 스테미나 회복률은 Stamina()에서 기본 상태일 때 _normalStamina로 설정됨
            }
        }
        if (_isRunning && _stamina <=0) // 달리는 중 스테미나 오링
        {
            _moveSpeed = BasicSpeed;
            _isRunning = false;
        }
    }

    private void Stamina()
    {
        // 현재 상태에 따른 스테미나 변경률(_staminaNumber) 설정
        if (_isWall && _stamina > 0) // 벽에 붙어있을 때
        {
            _staminaNumber = _wallStamina;
        }
        else if (_isRunning && _stamina > 0) // 달리는 중일 때
        {
            _staminaNumber = _runningStamina;
        }
        else if (_characterController.isGrounded) // 땅에 있고, 벽도 아니고, 달리는 중도 아닐 때 (기본 회복 상태)
        {
             _staminaNumber = _normalStamina;
        }
        else // 공중에 있지만 벽이 아닐 때 (스테미나 변화 없음 또는 약간 소모 - 선택)
        {
            _staminaNumber = 0; // 공중에서는 스테미나 변화 없음 (또는 약간 소모하도록 설정 가능)
        }


        _stamina += _staminaNumber * Time.deltaTime;
        _stamina = Mathf.Clamp(_stamina, 0, MaxStamina);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(h, 0, v);
        _animator.SetFloat("MoveAmount", Mathf.Clamp01(inputDirection.magnitude));

        Vector3 worldMoveDirection = Vector3.zero; // 최종 이동 방향 (월드 좌표계)

        if (cameraManager != null)
        {
            switch (cameraManager.CurrentCameraView)
            {
                case CameraManager.CameraViewState.FPS:
                    worldMoveDirection = transform.TransformDirection(inputDirection.normalized);
                    break;

                case CameraManager.CameraViewState.TPS:
                    if (inputDirection.magnitude >= 0.1f)
                    {
                        Quaternion cameraYRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                        worldMoveDirection = cameraYRotation * inputDirection.normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                    }
                    break;

                case CameraManager.CameraViewState.QuerterView: // CameraManager의 오타와 일치시킴
                    if (inputDirection.magnitude >= 0.1f)
                    {
                        Quaternion cameraYRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                        worldMoveDirection = cameraYRotation * inputDirection.normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                    }
                    break;

                default:
                    if (inputDirection.magnitude >= 0.1f)
                    {
                        Quaternion cameraYRotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                        worldMoveDirection = cameraYRotation * inputDirection.normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                    }
                    break;
            }
        }
        else 
        {
            if (inputDirection.magnitude >= 0.1f)
            {
                worldMoveDirection = Camera.main != null ? Camera.main.transform.TransformDirection(inputDirection.normalized) : inputDirection.normalized;
                worldMoveDirection.y = 0;
                if(worldMoveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                }
            }
        }

        if (_characterController.isGrounded)
        {
            _jumpingCount = 0;
        }


        // 1. 기본 이동 속도(_moveSpeed)를 기반으로 수평 속도 계산
        Vector3 finalHorizontalVelocity = worldMoveDirection * _moveSpeed;

        // 2. 구르기 상태(_rollingPower > 1f)일 경우, 이 수평 속도에 _rollingPower 배율 적용
        if (_rollingPower > 1f && worldMoveDirection.sqrMagnitude > 0.01f)
        {
            // _rollingPower는 _baseRollPowerFromCore에서 시작하여 1로 감소하는 배율.
            // 예: _moveSpeed가 10이고, _rollingPower가 (초기값) 3이라면, 수평 속도는 30으로 시작.
            finalHorizontalVelocity.x *= _rollingPower;
            finalHorizontalVelocity.z *= _rollingPower;
        }


        Vector3 finalVelocity = finalHorizontalVelocity; // x, z 성분은 위에서 결정됨
        finalVelocity.y = _yVelocity; // y축 속도(중력, 점프) 적용

        _characterController.Move(finalVelocity * Time.deltaTime);
    }
}