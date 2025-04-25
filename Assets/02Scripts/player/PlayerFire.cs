using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFire : MonoBehaviour
{
    [FormerlySerializedAs("PlayerData")] public PlayerCore playerCore;
    public CursorInUIZone cursorInUI;
    public CameraManager CameraManager;

    public Transform FirePosition; // 총알/폭탄이 실제로 생성되는 위치
    public BombPool BombPool;

    public float ThrowPower = 15f;
    public ParticleSystem BulletEffect; // 피격 효과

    // --- UI 참조 ---
    public UI_Boomb BombUI;
    public UI_Bullet BulletUI;

    // --- 폭탄 설정 ---
    [Header("폭탄 설정")] // Inspector에 표시될 헤더
    [SerializeField] private int _maxBombCount = 3; // 최대 폭탄 개수
    [SerializeField] private float _minThrowPower = 3f; // 최소 던지기 파워
    [SerializeField] private float _maxThrowPower = 30f; // 최대 던지기 파워
    [SerializeField] private float _chargeRate = 10f; // 충전 속도 (명확성을 위해 _thorwrate에서 이름 변경)

    // --- 총알 설정 ---
    [Header("총알 설정")] // Inspector에 표시될 헤더
    [SerializeField] private int _maxBulletCount = 50; // 최대 총알 개수
    [SerializeField] private float _shotCoolTime = 0.2f; // 발사 간격 쿨타임
    [SerializeField] private float _rerollCoolTime = 2f; // 재장전 시간
    [SerializeField] private float _maxShotDistance = 100f; // 레이캐스트 최대 사거리
    public TrailRenderer BulletTrailPrefab; // 총알 궤적 프리팹

    // --- 런타임 상태 변수 ---
    private int _currentBombCount; // 현재 폭탄 개수
    private int _currentBulletCount; // 현재 총알 개수
    private bool _shotPossible = true; // 발사 가능 여부
    private Coroutine _rerollCoroutine; // 재장전 코루틴 참조
    private float _currentChargePower; // 현재 폭탄 충전 파워 추적

    
    

    private void Start()
    {
        // UI 초기화
        BombUI.SetMaxBombCount(_maxBombCount);
        BulletUI.SetMaxBulletCount(_maxBulletCount);

        // 탄약/폭탄 초기화
        _currentBombCount = _maxBombCount;
        _currentBulletCount = _maxBulletCount;
        BombUI.ChangeBombCount(_currentBombCount);
        BulletUI.ChangeBulletCount(_currentBulletCount);

        _currentChargePower = _minThrowPower; // 충전 파워 초기화

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false; // 잠금 상태일 때 커서 숨김
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play )
               {
            HandleShootingInput(); // 발사 입력 처리
            HandleBombInput(); // 폭탄 입력 처리
            HandleReloadInput(); // 재장전 입력 처리
        }
    }

    // --- 입력 처리 ---

    private void HandleShootingInput()
    {
        // 발사 *가능* 상태인지 확인 (재장전 중 아님, 탄약 있음, 쿨타임 완료)
        if (Input.GetMouseButton(0) && _shotPossible && _currentBulletCount > 0 && _rerollCoroutine == null)
        {
            // 발사 시 재장전 중지

            if(!cursorInUI.InUIZone)
            {
                FireShot(); // 발사 로직 실행
            }
        }
        // 탄약이 없고 재장전 중이 아닐 경우 자동으로 재장전 시작
        else if (_currentBulletCount == 0 && _rerollCoroutine == null)
        {
            Reroll();
        }
    }

    private void HandleBombInput()
    {
        // 폭탄 던지기 충전
        if (Input.GetMouseButton(1))
        {
            if (_currentBombCount > 0 && _currentChargePower < _maxThrowPower)
            {
                _currentChargePower += Time.deltaTime * _chargeRate;
                _currentChargePower = Mathf.Min(_currentChargePower, _maxThrowPower); // 최대값으로 제한
                // 선택 사항: 충전 레벨을 표시하도록 UI 업데이트
            }
        }

        // 폭탄 던지기 발사 (마우스 버튼 놓음)
        if (Input.GetMouseButtonUp(1))
        {
            if (_currentBombCount > 0)
            {
                FireBomb(_currentChargePower); // 충전된 파워 전달
                _currentChargePower = _minThrowPower; // 충전 상태 초기화
                // 선택 사항: 충전 UI 초기화
            }
        }
    }

     private void HandleReloadInput()
     {
         // R키를 누르고, 재장전 중이 아니며, 탄약이 최대치가 아닐 때 재장전
        if (Input.GetKeyDown(KeyCode.R) && _rerollCoroutine == null && _currentBulletCount < _maxBulletCount)
        {
            Reroll();
        }
     }

    // --- 발사 로직 ---

    private void FireShot()
    {
        StartCoroutine(ShotCooldownCoroutine()); // 쿨타임 타이밍 처리

        _currentBulletCount--;
        BulletUI.ChangeBulletCount(_currentBulletCount);

        Vector3 targetPoint; // 조준 목표 지점
        Vector3 fireDirection; // 실제 발사 방향
        bool aimingHit = GetAimTargetPoint(out targetPoint); // 플레이어가 조준하는 위치 가져오기

        // 발사 위치에서 목표 지점까지의 실제 방향 계산
        fireDirection = (targetPoint - FirePosition.position).normalized;

        // FirePosition에서 데미지 판정을 위한 실제 레이캐스트 수행
        RaycastHit damageHitInfo;
        if (Physics.Raycast(FirePosition.position, fireDirection, out damageHitInfo, _maxShotDistance))
        {
            // 실제 피격 지점을 이펙트 및 궤적 종료 지점으로 사용
            targetPoint = damageHitInfo.point; // 목표 지점을 실제 피격 지점으로 업데이트
            Damage damage = new Damage
            {
                Value = 10,
                From = this.gameObject,
                KnockBackPower = 1f,
                HitDirection = -fireDirection // 적 *방향으로*의 벡터 (피격 방향)
            };

            if (damageHitInfo.collider.TryGetComponent<IDamagable>(out IDamagable damagable))
            {
                damagable.TakeDamage(damage);
            }
           

             // --- 피격 이펙트 재생 ---
            PlayImpactEffect(damageHitInfo.point, damageHitInfo.normal);
        }
        


        // --- 총알 궤적 생성 ---
        SpawnTrail(FirePosition.position, targetPoint);
    }

    private void FireBomb(float power)
    {
        _currentBombCount--;
        BombUI.ChangeBombCount(_currentBombCount);

        Vector3 targetPoint; // 조준 목표 지점
        Vector3 fireDirection; // 실제 발사 방향
        bool aimingHit = GetAimTargetPoint(out targetPoint); // 발사와 동일한 방식으로 조준점 가져오기

        // 발사 위치에서 목표 지점까지의 방향 계산
        fireDirection = (targetPoint - FirePosition.position).normalized;

        // 더 나은 포물선을 위해 방향을 약간 위로 조정 (선택 사항)
        // fireDirection = Quaternion.AngleAxis(-10, transform.right) * fireDirection; // 예시: 위쪽으로 10도

        BombPool.FireBomb(FirePosition, fireDirection, power);
    }

    // --- 조준 계산 ---

    /// <summary>
    /// 카메라 뷰 상태에 따라 플레이어가 조준하는 월드 좌표 지점을 결정합니다.
    /// </summary>
    /// <param name="targetPoint">계산된 월드 좌표 목표 지점입니다.</param>
    /// <returns>조준 레이캐스트가 무언가에 맞았다면 true, 그렇지 않다면 false를 반환합니다.</returns>
    private bool GetAimTargetPoint(out Vector3 targetPoint)
    {
        Ray aimRay; // 조준 광선
        bool hitDetected = false; // 충돌 감지 여부
        RaycastHit hitInfo; // 충돌 정보

        switch (CameraManager.CameraView)
        {
            case CameraManager.CameraViewState.FPS:

                // FPS와 TPS의 경우, 조준 광선은 화면 중앙(카메라)에서 시작
                aimRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                if (Physics.Raycast(aimRay, out hitInfo, _maxShotDistance))
                {
                    targetPoint = hitInfo.point;
                    hitDetected = true;
                }
                else
                {
                    // 맞춘 대상이 없으면, 카메라 전방 방향으로 멀리 떨어진 지점을 조준
                    targetPoint = aimRay.origin + aimRay.direction * _maxShotDistance;
                    hitDetected = false;
                }
                break;
            case CameraManager.CameraViewState.TPS:
            case CameraManager.CameraViewState.QuerterView: // 원본 스크립트의 오타 유지
                // 쿼터뷰의 경우, 조준 광선은 카메라에서 마우스 커서 방향으로 시작
                aimRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                // FirePosition의 높이에 있는 평면에 투영
                Plane aimPlane = new Plane(Vector3.up, FirePosition.position);
                float distance;
                if (aimPlane.Raycast(aimRay, out distance))
                {
                    targetPoint = aimRay.GetPoint(distance);
                    // 선택 사항: FirePosition에서 targetPoint로 2차 레이캐스트 추가하여
                    // *실제* 발사 경로가 막혔는지 확인 (하지만 평면 지점을 조준하는 것이 더 간단함)
                    hitDetected = true; // 기술적으로는 평면에 맞음
                }
                else
                {
                    // 평면 레이캐스트 실패 시 대체 처리 (예: 평면과 평행하게 바라볼 때)
                    // 백업으로 FirePosition에서 정면으로 조준
                    targetPoint = FirePosition.position + transform.forward * _maxShotDistance; // 플레이어의 전방 방향 사용
                     hitDetected = false;
                }
                break;

            default:
                Debug.LogError("GetAimTargetPoint에서 처리되지 않은 CameraViewState!");
                targetPoint = FirePosition.position + transform.forward * _maxShotDistance; // 기본 대체 처리
                hitDetected = false;
                break;
        }
        return hitDetected;
    }


    // --- 코루틴 및 효과 ---

    IEnumerator ShotCooldownCoroutine()
    {
        _shotPossible = false; // 발사 불가능 상태로 변경
        yield return new WaitForSeconds(_shotCoolTime); // 설정된 쿨타임만큼 대기
        _shotPossible = true; // 발사 가능 상태로 복귀
    }

     private void Reroll()
    {
        // 이미 재장전 중이라면, 또 시작하지 않음
        if (_rerollCoroutine != null) return;

        BulletUI.StartReroll(_rerollCoolTime); // UI에 재장전 진행 상황 표시 요청
        _rerollCoroutine = StartCoroutine(RerollCoroutine()); // 재장전 코루틴 시작 및 참조 저장
    }

    IEnumerator RerollCoroutine()
    {
        yield return new WaitForSeconds(_rerollCoolTime); // 설정된 재장전 시간만큼 대기
        _currentBulletCount = _maxBulletCount; // 탄약 수를 최대로 채움
        BulletUI.ChangeBulletCount(_currentBulletCount); // 재장전 후 UI 업데이트
        BulletUI.StopReroll(); // UI에 재장전 완료 알림
        _rerollCoroutine = null; // 코루틴 참조 제거
    }

    private void PlayImpactEffect(Vector3 position, Vector3 normal)
    {
         if (BulletEffect != null)
         {
            BulletEffect.transform.position = position; // 이펙트 위치 설정
            BulletEffect.transform.forward = normal; // 표면 노멀(법선)에 맞춰 이펙트 정렬
            BulletEffect.Play(); // 이펙트 재생
         }
    }


    private void SpawnTrail(Vector3 startPoint, Vector3 endPoint)
    {
        if (BulletTrailPrefab != null) // 궤적 프리팹이 할당되었는지 확인
        {
            // 궤적 프리팹 인스턴스화
            TrailRenderer trail = Instantiate(BulletTrailPrefab, startPoint, Quaternion.identity);
            // 궤적 이동 코루틴 시작
            StartCoroutine(MoveTrailCoroutine(trail, endPoint));
        }
    }

    private IEnumerator MoveTrailCoroutine(TrailRenderer trail, Vector3 endPoint)
    {
        float time = 0; // 경과 시간 (0 to 1)
        Vector3 startPos = trail.transform.position; // 시작 위치
        // TrailRenderer의 time 값이 0보다 크면 사용, 아니면 기본값(0.1f) 사용 (0으로 나누기 방지)
        float trailTime = trail.time > 0 ? trail.time : 0.1f;

        // 거리와 원하는 속도에 기반하여 이동 시간 추정, 또는 트레일 시간 사용
        float distance = Vector3.Distance(startPos, endPoint);
        float duration = distance / 100f; // 예시: 초당 100 유닛 이동 속도 기준 시간 계산
        // 대안: duration = trailTime; // 트레일이 설정된 시간 동안 지속되도록 함

        while (time < 1f) // 이동 완료 전까지 반복
        {
            // Lerp를 사용하여 시작점과 끝점 사이를 부드럽게 이동
            trail.transform.position = Vector3.Lerp(startPos, endPoint, time);
            // 경과 시간 업데이트 (deltaTime을 duration으로 나누어 정규화)
            time += Time.deltaTime / duration;
            yield return null; // 다음 프레임까지 대기
        }

        trail.transform.position = endPoint; // 정확한 끝 지점에 도달하도록 보장

        // 선택적으로 파괴를 지연시키거나 트레일이 자연스럽게 사라지도록 함 (TrailRenderer의 시간 사용)
        Destroy(trail.gameObject, trailTime);
    }
}