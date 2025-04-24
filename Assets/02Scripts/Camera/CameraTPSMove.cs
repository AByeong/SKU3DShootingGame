using UnityEngine;
using System.Collections;

public class CameraTPSMove : MonoBehaviour
{
    [Header("타겟 및 속도")]
    [Tooltip("따라갈 대상 (플레이어)")]
    public Transform ObjectToFollow;
    [Tooltip("따라가는 속도")]
    [SerializeField] private float _followSpeed = 10f;
    [Tooltip("마우스 회전 감도")]
    [SerializeField] private float _sensitivity = 100f;

    [Header("회전 제한")] // 헤더 복구
    [Tooltip("수직 회전 최대 각도 제한")]
    [SerializeField] private float _clampAngle = 70f; // 변수 복구

    // 내부 회전 값
    private float _rotX; // 수직 회전값 변수 복구
    private float _rotY; // 수평 회전값

    [Header("카메라 위치 및 거리")] // 헤더 이름 변경
    [Tooltip("실제 카메라의 Transform")]
    public Transform CameraTransform;
    [Tooltip("피봇(이 오브젝트) 기준 카메라의 로컬 오프셋 (실시간 조정 가능)")]
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 1.5f, -5f);
    // [Tooltip("카메라의 고정된 수직 각도")] // 이 변수는 제거
    // [SerializeField] private float _fixedVerticalAngle = 10f;
    [Tooltip("카메라가 타겟에 가장 가까워질 수 있는 최소 거리 (충돌 시)")]
    [SerializeField] private float _minDistance = 1f;

    // 내부 거리 및 방향 계산용 변수
    private Vector3 _dirNormalized;
    private Vector3 _finalDirection;
    private float _finalDistance;
    private float _currentMaxDistance;
    [Tooltip("카메라 위치 보간(스무딩) 속도")]
    [SerializeField] private float _smoothing = 10f;

    [Header("옵션")] // 헤더 복구
    [Tooltip("마우스 Y축 입력 반전 여부")]
    [SerializeField] private bool _invertY = false; // 변수 복구

    private void Start()
    {
        if (CameraTransform == null) { Debug.LogError("CameraTransform 미할당!", this); enabled = false; return; }
        if (ObjectToFollow == null) { Debug.LogError("ObjectToFollow 미할당!", this); enabled = false; return; }
        InitializeCameraState();
    }

    private void OnEnable()
    {
        if (CameraTransform != null && ObjectToFollow != null) { InitializeCameraState(); }
    }

    public void InitializeCameraState()
    {
        transform.position = ObjectToFollow.position;

        // 초기 회전 설정 (플레이어 방향 기준, 수직 각도는 0 또는 원하는 값으로 시작)
        float initialPitch = 10f; // 예시: 시작 시 약간 아래를 보도록 10도 설정
        transform.rotation = Quaternion.Euler(initialPitch, ObjectToFollow.eulerAngles.y, 0f);
        _rotX = initialPitch; // 내부 변수도 초기화
        _rotY = transform.eulerAngles.y;

        CameraTransform.localPosition = _cameraOffset;
        _finalDistance = _cameraOffset.magnitude;
    }


    private void Update()
    {
        // 마우스 입력으로 피봇 회전값 업데이트 (수직, 수평 모두)
        _rotY += Input.GetAxis("Mouse X") * _sensitivity * Time.deltaTime;
        _rotX += (_invertY ? 1 : -1) * Input.GetAxis("Mouse Y") * _sensitivity * Time.deltaTime; // 수직 회전 로직 복구
        _rotX = Mathf.Clamp(_rotX, -_clampAngle, _clampAngle); // 수직 회전 제한 로직 복구

        // 최종 회전값 생성: 수직(_rotX), 수평(_rotY) 사용, Z축은 0으로 고정!
        Quaternion rot = Quaternion.Euler(_rotX, _rotY, 0);
        transform.rotation = rot; // 피봇 회전 적용
    }

    private void LateUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, ObjectToFollow.position, _followSpeed * Time.deltaTime);

        _dirNormalized = _cameraOffset.normalized;
        _currentMaxDistance = _cameraOffset.magnitude;

        _finalDirection = transform.TransformPoint(_dirNormalized * _currentMaxDistance);

        RaycastHit hit;
        if (Physics.Linecast(transform.position, _finalDirection, out hit))
        {
            _finalDistance = Mathf.Clamp(hit.distance, _minDistance, _currentMaxDistance);
        }
        else
        {
            _finalDistance = _currentMaxDistance;
        }

        CameraTransform.localPosition = Vector3.Lerp(CameraTransform.localPosition, _dirNormalized * _finalDistance, Time.deltaTime * _smoothing);

        // --- 추가: 카메라 자체의 로컬 Z축 회전 강제 0 설정 ---
        // 만약 카메라가 계속 삐딱해진다면, 이 부분을 활성화하여 매 프레임 카메라의 로컬 Z축 회전을 0으로 강제합니다.
        // Vector3 camLocalEuler = CameraTransform.localEulerAngles;
        // if (Mathf.Abs(camLocalEuler.z) > 0.01f) // 아주 작은 오차가 아니라면 강제 적용
        // {
        //     CameraTransform.localRotation = Quaternion.Euler(camLocalEuler.x, camLocalEuler.y, 0f);
        // }
        // --- 또는 더 간단하게 (카메라 자체의 로컬 X,Y 회전이 필요 없다면):
        // CameraTransform.localRotation = Quaternion.identity; // 로컬 회전을 완전히 초기화 (카메라가 항상 피봇의 정면만 바라보게 됨)
    }
}