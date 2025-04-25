using UnityEngine;

public class CameraTPSMove : MonoBehaviour
{
    public Vector3 _offset;
    public float _rotationSpeed = 100f; // 회전 속도 기본값 설정
    [HideInInspector] public float _rotationX; // Inspector에서 숨김
    [HideInInspector] public float _rotationY; // Inspector에서 숨김

    public Camera Camera;
    [HideInInspector] public Transform Target; // Inspector에서 숨김

    private void Start()
    {
        // 초기화 시 Target이 설정되어 있지 않다면 에러 메시지 출력 (안전 장치)
        if (Target == null)
        {
            Debug.LogError("CameraTPSMove 스크립트에 Target이 할당되지 않았습니다.");
            enabled = false; // 스크립트 비활성화
        }
    }

    void LateUpdate() 
    {
        if (Target != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * _rotationSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * _rotationSpeed * Time.deltaTime;

            _rotationX += mouseX;
            _rotationY = Mathf.Clamp(_rotationY - mouseY, -40f, 40f);

            // 플레이어의 Y축 회전 값을 기준으로 초기 X축 회전 동기화
            Quaternion targetRotation = Quaternion.Euler(_rotationY, _rotationX + Target.eulerAngles.y, 0);
            Vector3 finalOffset = targetRotation * _offset;

            Camera.transform.position = Target.position + finalOffset;
            Camera.transform.LookAt(Target.position + Vector3.up * _offset.y);
        }
    }
}