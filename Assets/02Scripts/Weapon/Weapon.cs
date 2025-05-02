using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Weapon : MonoBehaviour
{


    public Animator Animator;
    
    
    [Header("에셋")]
    public ParticleSystem[] BulletEffect;
    public TrailRenderer BulletTrailPrefab;
    [Header("UI")]
    public UI_Bullet BulletUI;
    
    [Header("파라미터")]
    public int CurrentAmmo;
    public CameraManager CameraManager;
    public Transform FirePosition;
    public float MaxShotDistance; //최대 사정거리
    public Vector3 ShotDirection; //사격 방향
    public float  ShotCoolTime;
    public int MaxBulletCount = 50; // 최대 총알 개수
    public float RerollCoolTime;
    
    public bool ShotPossible = true;
    public Coroutine RerollCoroutine;
    public Vector3 ReboundOffset;

    private void Start()
    {
        Initialize();
    }

    public abstract void Initialize();


    public void Attack()
    {
        if(CurrentAmmo <= 0) Reroll();
        
        Debug.Log("Shot!");
        Fire();
        
    }
    
    public abstract void Fire();


    public abstract void Reroll();


    
    public bool GetAimTargetPoint(out Vector3 targetPoint)
    {
        Ray aimRay; // 조준 광선
        bool hitDetected = false; // 충돌 감지 여부
        RaycastHit hitInfo; // 충돌 정보

        switch (CameraManager.CameraView)
        {
            case CameraManager.CameraViewState.FPS:
            case CameraManager.CameraViewState.TPS:

                // FPS의 경우, 조준 광선은 화면 중앙(카메라)에서 시작
                aimRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                if (Physics.Raycast(aimRay, out hitInfo, MaxShotDistance))
                {
                    targetPoint = hitInfo.point;
                    hitDetected = true;
                }
                else
                {
                    // 맞춘 대상이 없으면, 카메라 전방 방향으로 멀리 떨어진 지점을 조준
                    targetPoint = aimRay.origin + aimRay.direction * MaxShotDistance;
                    hitDetected = false;
                }
                break;
            

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
                    targetPoint = FirePosition.position + transform.forward * MaxShotDistance; // 플레이어의 전방 방향 사용
                     hitDetected = false;
                }
                break;

            default:
                Debug.LogError("GetAimTargetPoint에서 처리되지 않은 CameraViewState!");
                targetPoint = FirePosition.position + transform.forward * MaxShotDistance; // 기본 대체 처리
                hitDetected = false;
                break;
        }
        return hitDetected;
    }
    
    // IEnumerator ShotCooldownCoroutine()
    // {
    //     Debug.Log("Cooldown!");
    //     ShotPossible = false; // 발사 불가능 상태로 변경
    //     yield return new WaitForSeconds(ShotCoolTime); // 설정된 쿨타임만큼 대기
    //     ShotPossible = true; // 발사 가능 상태로 복귀
    //     Debug.Log("CooldownEnd!");
    // }
    
}