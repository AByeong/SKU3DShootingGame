using DG.Tweening;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView // 참고: QuarterView 오타 가능성
    }

    public CameraQuerterView QuerterView; // 참고: QuarterView 오타 가능성
    public CameraFollow CameraFollow;
    public CameraRotate CameraRotate;
    public CameraTPSMove CameraTPSMove;
    public Transform PlayerTransform; // 플레이어의 Transform 참조
    public Camera Camera;

    public CameraViewState CameraView = CameraViewState.FPS;
    public float InitialTPSToPlayerDistance = 3f; // 초기 TPS 카메라와 플레이어 간 거리
    public float InitialTPSCameraHeight = 1.5f; // 초기 TPS 카메라 높이

    [Header("쿼터뷰 흔들림 (Rotation Shake)")]
    public Vector3 QuarterViewShakeRotationStrength = new Vector3(1f, 1f, 1f);
    public float QuarterViewShakeRotationFrequency = 10f;
    public float QuarterViewShakeRotationDuration = 0.2f;
    private Tweener _quarterViewShakeRotationTweener; // DOShakeRotation은 Tweener 반환

    [Header("FPS/TPS 흔들림 (FOV/Viewport Rect)")]
    public float FOVShakeIntensity = 2f;
    public float FOVShakeDuration = 0.15f;
    public float ViewportShakeIntensity = 0.02f;
    public float ViewportShakeDuration = 0.1f;
    // private Tweener _fovShakeTweener; // 이전 타입
    // private Tweener _viewportShakeTweener; // 이전 타입
    private Sequence _fovShakeTweener;       // 수정된 타입: Sequence
    private Sequence _viewportShakeTweener;  // 수정된 타입: Sequence

    private void Start()
    {
        FPS_State(); // 초기 상태 설정
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8)) // FPS
        {
            FPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9)) // TPS
        {
            TPS_State();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) // QuarterView (오타 감안)
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
    }

    private void TPS_State()
    {
        CameraView = CameraViewState.TPS;

        if (PlayerTransform != null && CameraTPSMove != null)
        {
            CameraTPSMove.enabled = true;
            CameraTPSMove.Target = PlayerTransform;

            // 초기 카메라 위치 계산: 플레이어의 회전 방향 뒤쪽으로 이동
            Quaternion playerRotation = PlayerTransform.rotation;
            Vector3 initialOffset = playerRotation * new Vector3(0f, InitialTPSCameraHeight, -InitialTPSToPlayerDistance);
            Camera.transform.position = PlayerTransform.position + initialOffset;

            // 초기 카메라 회전 설정: 플레이어를 바라보도록 설정
            Camera.transform.LookAt(PlayerTransform.position + Vector3.up * InitialTPSCameraHeight / 2f); // 약간 위쪽을 보도록 조정

            // CameraTPSMove의 초기 회전 값 동기화 (현재 카메라의 회전 값 사용)
            CameraTPSMove._rotationX = Camera.transform.eulerAngles.y - PlayerTransform.eulerAngles.y; // 플레이어 기준 상대 회전
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

            // 필수 컴포넌트가 없어도 다른 상태 전환은 가능하도록 기본적인 설정은 유지
            Camera.orthographic = false;
            if (QuerterView != null) QuerterView.enabled = false;
            if (CameraFollow != null) CameraFollow.enabled = false;
            if (CameraRotate != null) CameraRotate.enabled = false;
            if (CameraTPSMove != null) CameraTPSMove.enabled = true; // 일단 활성화는 시키지만 Target이 없어 제대로 동작 안 할 수 있음
        }
    }

    private void Querterview_State() // 참고: QuarterView 오타 가능성
    {
        CameraView = CameraViewState.QuerterView;

        if (QuerterView != null) QuerterView.enabled = true;
        if (CameraFollow != null) CameraFollow.enabled = false;
        if (CameraRotate != null) CameraRotate.enabled = false;
        if (CameraTPSMove != null) CameraTPSMove.enabled = false;
        // 참고: 필요하다면 여기서 Camera.orthographic = true; 설정
    }

    public void ShakeCamera()
    {
        switch (CameraView)
        {
            case CameraViewState.QuerterView:
                ShakeCameraRotation();
                break;
            case CameraViewState.FPS:
            case CameraViewState.TPS:
                // 둘 중 원하는 효과를 선택 또는 조합
                // ShakeCameraFOV();
                ShakeCameraViewport();
                break;
        }
    }

    public void ShakeCameraRotation(float durationOverride = -1f, Vector3 strengthOverride = default, float frequencyOverride = -1f)
    {
        if (CameraView != CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : QuarterViewShakeRotationDuration;
        Vector3 strength = (strengthOverride != default) ? strengthOverride : QuarterViewShakeRotationStrength;
        float frequency = (frequencyOverride >= 0) ? frequencyOverride : QuarterViewShakeRotationFrequency;

        if (_quarterViewShakeRotationTweener != null && _quarterViewShakeRotationTweener.IsActive())
        {
            _quarterViewShakeRotationTweener.Restart();
            // Vector3 곱셈 수정: Vector3.Scale 사용
            _quarterViewShakeRotationTweener.ChangeValues(Camera.transform.localEulerAngles, Camera.transform.localEulerAngles + Vector3.Scale(Random.insideUnitSphere, strength), duration);
        }
        else
        {
            _quarterViewShakeRotationTweener = Camera.transform.DOShakeRotation(duration, strength, (int)frequency)
                .SetAutoKill(true);
        }
    }

    public void ShakeCameraFOV(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (CameraView == CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : FOVShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : FOVShakeIntensity;
        float originalFOV = Camera.fieldOfView;
        float minFOV = originalFOV - intensity;
        float maxFOV = originalFOV + intensity;

        _fovShakeTweener?.Kill(); // 이전 트위너/시퀀스가 있다면 종료

        Sequence fovShakeSequence = DOTween.Sequence();
        fovShakeSequence.Append(Camera.DOFieldOfView(maxFOV, duration / 2f).SetEase(Ease.OutQuad));
        fovShakeSequence.Append(Camera.DOFieldOfView(minFOV, duration / 2f).SetEase(Ease.InQuad));
        fovShakeSequence.Append(Camera.DOFieldOfView(originalFOV, duration / 2f).SetEase(Ease.OutQuad));

        // _fovShakeTweener 타입이 Sequence로 변경되어 이제 할당 가능
        _fovShakeTweener = fovShakeSequence.Play().SetAutoKill(true);
    }

    public void ShakeCameraViewport(float durationOverride = -1f, float intensityOverride = -1f)
    {
        if (CameraView == CameraViewState.QuerterView) return;

        float duration = (durationOverride > 0) ? durationOverride : ViewportShakeDuration;
        float intensity = (intensityOverride >= 0) ? intensityOverride : ViewportShakeIntensity;
        Rect originalViewport = Camera.rect;
        Vector2 randomOffset() => Random.insideUnitCircle * intensity;

        _viewportShakeTweener?.Kill(); // 이전 트위너/시퀀스가 있다면 종료

        Sequence viewportShakeSequence = DOTween.Sequence();
        viewportShakeSequence.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.OutQuad));
        viewportShakeSequence.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        viewportShakeSequence.Append(Camera.DORect(new Rect(originalViewport.x + randomOffset().x, originalViewport.y + randomOffset().y, originalViewport.width, originalViewport.height), duration / 4f).SetEase(Ease.InOutQuad));
        viewportShakeSequence.Append(Camera.DORect(originalViewport, duration / 4f).SetEase(Ease.InQuad));

        // _viewportShakeTweener 타입이 Sequence로 변경되어 이제 할당 가능
        _viewportShakeTweener = viewportShakeSequence.Play().SetAutoKill(true);
    }
}