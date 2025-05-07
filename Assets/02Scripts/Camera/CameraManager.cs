using DG.Tweening;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView // Typo: Should ideally be QuarterView
    }

    public CameraQuerterView QuerterView;
    public CameraFollow CameraFollow;
    public CameraRotate CameraRotate;
    public CameraTPSMove CameraTPSMove;
    public Transform PlayerTransform; // 플레이어의 Transform 참조
    public Camera Camera; // Main Camera 컴포넌트

    public CameraViewState CameraView = CameraViewState.FPS;
    public float InitialTPSToPlayerDistance = 3f; // 초기 TPS 카메라와 플레이어 간 거리
    public float InitialTPSCameraHeight = 1.5f; // 초기 TPS 카메라 높이

    [Header("쿼터뷰 흔들림 (Rotation Shake)")]
    public Vector3 QuarterViewShakeRotationStrength = new Vector3(1f, 1f, 1f);
    public float QuarterViewShakeRotationFrequency = 10f;
    public float QuarterViewShakeRotationDuration = 0.2f;
    private Tweener _quarterViewShakeRotationTweener;

    [Header("FPS/TPS 흔들림 (FOV/Viewport Rect)")]
    public float FOVShakeIntensity = 2f;
    public float FOVShakeDuration = 0.15f;
    public float ViewportShakeIntensity = 0.02f;
    public float ViewportShakeDuration = 0.1f;
    private Sequence _fovShakeTweener;
    private Sequence _viewportShakeTweener;

    private int _playerLayer = -1; // "Player" 레이어의 인덱스를 저장할 변수

    private void Awake()
    {
        _playerLayer = LayerMask.NameToLayer("Player");
        if (_playerLayer == -1)
        {
            Debug.LogWarning("CameraManager: 'Player' 레이어를 찾을 수 없습니다. 컬링 마스크 기능이 제대로 동작하지 않을 수 있습니다.");
        }

        if (Camera == null)
        {
            Camera = GetComponent<Camera>();
            if (Camera == null)
            {
                Camera = Camera.main;
            }
            if (Camera == null)
            {
                Debug.LogError("CameraManager: 카메라 컴포넌트를 찾을 수 없습니다. Camera 변수에 카메라를 할당해주세요.");
                enabled = false;
                return;
            }
        }
    }

    private void Start()
    {
        if (Camera == null) return;
        FPS_State();
    }

    private void Update()
    {
        if (Camera == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            FPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Querterview_State(); // Typo: Querterview_State
        }
    }

    private void FPS_State()
    {
        CameraView = CameraViewState.FPS;
        Camera.orthographic = false;

        // --- START OF FIX ---
        if (PlayerTransform != null)
        {
            // Reset Player's pitch and roll, preserving its current yaw (horizontal facing).
            // This ensures PlayerTransform.forward is horizontal and aligns with CameraRotate's initialization.
            float currentYaw = PlayerTransform.eulerAngles.y;
            PlayerTransform.eulerAngles = new Vector3(0f, currentYaw, 0f);
        }
        // --- END OF FIX ---

        if (QuerterView != null) QuerterView.enabled = false;
        if (CameraFollow != null) CameraFollow.enabled = true; // CameraFollow might attach the camera to PlayerTransform
        if (CameraRotate != null) CameraRotate.enabled = true; // CameraRotate will now initialize using the 'cleaned' PlayerTransform rotation
        if (CameraTPSMove != null) CameraTPSMove.enabled = false;

        if (_playerLayer != -1)
        {
            Camera.cullingMask &= ~(1 << _playerLayer);
        }
    }

    private void TPS_State()
    {
        CameraView = CameraViewState.TPS;
        Camera.orthographic = false;

        if (PlayerTransform != null && CameraTPSMove != null)
        {
            CameraTPSMove.enabled = true;
            CameraTPSMove.Target = PlayerTransform;

            Quaternion playerRotation = PlayerTransform.rotation; // Use current player rotation for TPS setup
            Vector3 initialOffset = playerRotation * new Vector3(0f, InitialTPSCameraHeight, -InitialTPSToPlayerDistance);
            Camera.transform.position = PlayerTransform.position + initialOffset;
            Camera.transform.LookAt(PlayerTransform.position + Vector3.up * InitialTPSCameraHeight / 2f); // Look slightly above player's feet
        }
        else
        {
            if (PlayerTransform == null) Debug.LogError("PlayerTransform이 CameraManager에 할당되지 않았습니다.");
            if (CameraTPSMove == null) Debug.LogError("CameraTPSMove가 CameraManager에 할당되지 않았습니다.");
            if (CameraTPSMove != null) CameraTPSMove.enabled = true;
        }

        if (QuerterView != null) QuerterView.enabled = false;
        if (CameraFollow != null) CameraFollow.enabled = false;
        if (CameraRotate != null) CameraRotate.enabled = false;

        if (_playerLayer != -1)
        {
            Camera.cullingMask |= (1 << _playerLayer);
        }
    }

    // Typo: Querterview_State, consider renaming to QuarterViewState or QuarterView_State
    private void Querterview_State()
    {
        CameraView = CameraViewState.QuerterView; // Typo: CameraViewState.QuerterView
        Camera.orthographic = true;

        if (QuerterView != null) QuerterView.enabled = true;
        if (CameraFollow != null) CameraFollow.enabled = false;
        if (CameraRotate != null) CameraRotate.enabled = false;
        if (CameraTPSMove != null) CameraTPSMove.enabled = false;

        if (_playerLayer != -1)
        {
            Camera.cullingMask |= (1 << _playerLayer);
        }
    }

    public void ShakeCamera(float duration, float strength)
    {
        if (Camera == null) return;
        switch (CameraView)
        {
            case CameraViewState.QuerterView: // Typo
                ShakeCameraRotation(duration, Vector3.one*strength);
                break;
            case CameraViewState.FPS:
            case CameraViewState.TPS:
                ShakeCameraViewport(); // Consider if FPS should be FOV and TPS Viewport, or configurable
                // ShakeCameraFOV(); // If you prefer FOV for FPS/TPS
                break;
        }
    }

    public void ShakeCameraRotation(float durationOverride = -1f, Vector3 strengthOverride = default, float frequencyOverride = -1f)
    {
        if (Camera == null || CameraView != CameraViewState.QuerterView) return; // Typo

        float duration = (durationOverride > 0) ? durationOverride : QuarterViewShakeRotationDuration;
        Vector3 strength = (strengthOverride != default) ? strengthOverride : QuarterViewShakeRotationStrength;
        float frequency = (frequencyOverride >= 0) ? frequencyOverride : QuarterViewShakeRotationFrequency;

        _quarterViewShakeRotationTweener?.Kill();
        _quarterViewShakeRotationTweener = Camera.transform.DOShakeRotation(duration, strength, (int)frequency)
            .SetAutoKill(true);
    }

    public void ShakeCameraFOV(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (Camera == null || CameraView == CameraViewState.QuerterView || Camera.orthographic) return; // Typo & check for orthographic

        float duration = (durationOverride > 0) ? durationOverride : FOVShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : FOVShakeIntensity;
        float originalFOV = Camera.fieldOfView;
        float minFOV = originalFOV - intensity;
        float maxFOV = originalFOV + intensity;

        _fovShakeTweener?.Kill();

        _fovShakeTweener = DOTween.Sequence();
        _fovShakeTweener.Append(Camera.DOFieldOfView(maxFOV, duration / 3f).SetEase(Ease.OutQuad)); // Adjusted timings slightly
        _fovShakeTweener.Append(Camera.DOFieldOfView(minFOV, duration / 3f).SetEase(Ease.InOutQuad));
        _fovShakeTweener.Append(Camera.DOFieldOfView(originalFOV, duration / 3f).SetEase(Ease.InQuad));
        _fovShakeTweener.Play().SetAutoKill(true);
    }

    public void ShakeCameraViewport(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (Camera == null || CameraView == CameraViewState.QuerterView) return; // Typo

        float duration = (durationOverride > 0) ? durationOverride : ViewportShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : ViewportShakeIntensity;
        Rect originalViewport = Camera.rect;
        // Corrected randomOffset to be a function for variety
        Vector2 GetRandomOffset() => Random.insideUnitCircle * intensity;

        _viewportShakeTweener?.Kill();

        _viewportShakeTweener = DOTween.Sequence();
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + GetRandomOffset().x, originalViewport.y + GetRandomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.OutQuad));
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + GetRandomOffset().x, originalViewport.y + GetRandomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + GetRandomOffset().x, originalViewport.y + GetRandomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        _viewportShakeTweener.Append(Camera.DORect(originalViewport, duration / 4f).SetEase(Ease.InQuad));
        _viewportShakeTweener.Play().SetAutoKill(true);
    }
}