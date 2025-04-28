using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    private bool _isGrounded = true;
    [SerializeField]
    private bool _isRunning = false;



    private float _normalStamina = 15f;
    private float _wallStamina = -10f;
    private float _runningStamina = -10f;

    private float _rollPower = 5f;
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _moveSpeed = BasicSpeed;
        _animator = GetComponentInChildren<Animator>();

        
    }

    private void Start()
    {
        _stamina = MaxStamina;
        
        
        BasicSpeed = playerCore.BasicSpeed;
        DashSpeed = playerCore.DashSpeed;
        MaxStamina = playerCore.MaxStamina;
        _needStamina2Roll = playerCore.NeedStamina2Roll;
        _normalStamina = playerCore.NormalStamina;
        _wallStamina = playerCore.WallStamina;
        _runningStamina = playerCore.RunningStamina;
        _rollPower = playerCore.RollPower;
        JumpPower = playerCore.JumpPower;
        _yVelocity = GRAVITY;

    }

    private void Update()
    {
        
        
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play )
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

    }

    private void SetGravity()
    {
        
        
        if (_isWall && _stamina > 0)
        {
            _yVelocity = 1;
        }
        else
        {
            if (!_characterController.isGrounded)
            {
                _yVelocity += GRAVITY * Time.deltaTime;               
            }
            else
            {
                //_yVelocity = -1f;
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
            }
        }

        if (_rollingPower > 0f)
        {
            _rollingPower -= Time.deltaTime * _rollPower;
        }
    }
    

    private void Jump()
        {
            _isGrounded = _characterController.isGrounded;
            //점프 적용
            if (Input.GetButtonDown("Jump") && _jumpingCount < 1 )
            {
                _animator.SetTrigger("Jump");
                _yVelocity = JumpPower;
                _jumpingCount++;
            }else
            {
                _animator.SetTrigger("Ground");
            }
        }
        
private void Run(){
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
        {_moveSpeed = BasicSpeed;
            _isRunning = false;
        }
    }
}
      private  void Stamina()
        {    
            _stamina += _staminaNumber * Time.deltaTime;//계속해서 스태미나를 채운다
            _stamina = Mathf.Clamp(_stamina, 0, MaxStamina);
            
            
            if (_isGrounded && !_isWall && !_isRunning)
            {
                _staminaNumber = _normalStamina;//일반적인 상태일 때는 스태미나가 일반적으로 오른다.
            }
            
            //
            // if (_stamina > MaxStamina)
            // {
            //     _stamina = MaxStamina;
            // }
            //
            // if(_stamina<0)
            // {
            //     _moveSpeed = BasicSpeed;
            //     _stamina = 0;
            // }
            
            
        }
        
      private  void BasicCharacterMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 dir = new Vector3(h, 0, v);
            _animator.SetFloat("MoveAmount", dir.magnitude);
            dir.Normalize();
            
            dir.z += _rollingPower;

            //메인카메라 기준으로 방향을 전환한다.
            //TransformDirection : 로컬 공간의 벡터를 월드 공간의 벡터로 바꿔줌
            dir = Camera.main.transform.TransformDirection(dir);


            if (_characterController.isGrounded) _jumpingCount = 0;

            
            //중력 적용
            dir.y = _yVelocity;
            


            _characterController.Move(dir * Time.deltaTime * _moveSpeed);
            Debug.Log("Dir : " + dir);

        }
    }

