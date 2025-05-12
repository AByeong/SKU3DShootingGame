using UnityEngine;
using DG.Tweening; // DOTween 에셋 사용 시 (카메라 쉐이크 등)

public class CameraManager : MonoBehaviour
{
    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView // 사용자 코드의 오타 유지
    }

    [Header("Core Assignments")]
    [Tooltip("게임의 메인 카메라를 할당합니다.")]
    public Camera MainCamera;
    [Tooltip("플레이어의 Transform을 할당합니다.")]
    public Transform PlayerTransform;

    [Header("Camera Control Scripts")]
    [Tooltip("쿼터뷰 카메라 제어 스크립트를 할당합니다.")]
    public CameraQuerterView QuerterViewScript; // 쿼터뷰용 스크립트 (사용자 정의)
    [Tooltip("FPS 카메라 추적 스크립트를 할당합니다.")]
    public CameraFollow FPS_CameraFollowScript; // FPS용 간단한 추적 스크립트 (사용자 정의)
    [Tooltip("FPS 카메라 회전 스크립트를 할당합니다.")]
    public CameraRotate FPS_CameraRotateScript; // 제공된 FPS 회전 스크립트
    [Tooltip("TPS 카메라 이동 및 회전 스크립트를 할당합니다.")]
    public CameraTPSMove TPS_CameraMoveScript;   // 제공된 TPS 이동/회전 스크립트

    [Header("Current State")]
    [Tooltip("시작 시 카메라 상태를 설정합니다.")]
    public CameraViewState CurrentCameraView = CameraViewState.FPS;

    [Header("Shake Parameters - General")]
    public float DefaultShakeDuration = 0.2f;
    public float DefaultShakeStrength = 1f;

    [Header("QuarterView Settings")] // <<< [NEW] 쿼터뷰 전용 설정 추가
    [Tooltip("쿼터뷰 시 카메라의 월드 포지션입니다.")]
    public Vector3 QuarterViewPosition = new Vector3(0, 10, -10); // 예시 값, Inspector에서 조정하세요.
    [Tooltip("쿼터뷰 시 카메라의 월드 회전값 (오일러 각도)입니다.")]
    public Vector3 QuarterViewEulerRotation = new Vector3(45, 0, 0); // 예시 값, Inspector에서 조정하세요.
    [Tooltip("쿼터뷰 시 카메라의 직교 크기입니다.")]
    public float QuarterViewOrthographicSize = 10f; // 예시 값, Inspector에서 조정하세요.


    [Header("QuarterView Shake (Rotation)")]
    public Vector3 QuarterViewShakeRotationStrength = new Vector3(1f, 1f, 1f);
    public float QuarterViewShakeRotationFrequency = 10f;
    public float QuarterViewShakeRotationDuration = 0.2f;
    private Tweener _quarterViewShakeRotationTweener;

    [Header("FPS/TPS Shake (FOV/Viewport Rect)")]
    public float FOVShakeIntensity = 2f;
    public float FOVShakeDuration = 0.15f;
    public float ViewportShakeIntensity = 0.02f;
    public float ViewportShakeDuration = 0.1f;
    private Sequence _fovShakeTweener;
    private Sequence _viewportShakeTweener;

    private int _playerLayer = -1;

    void Awake()
    {
        _playerLayer = LayerMask.NameToLayer("Player");
        if (_playerLayer == -1)
        {
            Debug.LogWarning("CameraManager: 'Player' 레이어를 찾을 수 없습니다. 컬링 마스크 기능이 제대로 동작하지 않을 수 있습니다.", this);
        }

        if (MainCamera == null)
        {
            MainCamera = Camera.main;
            if (MainCamera == null)
            {
                Debug.LogError("CameraManager: 메인 카메라가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
                enabled = false;
                return;
            }
        }

        if (PlayerTransform == null) Debug.LogError("CameraManager: PlayerTransform이 할당되지 않았습니다.", this);
        if (TPS_CameraMoveScript == null) Debug.LogError("CameraManager: TPS_CameraMoveScript가 할당되지 않아 TPS 모드가 정상 작동하지 않을 수 있습니다.", this);
        if (QuerterViewScript == null) Debug.LogWarning("CameraManager: QuerterViewScript가 할당되지 않았습니다. 쿼터뷰 전용 로직이 있다면 Inspector에서 할당해주세요.", this);
        if (FPS_CameraFollowScript == null) Debug.LogWarning("CameraManager: FPS_CameraFollowScript가 할당되지 않았습니다.", this);
        if (FPS_CameraRotateScript == null) Debug.LogWarning("CameraManager: FPS_CameraRotateScript가 할당되지 않았습니다.", this);
    }

    void Start()
    {
        DisableAllCameraScripts();
        SwitchCameraState(CurrentCameraView, true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8)) SwitchCameraState(CameraViewState.FPS);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) SwitchCameraState(CameraViewState.TPS);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) SwitchCameraState(CameraViewState.QuerterView);
    }

    void DisableAllCameraScripts()
    {
        if (QuerterViewScript != null) QuerterViewScript.enabled = false;
        if (FPS_CameraFollowScript != null) FPS_CameraFollowScript.enabled = false;
        if (FPS_CameraRotateScript != null) FPS_CameraRotateScript.enabled = false;
        if (TPS_CameraMoveScript != null) TPS_CameraMoveScript.enabled = false;
    }

    public void SwitchCameraState(CameraViewState newState, bool isInitialSetup = false)
    {
        if (!isInitialSetup && newState == CurrentCameraView && IsCurrentStateScriptEnabled(newState))
        {
            return;
        }

        CurrentCameraView = newState;

        //DisableAllCameraScripts();
        KillShakeTweens();

        switch (CurrentCameraView)
        {
            case CameraViewState.FPS:
                SetupFPSState();
                break;
            case CameraViewState.TPS:
                SetupTPSState();
                break;
            case CameraViewState.QuerterView:
                SetupQuerterViewState(); // 오타 수정: QuerterView -> QuarterView (메서드 이름은 일관성 위해 오타 유지)
                break;
        }
    }
    
    private bool IsCurrentStateScriptEnabled(CameraViewState state)
    {
        switch (state)
        {
            case CameraViewState.FPS:
                // FPS는 Follow와 Rotate 스크립트 둘 다 활성화될 수 있으므로, 핵심인 Rotate 스크립트 기준으로 확인하거나,
                // 또는 두 스크립트 중 하나라도 null이 아니고 활성화되어 있다면 true로 볼 수도 있습니다.
                // 여기서는 Rotate 스크립트를 기준으로 합니다.
                return (FPS_CameraRotateScript != null && FPS_CameraRotateScript.enabled) || (FPS_CameraFollowScript != null && FPS_CameraFollowScript.enabled) ;
            case CameraViewState.TPS:
                return TPS_CameraMoveScript != null && TPS_CameraMoveScript.enabled;
            case CameraViewState.QuerterView:
                // 쿼터뷰는 QuerterViewScript가 없어도 CameraManager가 직접 위치를 설정하므로,
                // 여기서는 스크립트 유무보다는 상태 자체로 판단하는 것이 더 적합할 수 있습니다.
                // 하지만 기존 로직을 유지하며, QuerterViewScript의 활성화 여부를 반환합니다.
                // 만약 QuerterViewScript가 선택적이라면, 이 부분은 수정이 필요할 수 있습니다.
                return QuerterViewScript != null && QuerterViewScript.enabled;
            default:
                return false;
        }
    }


    private void SetupFPSState()
    {
        if (MainCamera == null || PlayerTransform == null)
        {
            Debug.LogError("FPS State 설정 실패: MainCamera 또는 PlayerTransform이 없습니다.", this);
            return;
        }

        MainCamera.orthographic = false; // <<< FPS는 원근 투영
        Cursor.lockState = CursorLockMode.Confined; // 또는 CursorLockMode.Locked
        Cursor.visible = false; // FPS에서는 일반적으로 커서 숨김

        if (FPS_CameraFollowScript != null)
        {
            FPS_CameraFollowScript.enabled = true;
            // FPS_CameraFollowScript.Target = PlayerTransform; // 필요하다면 타겟 재할당
            // FPS_CameraFollowScript에 필요한 추가 설정 (예: 타겟, 오프셋)
        }

        if (FPS_CameraRotateScript != null)
        {
            FPS_CameraRotateScript.enabled = true;
            FPS_CameraRotateScript.PlayerTransform = this.PlayerTransform;
            FPS_CameraRotateScript.InitializeRotation();
        }

        if (_playerLayer != -1) MainCamera.cullingMask &= ~(1 << _playerLayer);
    }

    private void SetupTPSState()
    {
        if (MainCamera == null || PlayerTransform == null || TPS_CameraMoveScript == null)
        {
            Debug.LogError("TPS State 설정 실패: MainCamera, PlayerTransform, 또는 TPS_CameraMoveScript가 없습니다.", this);
            return;
        }

        MainCamera.orthographic = false; // <<< TPS는 원근 투영
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        TPS_CameraMoveScript.Initialize(PlayerTransform, MainCamera);
        TPS_CameraMoveScript.enabled = true;

        Vector3 baseOffset = TPS_CameraMoveScript._offset;
        float initialPitch = TPS_CameraMoveScript.DefaultPitch;
        float initialRelativeYaw = 0f;

        Quaternion initialCamWorldOrbitRot = Quaternion.Euler(initialPitch, initialRelativeYaw + PlayerTransform.eulerAngles.y, 0f);

        MainCamera.transform.position = PlayerTransform.position + (initialCamWorldOrbitRot * baseOffset);
        MainCamera.transform.LookAt(PlayerTransform.position + Vector3.up * baseOffset.y);

        if (_playerLayer != -1) MainCamera.cullingMask |= (1 << _playerLayer);
    }

    private void SetupQuerterViewState() // 메서드 이름은 사용자의 오타를 유지합니다.
    {
        if (MainCamera == null)
        {
            Debug.LogError("QuerterView State 설정 실패: MainCamera가 없습니다.", this);
            return;
        }

        MainCamera.orthographic = true; // <<< 쿼터뷰는 직교 투영
        MainCamera.orthographicSize = QuarterViewOrthographicSize; // <<< 직교 크기 설정
        MainCamera.transform.position = QuarterViewPosition;       // <<< 위치 설정
        MainCamera.transform.rotation = Quaternion.Euler(QuarterViewEulerRotation); // <<< 회전값 설정

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (QuerterViewScript != null)
        {
            QuerterViewScript.enabled = true;
            // QuerterViewScript에 필요한 추가 설정 (예: 타겟 할당 등)
            // 만약 QuerterViewScript가 카메라 위치/회전을 직접 제어한다면,
            // 위의 MainCamera.transform 설정과 충돌하지 않도록 주의해야 합니다.
            // 이 예제에서는 CameraManager가 직접 위치/회전을 설정하고,
            // QuerterViewScript는 추가적인 로직(예: 특정 대상 추적)을 담당할 수 있습니다.
        }
        else
        {
            // QuerterViewScript가 할당되지 않았어도, 위에서 카메라 위치/회전을 고정했으므로 기본 작동은 합니다.
            // Debug.Log("CameraManager: QuerterViewScript가 할당되지 않았습니다. 고정된 쿼터뷰 카메라를 사용합니다.");
        }
        if (_playerLayer != -1) MainCamera.cullingMask |= (1 << _playerLayer);
    }

    // --- 카메라 흔들림 함수들 (DOTween 필요) ---
    public void ShakeCamera(float duration, float strength)
    {
        if (MainCamera == null) return;
        switch (CurrentCameraView)
        {
            case CameraViewState.QuerterView:
                ShakeCameraRotation(duration > 0 ? duration : QuarterViewShakeRotationDuration, Vector3.one * strength, -1f);
                break;
            case CameraViewState.FPS:
            case CameraViewState.TPS:
                // FPS/TPS에서는 Viewport Shake 또는 FOV Shake 중 선택 또는 조합 가능
                ShakeCameraViewport(duration > 0 ? duration : ViewportShakeDuration, ViewportShakeIntensity * strength / DefaultShakeStrength);
                // 또는 ShakeCameraFOV(duration > 0 ? duration : FOVShakeDuration, FOVShakeIntensity * strength / DefaultShakeStrength);
                break;
        }
    }

    public void ShakeCameraRotation(float durationOverride = -1f, Vector3 strengthOverride = default, float frequencyOverride = -1f)
    {
        if (MainCamera == null || CurrentCameraView != CameraViewState.QuerterView) return; // 쿼터뷰에서만 회전 쉐이크 사용
        KillShakeTweens();
        float duration = (durationOverride > 0) ? durationOverride : QuarterViewShakeRotationDuration;
        Vector3 strength = (strengthOverride != default) ? strengthOverride : QuarterViewShakeRotationStrength;
        float frequency = (frequencyOverride >= 0) ? frequencyOverride : QuarterViewShakeRotationFrequency;

        // 수정된 부분: .SetOptions(false) 제거
        // DOShakeRotation의 마지막 파라미터 'false'는 fadeOut을 비활성화하는 것입니다.
        _quarterViewShakeRotationTweener = MainCamera.transform.DOShakeRotation(duration, strength, (int)frequency, 90f, false)
            .SetAutoKill(true);
    }

    public void ShakeCameraFOV(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (MainCamera == null || MainCamera.orthographic) return; // 직교 카메라는 FOV 없음
        KillShakeTweens();
        float duration = (durationOverride > 0) ? durationOverride : FOVShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : FOVShakeIntensity;
        float originalFOV = MainCamera.fieldOfView;
        _fovShakeTweener = DOTween.Sequence()
            .Append(MainCamera.DOFieldOfView(originalFOV + intensity, duration / 2f).SetEase(Ease.OutQuad))
            .Append(MainCamera.DOFieldOfView(originalFOV, duration / 2f).SetEase(Ease.InQuad))
            .SetAutoKill(true).Play();
    }

    public void ShakeCameraViewport(float durationOverride = -1f, float intensityOverride = -1f)
    {
        // Viewport 쉐이크는 직교 카메라에도 적용 가능하지만, 효과가 다를 수 있습니다.
        if (MainCamera == null) return;
        KillShakeTweens();
        float duration = (durationOverride > 0) ? durationOverride : ViewportShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : ViewportShakeIntensity;
        Rect originalViewport = MainCamera.rect;

        _viewportShakeTweener = DOTween.Sequence();
        int shakes = 4;
        float shakeDur = duration / (float)shakes;
        for (int i = 0; i < shakes - 1; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * intensity;
            _viewportShakeTweener.Append(MainCamera.DORect(new Rect(originalViewport.x + randomOffset.x, originalViewport.y + randomOffset.y, originalViewport.width, originalViewport.height), shakeDur).SetEase(Ease.InOutQuad));
        }
        _viewportShakeTweener.Append(MainCamera.DORect(originalViewport, shakeDur).SetEase(Ease.InQuad));
        _viewportShakeTweener.SetAutoKill(true).Play();
    }

    private void KillShakeTweens()
    {
        _quarterViewShakeRotationTweener?.Kill(true);
        _fovShakeTweener?.Kill(true);
        _viewportShakeTweener?.Kill(true);
    }
}