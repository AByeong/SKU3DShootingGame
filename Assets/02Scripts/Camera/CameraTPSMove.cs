using UnityEngine;

public class CameraTPSMove : MonoBehaviour
{
    [Header("TPS Settings")]
    [Tooltip("카메라의 기본 오프셋 값입니다. 피치 및 상대 요(yaw)가 0일 때 목표로부터의 거리입니다.")]
    public Vector3 _offset = new Vector3(0f, 1.5f, -3f); // 예시 기본 오프셋

    [Tooltip("마우스 입력에 따른 카메라 회전 속도입니다.")]
    public float _rotationSpeed = 150f;

    [Tooltip("카메라의 기본 수직 각도(피치)입니다. 양수 값은 약간 아래를 향합니다.")]
    public float DefaultPitch = 15f;

    [Tooltip("카메라의 최소 수직 각도(피치)입니다.")]
    public float MinPitch = -40f;

    [Tooltip("카메라의 최대 수직 각도(피치)입니다.")]
    public float MaxPitch = 40f;

    // CameraManager에 의해 할당될 변수들
    [HideInInspector] public Camera Camera;
    [HideInInspector] public Transform Target;

    // 마우스 입력에 의해 누적될 내부 궤도 회전값
    private float _currentRotationX; // 수평 궤도 각도 (요)
    private float _currentRotationY; // 수직 궤도 각도 (피치)

    void OnEnable()
    {
        if (Target == null)
        {
            // CameraManager가 Target을 설정하기 전에 OnEnable이 호출될 수 있으므로,
            // Target이 null이어도 바로 비활성화하지 않고 LateUpdate에서 확인하도록 변경하거나,
            // CameraManager가 Initialize를 호출한 후 enabled = true 하도록 순서를 조정할 수 있습니다.
            // 여기서는 CameraManager가 Initialize를 호출하고 enabled=true 할 것으로 가정합니다.
            // 만약 CameraManager가 단순히 enabled=true만 한다면, Start()나 Awake()에서 초기 참조를 설정해야 합니다.
            // Debug.LogWarning("CameraTPSMove: Target이 아직 할당되지 않았습니다. CameraManager가 설정할 것입니다.", this);
            return; // Target/Camera가 설정될 때까지 기다립니다.
        }
        if (Camera == null)
        {
            // Debug.LogWarning("CameraTPSMove: Camera가 아직 할당되지 않았습니다. CameraManager가 설정할 것입니다.", this);
            return;
        }

        // 이 스크립트가 활성화될 때 (주로 CameraManager에 의해 TPS 모드로 전환될 때),
        // 누적된 궤도 각도를 초기화합니다.
        // CameraManager는 이 초기화된 값들(수평 0, 수직 DefaultPitch)을 기준으로
        // 카메라의 절대 위치/회전을 이미 설정해 놓은 상태여야 합니다.
        _currentRotationX = 0f;
        _currentRotationY = DefaultPitch;

        // 커서 상태 관리 (TPS 모드에 적합하게)
        // Cursor.lockState = CursorLockMode.Locked; // 또는 None, 게임 디자인에 따라
        // Cursor.visible = false; // 또는 true
    }

    void LateUpdate()
    {
        // Target 또는 Camera가 아직 할당되지 않았으면 아무것도 하지 않습니다.
        // (CameraManager가 Initialize를 통해 설정해 줄 때까지 대기)
        if (Target == null || Camera == null)
        {
            return;
        }
        ApplyCameraTransform();
    }

    void ApplyCameraTransform()
    {
        // 마우스 입력을 받아 궤도 각도를 누적합니다.
        float mouseX = Input.GetAxis("Mouse X") * _rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * _rotationSpeed * Time.deltaTime;

        _currentRotationX += mouseX;
        _currentRotationY -= mouseY; // Y축은 반전시켜야 자연스러운 상하 조작이 됩니다.
        _currentRotationY = Mathf.Clamp(_currentRotationY, MinPitch, MaxPitch); // 수직 각도를 제한합니다.

        // 카메라의 목표 궤도 회전을 계산합니다.
        // 목표물(플레이어)의 현재 Y축 회전 + 마우스로 누적된 수평(_currentRotationX) 및 수직(_currentRotationY) 회전을 합칩니다.
        Quaternion targetOrbitalRotation = Quaternion.Euler(_currentRotationY, _currentRotationX + Target.eulerAngles.y, 0f);

        // 기본 오프셋(_offset)을 계산된 궤도 회전만큼 회전시켜 최종 오프셋 벡터를 구합니다.
        Vector3 finalOffset = targetOrbitalRotation * _offset;

        // 카메라 위치를 설정합니다: 목표물 위치 + 최종 오프셋.
        Camera.transform.position = Target.position + finalOffset;

        // 카메라가 목표물의 특정 지점(기본 _offset의 Y값을 사용한 높이)을 바라보도록 합니다.
        // 이를 통해 카메라 위치에 관계없이 일관된 시야를 유지하는 데 도움이 됩니다.
        Vector3 lookAtPoint = Target.position + Vector3.up * _offset.y;
        Camera.transform.LookAt(lookAtPoint);
    }

    // CameraManager가 Target과 Camera를 명시적으로 설정하기 위한 공용 메서드
    public void Initialize(Transform target, Camera cameraComponent)
    {
        Target = target;
        Camera = cameraComponent;

        // Initialize가 호출된 후 OnEnable에서 설정된 초기값을 기준으로 카메라를 즉시 배치할 수 있도록
        // 여기서 _currentRotationX, _currentRotationY를 다시 설정할 수도 있습니다.
        // 하지만 CameraManager의 SetupTPSState에서 정확한 초기 위치를 설정하므로 OnEnable의 로직으로 충분할 수 있습니다.
        // 만약 OnEnable보다 Initialize가 항상 먼저 호출된다는 보장이 있다면 여기서 _currentRotationX/Y 초기화도 가능.
    }
}