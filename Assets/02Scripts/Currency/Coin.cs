using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Coin : MonoBehaviour
{
    [Header("Bezier Motion")]
    public float BezierDuration = 0.5f;
    public float JumpHeight = 1.5f;
    [Tooltip("코인이 착지할 수 있는 최대 수평 거리 (Enemy가 퍼뜨릴 때 이 값을 사용)")]
    public float SpreadRadius = 1f; // Renamed/Clarified for use by Enemy

    [Header("Attraction")]
    public float AttractRange = 3f;
    public float AttractSpeed = 6f;

    [Header("Collection")]
    [Tooltip("CoinInTransform과의 거리가 이 값 이하가 되면 자동으로 획득합니다.")]
    public float CollectionDistance = 0.5f; // 기본 획득 거리

    [FormerlySerializedAs("PlayerTransform")]
    [Header("References")]
    public Transform CoinInTransfrom;
    public PlayerCore PlayerCore; // Reference to PlayerCore for adding coins

    private Vector3 _startPos;
    private Vector3 _controlPos;
    private Vector3 _endPos;

    private bool _isBezierMoving = false; // Start as false, set to true by SetInitialMovement
    private float _bezierTime = 0f;

    private bool _isAttracting = false;
    private bool _hasLanded = false;

    // 제거: 초기 회전은 Start 또는 SetInitialMovement에서 설정
    // private Vector3 _rotationAxis = Vector3.up;
    private float _rotationSpeed;

    private void Awake()
    {
        // Find CoinInTransfrom and PlayerCore if not assigned in Inspector
        if (CoinInTransfrom == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CoinInTransfrom = player.transform;
                PlayerCore = player.GetComponent<PlayerCore>(); // Get PlayerCore here too
                if (PlayerCore == null)
                {
                    Debug.LogError("Player object found but does not have PlayerCore component!", player);
                }
            }
            else
            {
                Debug.LogWarning("Player object with tag 'Player' not found in Awake for Coin.", this);
            }
        }
        else // If CoinInTransfrom is assigned, try to get PlayerCore from it
        {
            if (PlayerCore == null)
            {
                PlayerCore = CoinInTransfrom.GetComponent<PlayerCore>();
                if (PlayerCore == null)
                {
                    Debug.LogError("PlayerTransform assigned but does not have PlayerCore component!", CoinInTransfrom.gameObject);
                }
            }
        }
    }

    private void Start()
    {
        // --- 시작 시 코인 세우기 ---
        // 프리팹이 이미 서있는 상태라면 이 줄은 주석 처리하거나 삭제하세요.
        transform.Rotate(90f, 0f, 0f, Space.Self); // 로컬 X축 기준 90도 회전 (필요에 따라 축 변경)
        // ------------------------

        // If SetInitialMovement was not called before Start (e.g., placed manually in scene)
        // Use the old random logic as a fallback.
        if (!_isBezierMoving)
        {
            _startPos = transform.position; // 회전 후의 시작 위치 사용
            _rotationSpeed = Random.Range(180f, 720f);

            Vector3 randomOffsetXZ = new Vector3(
                Random.Range(-SpreadRadius, SpreadRadius),
                0f, // Only horizontal spread for end position offset
                Random.Range(-SpreadRadius, SpreadRadius)
            );
            // 끝 위치 계산 시 Y=0 가정 대신, 시작 높이를 유지하도록 변경하거나 지형 감지 필요
            // 여기서는 간단히 시작 높이를 유지하도록 수정 (더 정확하려면 Raycast 필요)
            _endPos = new Vector3(_startPos.x + randomOffsetXZ.x, _startPos.y, _startPos.z + randomOffsetXZ.z);

            // Control point is mid-way horizontally, with added jump height
            _controlPos = _startPos + randomOffsetXZ * 0.5f + Vector3.up * JumpHeight;

            _isBezierMoving = true; // Start the movement if initialized here
        }
    }

    // New method for the Enemy to set up the coin's initial movement
    public void SetInitialMovement(Vector3 startPos, Vector3 landingOffsetHorizontal, float baseJumpHeight)
    {
        transform.position = startPos; // 위치 먼저 설정

        

        _startPos = transform.position; // 회전이 적용된 후의 위치

        // Calculate the landing position. Assume ground is at Y=0 for simplicity, or maintain start Y
        // 여기서는 시작 높이를 유지하도록 수정
        _endPos = new Vector3(_startPos.x + landingOffsetHorizontal.x, _startPos.y, _startPos.z + landingOffsetHorizontal.z);

        // Calculate the control point for the Bezier curve with random height variation
        float randomHeightOffset = Random.Range(-0.3f, 0.3f); // 작은 랜덤 높이 오프셋
        Vector3 horizontalMidPoint = _startPos + landingOffsetHorizontal * 0.5f;
        _controlPos = horizontalMidPoint + Vector3.up * (baseJumpHeight + randomHeightOffset);

        // Reset bezier time and start the movement
        _bezierTime = 0f;
        _isBezierMoving = true;
        _hasLanded = false; // Ensure hasLanded is false until bezier movement finishes

        // Initialize rotation
        _rotationSpeed = Random.Range(180f, 720f); // 기본 속도 범위 유지

        // Ensure attraction is off initially
        _isAttracting = false;
    }

    private void Update()
    {
        // Check for collection distance regardless of movement state
        if (CoinInTransfrom != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, CoinInTransfrom.position);
            if (distanceToPlayer <= CollectionDistance)
            {
                CoinInPlayer();
                return; // 이미 획득했으므로 더 이상 업데이트할 필요 없음
            }
        }

        if (_isBezierMoving)
        {
            _bezierTime += Time.deltaTime / BezierDuration;
            if (_bezierTime >= 1f)
            {
                _bezierTime = 1f;
                _isBezierMoving = false;
                _hasLanded = true; // Coin has landed

                // Snap to the final position to avoid slight offset
                transform.position = _endPos;
            }

            // 베지어 이동 중 위치 업데이트
            transform.position = CalculateQuadraticBezierPoint(_bezierTime, _startPos, _controlPos, _endPos);

            // 베지어 이동 중에도 월드 Y축 기준으로 회전 (이 부분은 문제 없음)
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            return; // 베지어 이동 중에는 아래 로직 실행 안 함
        }

        // After landing
        if (_hasLanded)
        {
            // 착지 후에도 월드 Y축을 기준으로 회전 (이 부분은 문제 없음)
            transform.Rotate(Vector3.up, _rotationSpeed * Time.unscaledDeltaTime, Space.World);

            // Check for attraction range *only if player exists*
            if (!_isAttracting && CoinInTransfrom != null)
            {
                float distance = Vector3.Distance(transform.position, CoinInTransfrom.position);
                if (distance < AttractRange)
                {
                    _isAttracting = true;
                }
            }
        }

        // Attraction movement
        if (_isAttracting)
        {
            // Move towards player *only if player exists*
            if (CoinInTransfrom != null)
            {
                Vector3 dir = (CoinInTransfrom.position - transform.position).normalized;
                transform.position += dir * AttractSpeed * Time.deltaTime;
                // 끌려갈 때도 월드 Y축 기준으로 계속 회전 (이 부분은 문제 없음)
                transform.Rotate(Vector3.up, _rotationSpeed * Time.unscaledDeltaTime, Space.World);
            }
            else
            {
                _isAttracting = false;
            }
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CoinInTransfrom != null && other.gameObject == CoinInTransfrom.gameObject)
        {
            CoinInPlayer();
        }
    }

    private void CoinInPlayer()
    {
        if (PlayerCore != null)
        {
            Debug.Log("코인 획득");
            PlayerCore.GetCoin();
        }
        else
        {
            Debug.LogWarning("Coin collected but PlayerCore reference is missing!", this);
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, AttractRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, CollectionDistance);
    }

    // SpawnCoinsInArea 함수는 수정할 필요 없음 (Instantiate 시 프리팹의 기본 회전을 사용하거나,
    // 생성된 코인의 Start/SetInitialMovement에서 회전이 적용됨)
    public static void SpawnCoinsInArea(Vector3 spawnPosition, float spawnRadius, int coinCount, GameObject coinPrefab, float baseJumpHeight = 1.5f, float bezierDuration = 0.5f)
    {
        for (int i = 0; i < coinCount; i++)
        {
            // 코인 생성 위치 계산 (Y값은 spawnPosition 그대로 사용)
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomSpawnPos = new Vector3(spawnPosition.x + randomCircle.x, spawnPosition.y, spawnPosition.z + randomCircle.y);

            // 착지 위치 계산을 위한 수평 오프셋 (랜덤하게 생성)
            // 여기서는 간단히 스폰 위치 주변 랜덤한 곳으로 설정
            Vector2 landingOffsetCircle = Random.insideUnitCircle * spawnRadius; // 착지 위치를 위한 또 다른 랜덤 값
            Vector3 landingOffsetHorizontal = new Vector3(landingOffsetCircle.x, 0f, landingOffsetCircle.y);


            if (coinPrefab != null)
            {
                // Quaternion.identity는 프리팹의 기본 회전을 사용합니다.
                GameObject spawnedCoin = Instantiate(coinPrefab, randomSpawnPos, Quaternion.identity);
                Coin coinComponent = spawnedCoin.GetComponent<Coin>();
                if (coinComponent != null)
                {
                    // Bezier 및 점프 높이 설정
                    coinComponent.BezierDuration = bezierDuration;
                    // JumpHeight는 SetInitialMovement에서 baseJumpHeight를 사용하므로 여기서는 직접 설정 안 함

                    // SetInitialMovement 호출하여 움직임 시작 (내부에서 초기 회전도 처리)
                    coinComponent.SetInitialMovement(randomSpawnPos, landingOffsetHorizontal, baseJumpHeight);
                }
                else
                {
                    Debug.LogError("생성된 코인 오브젝트에 Coin 스크립트가 없습니다!", spawnedCoin);
                }
            }
            else
            {
                Debug.LogError("Coin 프리팹이 설정되지 않았습니다!");
                return; // 프리팹 없으면 종료
            }
        }
    }
}