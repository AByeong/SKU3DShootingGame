using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public enum CameraViewState
    {
        FPS,
        TPS,
        QuerterView
    }

    public CameraQuerterView QuerterView;
    public CameraFollow CameraFollow;
    public CameraRotate CameraRotate;
    public CameraTPSMove CameraTPSMove;
    public Transform PlayerTransform; // 플레이어의 Transform 참조
    public Camera Camera;

    public CameraViewState CameraView = CameraViewState.FPS;
    public float InitialTPSToPlayerDistance = 3f; // 초기 TPS 카메라와 플레이어 간 거리
    public float InitialTPSCameraHeight = 1.5f; // 초기 TPS 카메라 높이

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

        if (Input.GetKeyDown(KeyCode.Alpha0)) // QuarterVie
        {
            Querterview_State();
        }
    }

    private void FPS_State()
    {
        CameraView = CameraViewState.FPS;

        Camera.orthographic = false;
        QuerterView.enabled = false;
        CameraFollow.enabled = true;
        CameraRotate.enabled = true;
        CameraTPSMove.enabled = false;
    }

    private void TPS_State()
    {
        CameraView = CameraViewState.TPS;

        if (PlayerTransform != null)
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
            QuerterView.enabled = false;
            CameraFollow.enabled = false;
            CameraRotate.enabled = false;
        }
        else
        {
            Debug.LogError("PlayerTransform이 CameraManager에 할당되지 않았습니다.");
            CameraTPSMove.enabled = true;
            Camera.orthographic = false;
            QuerterView.enabled = false;
            CameraFollow.enabled = false;
            CameraRotate.enabled = false;
        }
    }

    private void Querterview_State()
    {
        CameraView = CameraViewState.QuerterView;

        QuerterView.enabled = true;
        CameraFollow.enabled = false;
        CameraRotate.enabled = false;
        CameraTPSMove.enabled = false;
    }
}