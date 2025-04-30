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

    private Vector3 _rotationAxis;
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
        // If SetInitialMovement was not called before Start (e.g., placed manually in scene)
        // Use the old random logic as a fallback.
        if (!_isBezierMoving)
        {
            _startPos = transform.position;

            _rotationAxis = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f), // Slightly favor upward axis for rotation
                Random.Range(-1f, 1f)
            ).normalized;
            _rotationSpeed = Random.Range(180f, 720f);


            Vector3 randomOffsetXZ = new Vector3(
                Random.Range(-SpreadRadius, SpreadRadius),
                0f, // Only horizontal spread for end position offset
                Random.Range(-SpreadRadius, SpreadRadius)
            );
            _endPos = new Vector3(_startPos.x + randomOffsetXZ.x, 0f, _startPos.z + randomOffsetXZ.z); // Assume ground at Y=0

            // Control point is mid-way horizontally, with added jump height
            _controlPos = _startPos + randomOffsetXZ * 0.5f + Vector3.up * JumpHeight;


            _isBezierMoving = true; // Start the movement if initialized here
        }
        // If SetInitialMovement *was* called, _isBezierMoving is already true and positions are set.
        // Rotation initialization is now also handled in SetInitialMovement.

    }

    // New method for the Enemy to set up the coin's initial movement
    public void SetInitialMovement(Vector3 startPos, Vector3 landingOffsetHorizontal, float baseJumpHeight)
    {
        _startPos = startPos;

        // Calculate the landing position. Assume ground is at Y=0 for simplicity.
        // The horizontal offset is provided by the enemy.
        _endPos = new Vector3(_startPos.x + landingOffsetHorizontal.x, 0f, _startPos.z + landingOffsetHorizontal.z);

        // Calculate the control point for the Bezier curve with random height variation
        float randomHeightOffset = Random.Range(-0.3f, 0.3f); // 작은 랜덤 높이 오프셋
        Vector3 horizontalMidPoint = _startPos + landingOffsetHorizontal * 0.5f;
        _controlPos = horizontalMidPoint + Vector3.up * (baseJumpHeight + randomHeightOffset);

        // Reset bezier time and start the movement
        _bezierTime = 0f;
        _isBezierMoving = true;
        _hasLanded = false; // Ensure hasLanded is false until bezier movement finishes

        // Initialize rotation with random variations
        _rotationAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
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

                // Stop rotating or transition to flat rotation upon landing
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); // Keep Y rotation, flatten X/Z
            }

            if (_isBezierMoving) // Only rotate while moving on the bezier curve
            {
                transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
                transform.position = CalculateQuadraticBezierPoint(_bezierTime, _startPos, _controlPos, _endPos);
            }

            // Do not check for attraction or flatten rotation while still moving on bezier
            return;
        }

        // After landing
        if (_hasLanded && !_isAttracting)
        {
            // Check for attraction range *only if player exists*
            if (CoinInTransfrom != null)
            {
                float distance = Vector3.Distance(transform.position, CoinInTransfrom.position);
                if (distance < AttractRange)
                {
                    _isAttracting = true;
                    // Stop previous rotation completely or smoothly transition out
                }
            }

            // Keep the coin upright after landing if not attracting
            Quaternion flatRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, flatRotation, Time.deltaTime * 5f);
        }

        // Attraction movement
        if (_isAttracting)
        {
            // Move towards player *only if player exists*
            if (CoinInTransfrom != null)
            {
                Vector3 dir = (CoinInTransfrom.position - transform.position).normalized;
                transform.position += dir * AttractSpeed * Time.deltaTime;
                // Optional: Make coin spin while attracting
                transform.Rotate(Vector3.up, 360 * Time.deltaTime); // Spin around Y axis
            }
            else
            {
                // If player disappears while attracting, stop attracting
                _isAttracting = false;
            }
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // p(t) = (1-t)^2 * P0 + 2*(1-t)*t*P1 + t^2 * P2
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2*(1-t)*t*P1
        p += tt * p2;       // t^2 * P2
        return p;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 기존의 OnTrggerEnter는 그대로 유지 (혹시 모를 충돌 기반 획득)
        if (CoinInTransfrom != null && other.gameObject == CoinInTransfrom.gameObject)
        {
            CoinInPlayer();
        }
    }

    private void CoinInPlayer()
    {
        // Add coin to player's inventory/score *only if PlayerCore exists*
        if (PlayerCore != null)
        {
            Debug.Log("코인 획득");
            PlayerCore.GetCoin();
            // PlayerCore.AddCoin(1); // Assuming PlayerCore has an AddCoin method
        }
        else
        {
            Debug.LogWarning("Coin collected but PlayerCore reference is missing!", this);
        }

        // Destroy or pool the coin
        Destroy(gameObject); // Or return to pool
    }

    // Optional: Draw gizmo for attraction range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, AttractRange);

        // 추가: 자동 획득 거리 기즈모
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, CollectionDistance);
    }

    /// <summary>
    /// 지정된 위치와 반경 내에 원하는 개수의 코인을 골고루 뿌립니다.
    /// </summary>
    /// <param name="spawnPosition">코인을 뿌릴 중심 위치입니다.</param>
    /// <param name="spawnRadius">코인을 뿌릴 반경입니다.</param>
    /// <param name="coinCount">생성할 코인의 개수입니다.</param>
    public static void SpawnCoinsInArea(Vector3 spawnPosition, float spawnRadius, int coinCount, GameObject coinPrefab, float baseJumpHeight = 1.5f, float bezierDuration = 0.5f)
    {
        for (int i = 0; i < coinCount; i++)
        {
            // 원형 범위 내의 랜덤한 위치 계산 (균등 분포를 위해)
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPosition = new Vector3(spawnPosition.x + randomCircle.x, spawnPosition.y, spawnPosition.z + randomCircle.y);

            // 코인 프리팹이 설정되었는지 확인
            if (coinPrefab != null)
            {
                // 코인 오브젝트 생성
                GameObject spawnedCoin = Instantiate(coinPrefab, randomPosition, Quaternion.identity);

                // Coin 스크립트가 있는지 확인하고 초기 움직임 설정
                Coin coinComponent = spawnedCoin.GetComponent<Coin>();
                if (coinComponent != null)
                {
                    // 코인이 생성될 시작 위치는 생성된 랜덤 위치
                    Vector3 startPos = spawnedCoin.transform.position;
                    // 착지할 최종 위치는 Y=0 (지면)
                    Vector3 endPosHorizontalOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);

                    coinComponent.BezierDuration = bezierDuration;
                    coinComponent.JumpHeight = baseJumpHeight; // SpawnCoinsInArea에서도 baseJumpHeight 사용
                    coinComponent.SetInitialMovement(startPos, endPosHorizontalOffset, baseJumpHeight);
                }
                else
                {
                    Debug.LogError("생성된 코인 오브젝트에 Coin 스크립트가 없습니다!", spawnedCoin);
                }
            }
            else
            {
                Debug.LogError("Coin 프리팹이 설정되지 않았습니다!");
                return;
            }
        }
    }
}