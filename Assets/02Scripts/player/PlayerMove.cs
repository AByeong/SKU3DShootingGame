using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    [FormerlySerializedAs("PlayerData")] public PlayerCore playerCore;

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
    private float _rollStamina = -10f;

    private float _rollPower = 5f;
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _moveSpeed = BasicSpeed;
        

        
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
        _rollStamina = playerCore.RollStamina;
        _rollPower = playerCore.RollPower;
        JumpPower = playerCore.JumpPower;

    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play )
        {
            BasicCharacterMovement();
            Stamina();
            Jump();
            Rolling();
            CheckOnWall();
            SetGravity();
        }

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
                _yVelocity = JumpPower;
                _jumpingCount++;
            }
        }
        

      private  void Stamina()
        {    
            

            
            if (_isGrounded && !_isWall && !_isRunning)
            {
                _staminaNumber = _normalStamina;
            }
            
            if (_stamina > MaxStamina)
            {
                _stamina = MaxStamina;
            }
            else if (_stamina >= 0)
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
                    {_moveSpeed = BasicSpeed;
                        _isRunning = false;
                    }
                }
                _stamina += _staminaNumber * Time.deltaTime;

            }
            else
            {
                _moveSpeed = BasicSpeed;
                _stamina = 0;
            }
            
            
        }
        
      private  void BasicCharacterMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 dir = new Vector3(h, 0, v);
            dir.Normalize();
            
            dir.z += _rollingPower;

            //메인카메라 기준으로 방향을 전환한다.
            //TransformDirection : 로컬 공간의 벡터를 월드 공간의 벡터로 바꿔줌
            dir = Camera.main.transform.TransformDirection(dir);


            if (_characterController.isGrounded) _jumpingCount = 0;
            //if(_characterController.collisionFlags == CollisionFlags.Below) <- 이것과 같은 말임




            //중력 적용
            
            dir.y = _yVelocity;
            


            _characterController.Move(dir * Time.deltaTime * _moveSpeed);

        }
    }

