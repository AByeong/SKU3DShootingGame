using System.Collections;
using UnityEngine;

public class DefaultGun : Weapon
{
    public override void Initialize()
    {
        CurrentAmmo = MaxBulletCount;
    }
    
   public override void Fire()
    {

        if (CurrentAmmo > 0)
        {
            CurrentAmmo--;
            BulletUI.ChangeBulletCount(CurrentAmmo);

            Vector3 targetPoint; // 조준 목표 지점
            Vector3 fireDirection; // 실제 발사 방향
            bool aimingHit = GetAimTargetPoint(out targetPoint); // 플레이어가 조준하는 위치 가져오기

            // 발사 위치에서 목표 지점까지의 실제 방향 계산
            fireDirection = (targetPoint - FirePosition.position).normalized;

            // FirePosition에서 데미지 판정을 위한 실제 레이캐스트 수행
            RaycastHit damageHitInfo;
            if (Physics.Raycast(FirePosition.position, fireDirection, out damageHitInfo, MaxShotDistance))
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
                if (BulletEffect != null)
                {
                    PlayImpactEffect(damageHitInfo.point, damageHitInfo.normal);
                }
            }



            // --- 총알 궤적 생성 ---
            SpawnTrail(FirePosition.position, targetPoint);
        }

    }

    

    //***************피격 이팩트 재생**********
    private void PlayImpactEffect(Vector3 position, Vector3 normal)
    {
        
            BulletEffect.transform.position = position; // 이펙트 위치 설정
            BulletEffect.transform.forward = normal; // 표면 노멀(법선)에 맞춰 이펙트 정렬
            BulletEffect.Play(); // 이펙트 재생
        
    }
    
    
    //*******궤적 그리기****
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
    

   
   
   
    public override void Reroll()
    {
        Debug.Log("Reroll!!");
        // 이미 재장전 중이라면, 또 시작하지 않음
        if (RerollCoroutine != null || CurrentAmmo == MaxBulletCount) return;

        BulletUI.StartReroll(RerollCoolTime); // UI에 재장전 진행 상황 표시 요청
        RerollCoroutine = StartCoroutine(Reroll_Coroutine()); // 재장전 코루틴 시작 및 참조 저장
    }
    
    IEnumerator Reroll_Coroutine()
    {
        yield return new WaitForSeconds(RerollCoolTime); // 설정된 재장전 시간만큼 대기
        CurrentAmmo = MaxBulletCount; // 탄약 수를 최대로 채움
        BulletUI.ChangeBulletCount(CurrentAmmo); // 재장전 후 UI 업데이트
        BulletUI.StopReroll(); // UI에 재장전 완료 알림
        RerollCoroutine = null; // 코루틴 참조 제거
        Debug.Log("RerollEnd!!");
    }
   
}
