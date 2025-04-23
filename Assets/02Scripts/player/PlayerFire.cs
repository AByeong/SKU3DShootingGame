using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFire : MonoBehaviour
{
    public CameraFollow CameraShake;
    
    public Transform FirePosition;
    public BombPool BombPool;

    public float ThrowPower = 15f;

    public ParticleSystem BulletEffect;

    public UI_Boomb BombUI;
    public UI_Bullet BulletUI;

    [SerializeField] private int _currentBombCount;
    [SerializeField] private int _maxBombCount = 3;

    [SerializeField] private float _minThrowPower = 3f;
    [SerializeField] private float _maxThrowPower = 30f;
    [SerializeField] private float _thorwrate = 1f;


    [SerializeField] private float _shotCoolTime = 2f;
    [SerializeField] private bool _shotPossible = true;

    [SerializeField] private int _maxBulletCount = 50;
    [SerializeField] private int _currentBulletCount;
    [SerializeField] private float _rerollCoolTime = 2f;
    
    private Coroutine _rerollCoroutine;
    public TrailRenderer BulletTrail;
    private void Start()
    {
        BombUI.SetMaxBombCount(_maxBombCount);
        BulletUI.SetMaxBulletCount(_maxBulletCount);
        
        _currentBombCount = _maxBombCount;
        _currentBulletCount = _maxBulletCount;
        
        BombUI.ChangeBombCount(_currentBombCount);
        BulletUI.ChangeBulletCount(_currentBulletCount);
        
        ThrowPower = _minThrowPower;
        
        Cursor.lockState = CursorLockMode.Locked;
        
    }

    private void Update()
    {
        
        
        FireShot(); 

        FireBomb();

        if (Input.GetKeyDown(KeyCode.R))
        {
            Reroll();
        }


    }

    private void FireShot()
    {
        if (_currentBulletCount == 0)
        {
            Reroll();
        }
        
        //1. 마우스 왼쪽 버튼 입력
        if (Input.GetMouseButton(0) && _currentBulletCount != 0)
        {
            if (_rerollCoroutine != null)
            {
                StopCoroutine(_rerollCoroutine);
            }
            
            BulletUI.StopReroll();
            if (_shotPossible)
            {
                
                StartCoroutine(CoolTimeShot());
            }


        }
    }
    
    IEnumerator  CoolTimeShot()
    {
        

       _shotPossible = false;
       _currentBulletCount--;
       BulletUI.ChangeBulletCount(_currentBulletCount);
            //2. 레이저를 생성하고 발사 위치와 진행 바향을 설정
            Ray ray = new Ray(FirePosition.position, Camera.main.transform.forward);
            //3. 레이저에 부딪힌 물체의 정보를 저장할 변수를 생성
            RaycastHit hitInfo = new RaycastHit();
            bool isHit = Physics.Raycast(ray, out hitInfo);
            //4. 레이저를 발사하고 부딪힌 정보가 있다면 피격 이벤트 생성하기
            if (isHit)
            {

                if (hitInfo.collider.CompareTag("Enemy"))
                {
                    Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
                    
                    Damage damage = new Damage();
                    damage.Value = 10;
                    damage.From = this.gameObject;
                    damage.KnockBackPower = 1f;
                    damage.HitDirection = hitInfo.normal;
                    enemy.TakeDamage(damage);
                    
                }
                
                
                TrailRenderer trail = Instantiate(BulletTrail, FirePosition.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, hitInfo));


            }
        

        yield return new WaitForSeconds(_shotCoolTime);
        _shotPossible = true;
        
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hitInfo)
    {
        float time = 0;
        Vector3 startPos = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPos, hitInfo.point, time);
            time += Time.deltaTime/trail.time;
            
            yield return null;
        }
        trail.transform.position = hitInfo.point;

        BulletEffect.transform.position = hitInfo.point;
        BulletEffect.transform.forward = hitInfo.normal;
        BulletEffect.Play();
        
        Destroy(trail.gameObject);
    }

    private void Reroll()
    {
        BulletUI.StartReroll(_rerollCoolTime);
        
        _rerollCoroutine =  StartCoroutine(RerollCoroutine());

    }

    IEnumerator RerollCoroutine()
    {
        yield return new WaitForSeconds(_rerollCoolTime);
        _currentBulletCount = _maxBulletCount;
        BulletUI.ChangeBulletCount(_currentBulletCount);
    }

    private void FireBomb()
    {
        //꾹 눌렀을 때 멀리 나가도록 해야한다.
        if (Input.GetMouseButton(1))
        {
            if (_currentBombCount > 0)
            {
                if (ThrowPower <= _maxThrowPower)
                {
                    ThrowPower += Time.deltaTime * _thorwrate;
                }
            }
            
            
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            if (_currentBombCount > 0)
            {
                BombPool.FireBomb(FirePosition, ThrowPower);
                _currentBombCount--;
                BombUI.ChangeBombCount(_currentBombCount);
                ThrowPower = _minThrowPower;
            }
            
            
        }
    }
}
