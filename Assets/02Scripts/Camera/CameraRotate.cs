using UnityEngine; // UnityEngine 네임스페이스 사용

public class CameraRotate : MonoBehaviour
{
    // 카메라 회전 스크립트
    // 목표 : 마우스를 조작하면 카메라(이 스크립트가 부착된 오브젝트)를 그 방향으로 회전시키고 싶다. (주로 FPS 시점용)

    [Tooltip("카메라 회전 속도")] // Inspector에 설명을 추가하는 Tooltip 속성
    public float RotationSpeed = 150f; // 카메라 회전 속도 (기존 15f는 매우 느릴 수 있어 150f 정도로 조정하는 것을 제안합니다)

    [Tooltip("플레이어의 Transform 참조 (카메라의 초기 수평 각도 설정용)")] // Inspector에 설명을 추가하는 Tooltip 속성
    public Transform PlayerTransform; // 플레이어 Transform 참조 (Inspector 창에서 할당해야 합니다)

    // 누적될 회전 각도 (X축 회전값, Y축 회전값)
    // _rotationX는 수평 회전(Y축 기준 회전), _rotationY는 수직 회전(X축 기준 회전)을 저장합니다.
    private float _rotationX = 0;
    private float _rotationY = 0;

    // 스크립트 컴포넌트가 활성화될 때마다 호출되는 함수입니다.
    // 게임 오브젝트가 처음 활성화될 때, 그리고 비활성화되었다가 다시 활성화될 때 호출됩니다.
    private void OnEnable()
    {
        // 카메라 회전 상태를 초기화하는 함수를 호출합니다.
        // 이렇게 하면 FPS 모드로 전환될 때마다 카메라 각도가 플레이어 기준으로 재설정됩니다.
        InitializeRotation();
    }

    // 카메라 회전 상태를 초기화하는 함수
    public void InitializeRotation()
    {
        // 플레이어 Transform 참조가 Inspector에서 할당되었는지 확인합니다.
        if (PlayerTransform == null)
        {
             // 할당되지 않았다면 경고 메시지를 출력하고,
             // 카메라의 현재 각도를 기준으로 내부 변수를 초기화하거나 기본값을 사용합니다.
             Debug.LogWarning("CameraRotate 스크립트에 PlayerTransform이 할당되지 않았습니다. 카메라의 현재 각도 또는 기본값으로 초기화합니다.");

             // 현재 게임 오브젝트(카메라)의 월드 오일러 각도를 사용하여 내부 회전 변수를 초기화합니다.
             _rotationX = transform.eulerAngles.y; // 현재 카메라의 수평 각도
             _rotationY = transform.eulerAngles.x; // 현재 카메라의 수직 각도

             // 수직 각도가 비정상적인 값(예: 90도 이상이면서 270도 미만)일 경우 보정합니다. (선택 사항)
             // 오일러 각 변환 시 예상치 못한 값이 들어갈 수 있으므로 안전 장치 역할.
             if (_rotationY > 90f && _rotationY < 270f)
             {
                // 예를 들어 위를 넘어 뒤집힌 각도라면 0(수평)으로 강제 설정
                _rotationY = 0f;
             }
             return; // 초기화 종료
        }

        // 플레이어 Transform 참조가 있다면,
        // 플레이어의 현재 Y축 월드 회전값(좌우 방향)을 카메라의 초기 수평 회전(_rotationX)으로 설정합니다.
        // 이렇게 하면 FPS 모드 시작 시 카메라가 플레이어가 바라보는 방향과 같게 시작됩니다.
        _rotationX = PlayerTransform.eulerAngles.y;

        // 수직 회전(_rotationY)은 0(수평) 또는 원하는 다른 기본 각도로 초기화합니다.
        _rotationY = 0f; // 정면(수평)을 보도록 초기화

        // 계산된 초기 회전값을 즉시 이 스크립트가 붙어있는 게임 오브젝트(카메라)의 Transform에 적용합니다.
        // 오일러 각(Euler Angles)을 사용하여 X축 기준 수직 회전, Y축 기준 수평 회전을 적용합니다. Z축은 0으로 고정합니다.
        transform.eulerAngles = new Vector3(_rotationY, _rotationX, 0f);
    }


    // 매 프레임 호출되는 함수입니다.
    private void Update()
    {
        // --- 마우스 입력 처리 및 회전 계산 ---

        // 1. 마우스의 X축, Y축 이동 값을 입력 시스템으로부터 받습니다.
        //    Input.GetAxis는 부드러운 입력 값을 반환합니다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 디버깅을 위해 마우스 입력 값을 콘솔에 출력합니다. (필요 시 주석 해제)
        // Debug.Log($"Mouse X: {mouseX}, Mouse Y: {mouseY}");

        // 2. 마우스 입력 값, 설정된 회전 속도(RotationSpeed), 그리고 프레임 시간(Time.deltaTime)을 곱하여
        //    이번 프레임에서의 회전량을 계산하고, 이전 프레임까지 누적된 회전 각도(_rotationX, _rotationY)에 더합니다.
        //    Time.deltaTime을 곱하는 것은 프레임 속도(FPS)에 관계없이 일정한 속도로 회전하도록 보장합니다.
        _rotationX += mouseX * RotationSpeed * Time.deltaTime;
        _rotationY -= mouseY * RotationSpeed * Time.deltaTime; // 마우스 Y축 이동은 일반적으로 반대로 적용해야 위아래 시점 조작이 자연스럽습니다.

        // 3. 수직 회전 각도(_rotationY)의 범위를 제한합니다. (예: -90도 ~ 90도)
        //    Mathf.Clamp 함수를 사용하여 값이 지정된 최소값과 최대값 사이에 있도록 합니다.
        //    이를 통해 카메라가 땅을 뚫고 보거나 하늘 위로 완전히 뒤집히는 것을 방지합니다.
        _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);


        // 4. 최종적으로 계산된 누적 회전 각도를 사용하여 이 스크립트가 부착된 게임 오브젝트(카메라)의 Transform을 회전시킵니다.
        //    transform.eulerAngles 속성에 새로운 Vector3 값을 할당하여 오일러 각으로 회전을 설정합니다.
        //    X에는 수직 회전값(_rotationY), Y에는 수평 회전값(_rotationX), Z에는 0을 사용합니다.
        transform.eulerAngles = new Vector3(_rotationY, _rotationX, 0f);
    }
}