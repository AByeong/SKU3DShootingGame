using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [Tooltip("회전 속도")]
    public float RotationSpeed = 150f; // 회전 속도 (쿼터뷰에서는 즉각적인 회전을 위해 더 크게 설정할 수도 있습니다)
    public CameraManager CameraManager; // 현재 카메라 시점 정보를 얻기 위한 CameraManager 참조
    public Camera MainCamera; // 메인 카메라 참조 (쿼터뷰에서 마우스 위치를 월드 좌표로 변환하는 데 사용)

    private float _rotationX = 0f;
    private float _rotationY = 0f;
   
    
    
    private void Start()
    {
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Play )
        {
            // 현재 활성화된 카메라 시점에 따라 회전 로직 분기 처리
            switch (CameraManager.CurrentCameraView)
            {
                case CameraManager.CameraViewState.FPS:
                    HandleFPSRotation();
                    break;
                case CameraManager.CameraViewState.TPS:
                    HandleTPSRotation();
                    break;
                case CameraManager.CameraViewState.QuerterView:
                    HandleQuarterViewRotation();
                    break;
                default:
                    Debug.LogWarning("알 수 없는 카메라 시점입니다.");
                    break;
            }
        }
    }

    public void ShakeCamera()
    {

    }
    
    

    private void HandleFPSRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _rotationX += mouseX * RotationSpeed * Time.deltaTime;
        _rotationY -= mouseY * RotationSpeed * Time.deltaTime;
        _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);

        // 카메라 회전
        MainCamera.transform.localEulerAngles = new Vector3(_rotationY, _rotationX, 0f);

        // 플레이어(오브젝트)는 카메라의 y축(yaw)만 따라가게 한다
        Vector3 playerRotation = new Vector3(0f, MainCamera.transform.eulerAngles.y, 0f);
        transform.eulerAngles = playerRotation;
    }


    private void HandleTPSRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _rotationX += mouseX * RotationSpeed * Time.deltaTime;
       
        transform.eulerAngles = new Vector3(0f, _rotationX, 0f);
    }

    private void HandleQuarterViewRotation()
    {
        // 쿼터뷰에서는 마우스 커서의 월드 좌표를 기준으로 플레이어를 회전시킵니다.
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position); // 플레이어 위치의 수평면
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 hitPoint = ray.GetPoint(rayDistance);
            // 플레이어의 현재 위치에서 마우스 클릭 지점까지의 방향 벡터를 계산합니다.
            Vector3 lookDirection = hitPoint - transform.position;
            lookDirection.y = 0f; // Y축 방향은 회전에 영향을 주지 않도록 0으로 설정합니다.

            // 방향 벡터가 0이 아닌 경우에만 회전을 수행합니다.
            if (lookDirection != Vector3.zero)
            {
                // 목표 회전 Quaternion을 생성합니다.
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                // 현재 회전에서 목표 회전으로 부드럽게 회전시킵니다.
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * RotationSpeed * 10f); // 회전 속도를 좀 더 빠르게 조정
            }
        }
    }
}