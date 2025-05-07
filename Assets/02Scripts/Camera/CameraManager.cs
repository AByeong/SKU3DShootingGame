using DG.Tweening;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView // 참고: QuarterView 오타 가능성 (QuarterView)
    }

    public CameraQuerterView QuerterView; // 참고: QuarterView 오타 가능성 (QuarterViewBehaviour)
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
        // "Player" 레이어의 인덱스를 가져옵니다.
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
                Camera = Camera.main; // 최후의 수단으로 메인 카메라를 찾아봅니다.
            }
            if (Camera == null)
            {
                Debug.LogError("CameraManager: 카메라 컴포넌트를 찾을 수 없습니다. Camera 변수에 카메라를 할당해주세요.");
                enabled = false; // 스크립트 비활성화
                return;
            }
        }
    }

    private void Start()
    {
        if (Camera == null) return; // Awake에서 카메라를 못찾았으면 Start 실행 중지
        FPS_State(); // 초기 상태 설정
    }

    private void Update()
    {
        if (Camera == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha8)) // FPS
        {
            FPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9)) // TPS
        {
            TPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) // QuarterView
        {
            Querterview_State();
        }
    }

    private void FPS_State()
    {
        CameraView = CameraViewState.FPS;

        Camera.orthographic = false;
        if (QuerterView != null) QuerterView.enabled = false;
        if (CameraFollow != null) CameraFollow.enabled = true;
        if (CameraRotate != null) CameraRotate.enabled = true;
        if (CameraTPSMove != null) CameraTPSMove.enabled = false;

        // FPS 모드: Player 레이어 컬링 (보이지 않게)
        if (_playerLayer != -1)
        {
            Camera.cullingMask &= ~(1 << _playerLayer);
        }
    }

    private void TPS_State()
    {
        CameraView = CameraViewState.TPS;

        if (PlayerTransform != null && CameraTPSMove != null)
        {
            CameraTPSMove.enabled = true;
            CameraTPSMove.Target = PlayerTransform;

            Quaternion playerRotation = PlayerTransform.rotation;
            Vector3 initialOffset = playerRotation * new Vector3(0f, InitialTPSCameraHeight, -InitialTPSToPlayerDistance);
            Camera.transform.position = PlayerTransform.position + initialOffset;
            Camera.transform.LookAt(PlayerTransform.position + Vector3.up * InitialTPSCameraHeight / 2f);

            CameraTPSMove._rotationX = Camera.transform.eulerAngles.y - PlayerTransform.eulerAngles.y;
            CameraTPSMove._rotationY = Camera.transform.eulerAngles.x;

            Camera.orthographic = false;
            if (QuerterView != null) QuerterView.enabled = false;
            if (CameraFollow != null) CameraFollow.enabled = false;
            if (CameraRotate != null) CameraRotate.enabled = false;
        }
        else
        {
            if (PlayerTransform == null) Debug.LogError("PlayerTransform이 CameraManager에 할당되지 않았습니다.");
            if (CameraTPSMove == null) Debug.LogError("CameraTPSMove가 CameraManager에 할당되지 않았습니다.");

            Camera.orthographic = false;
            if (QuerterView != null) QuerterView.enabled = false;
            if (CameraFollow != null) CameraFollow.enabled = false;
            if (CameraRotate != null) CameraRotate.enabled = false;
            if (CameraTPSMove != null) CameraTPSMove.enabled = true;
        }

        // TPS 모드: Player 레이어 보이도록 설정
        if (_playerLayer != -1)
        {
            Camera.cullingMask |= (1 << _playerLayer);
        }
    }

    private void Querterview_State() // 참고: 오타일 경우 QuarterViewState
    {
        CameraView = CameraViewState.QuerterView;

        if (QuerterView != null) QuerterView.enabled = true;
        if (CameraFollow != null) CameraFollow.enabled = false;
        if (CameraRotate != null) CameraRotate.enabled = false;
        if (CameraTPSMove != null) CameraTPSMove.enabled = false;
        // 참고: 필요하다면 여기서 Camera.orthographic = true; 설정 (쿼터뷰 카메라 스크립트에서 제어할 수도 있음)


        // QuarterView 모드: Player 레이어 보이도록 설정
        if (_playerLayer != -1)
        {
            Camera.cullingMask |= (1 << _playerLayer);
        }
    }

    public void ShakeCamera()
    {
        if (Camera == null) return;
        switch (CameraView)
        {
            case CameraViewState.QuerterView:
                ShakeCameraRotation();
                break;
            case CameraViewState.FPS:
            case CameraViewState.TPS:
                ShakeCameraViewport();
                break;
        }
    }

    public void ShakeCameraRotation(float durationOverride = -1f, Vector3 strengthOverride = default, float frequencyOverride = -1f)
    {
        if (Camera == null || CameraView != CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : QuarterViewShakeRotationDuration;
        Vector3 strength = (strengthOverride != default) ? strengthOverride : QuarterViewShakeRotationStrength;
        float frequency = (frequencyOverride >= 0) ? frequencyOverride : QuarterViewShakeRotationFrequency;

        _quarterViewShakeRotationTweener?.Kill(); // 이전 트위너 Kill
        _quarterViewShakeRotationTweener = Camera.transform.DOShakeRotation(duration, strength, (int)frequency)
            .SetAutoKill(true);
    }

    public void ShakeCameraFOV(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (Camera == null || CameraView == CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : FOVShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : FOVShakeIntensity;
        float originalFOV = Camera.fieldOfView;
        float minFOV = originalFOV - intensity;
        float maxFOV = originalFOV + intensity;

        _fovShakeTweener?.Kill();

        _fovShakeTweener = DOTween.Sequence(); // 새로운 시퀀스 생성
        _fovShakeTweener.Append(Camera.DOFieldOfView(maxFOV, duration / 2f).SetEase(Ease.OutQuad));
        _fovShakeTweener.Append(Camera.DOFieldOfView(minFOV, duration / 2f).SetEase(Ease.InQuad));
        _fovShakeTweener.Append(Camera.DOFieldOfView(originalFOV, duration / 2f).SetEase(Ease.OutQuad));
        _fovShakeTweener.Play().SetAutoKill(true);
    }

    public void ShakeCameraViewport(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (Camera == null || CameraView == CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : ViewportShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : ViewportShakeIntensity;
        Rect originalViewport = Camera.rect;
        Vector2 randomOffset() => Random.insideUnitCircle * intensity;

        _viewportShakeTweener?.Kill();

        _viewportShakeTweener = DOTween.Sequence(); // 새로운 시퀀스 생성
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.OutQuad));
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        _viewportShakeTweener.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        _viewportShakeTweener.Append(Camera.DORect(originalViewport, duration / 4f).SetEase(Ease.InQuad));
        _viewportShakeTweener.Play().SetAutoKill(true);
    }
}