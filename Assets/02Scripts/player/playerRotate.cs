using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [Tooltip("회전 속도")]
    public float RotationSpeed = 150f; // 회전 속도 (쿼터뷰에서는 즉각적인 회전을 위해 더 크게 설정할 수도 있습니다)
    public CameraManager CameraManager; // 현재 카메라 시점 정보를 얻기 위한 CameraManager 참조
    public Camera MainCamera; // 메인 카메라 참조 (쿼터뷰에서 마우스 위치를 월드 좌표로 변환하는 데 사용)

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    public bool GameStarted = false;

    public void ChangeStarting()
    {
        Debug.Log("PlayerRotate Triggered");
        GameStarted = !GameStarted;
    }
    private void Start()
    {
    }

    private void Update()
    {
        if (GameStarted == true)
        {
            // 현재 활성화된 카메라 시점에 따라 회전 로직 분기 처리
            switch (CameraManager.CameraView)
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

    private void HandleFPSRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _rotationX += mouseX * RotationSpeed * Time.deltaTime;
        _rotationY -= mouseY * RotationSpeed * Time.deltaTime;
        _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);

        // FPS에서는 주로 카메라를 회전시키므로, 이 스크립트가 부착된 오브젝트(플레이어)의 회전은 Y축만 적용합니다.
        transform.eulerAngles = new Vector3(0f, _rotationX, 0f);

        // 필요하다면 카메라(자식 오브젝트)의 회전은 별도의 CameraRotate 스크립트에서 처리하도록 유지합니다.
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