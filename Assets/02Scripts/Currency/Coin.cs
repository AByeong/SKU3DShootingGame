using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Coin : MonoBehaviour
{
    [Header("Initial Pop-up Motion")]
    public float BezierDuration = 0.5f;
    public float JumpHeight = 1.5f; // Height for the initial jump
    [Tooltip("코인이 착지할 수 있는 최대 수평 거리 (Enemy가 퍼뜨릴 때 이 값을 사용)")]
    public float SpreadRadius = 1f;

    [Header("Attraction Behavior")]
    public float AttractRange = 3f; // Distance to start attracting
    public float AttractBezierDuration = 0.4f; // Duration of the attraction curve
    public float AttractJumpHeight = 0.8f; // How high the attraction curve arcs

    [Header("Collection")]
    [Tooltip("CoinInTransform과의 거리가 이 값 이하가 되면 자동으로 획득합니다.")]
    public float CollectionDistance = 0.5f;

    [FormerlySerializedAs("PlayerTransform")]
    [Header("References")]
    public Transform CoinInTransfrom; // Keeping user's spelling
    public PlayerCore PlayerCore;

    // Initial Bezier state
    private Vector3 _startPos;
    private Vector3 _controlPos;
    private Vector3 _endPos;
    private bool _isBezierMoving = false;
    private float _bezierTime = 0f;

    // Attraction Bezier state
    private bool _isBezierAttracting = false;
    private float _attractBezierTime = 0f;
    private Vector3 _attractStartPos;
    private Vector3 _attractControlPos; // Calculated dynamically
    private Vector3 _attractEndPos;   // Calculated dynamically (player's position)

    private bool _hasLanded = false;
    private float _rotationSpeed;

    private void Awake()
    {
        if (CoinInTransfrom == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CoinInTransfrom = player.transform;
                if (PlayerCore == null) PlayerCore = player.GetComponent<PlayerCore>();
                if (PlayerCore == null) Debug.LogError("Player object found but does not have PlayerCore component!", player);
            }
            else Debug.LogWarning("Player object with tag 'Player' not found in Awake for Coin.", this);
        }
        else if (PlayerCore == null)
        {
            PlayerCore = CoinInTransfrom.GetComponent<PlayerCore>();
            if (PlayerCore == null) Debug.LogError("PlayerTransform assigned but does not have PlayerCore component!", CoinInTransfrom.gameObject);
        }
    }

    private void Start()
    {
        // Optional: Stand the coin up if the prefab is flat.
        // transform.Rotate(90f, 0f, 0f, Space.Self); // Rotate around its local X-axis

        if (!_isBezierMoving) // Fallback if SetInitialMovement wasn't called
        {
            _startPos = transform.position;
            _rotationSpeed = Random.Range(180f, 720f);
            Vector3 randomOffsetXZ = new Vector3(Random.Range(-SpreadRadius, SpreadRadius), 0f, Random.Range(-SpreadRadius, SpreadRadius));
            _endPos = new Vector3(_startPos.x + randomOffsetXZ.x, _startPos.y, _startPos.z + randomOffsetXZ.z); // Maintain Y
            _controlPos = _startPos + randomOffsetXZ * 0.5f + Vector3.up * JumpHeight;
            _isBezierMoving = true;
        }
    }

    public void SetInitialMovement(Vector3 startPos, Vector3 landingOffsetHorizontal, float baseJumpHeight)
    {
        transform.position = startPos;
        // If your prefab needs to be rotated to stand up, do it here or in Start()
        // Example: transform.rotation = Quaternion.Euler(90f, Random.Range(0,360), 0f);
        // For now, assuming prefab rotation is correct or handled by Start() if needed.

        _startPos = transform.position;
        _endPos = new Vector3(_startPos.x + landingOffsetHorizontal.x, _startPos.y, _startPos.z + landingOffsetHorizontal.z); // Maintain Y

        float randomHeightOffset = Random.Range(-0.3f, 0.3f);
        Vector3 horizontalMidPoint = _startPos + landingOffsetHorizontal * 0.5f;
        _controlPos = horizontalMidPoint + Vector3.up * (baseJumpHeight + randomHeightOffset);

        _bezierTime = 0f;
        _isBezierMoving = true;
        _hasLanded = false;
        _isBezierAttracting = false; // Ensure not attracting initially
        _rotationSpeed = Random.Range(180f, 720f);
    }

    private void Update()
    {
        // 1. Immediate Collection Check (Failsafe)
        if (CoinInTransfrom != null)
        {
            if (Vector3.Distance(transform.position, CoinInTransfrom.position) <= CollectionDistance)
            {
                CoinInPlayer();
                return; // Collected, no further updates.
            }
        }

        // 2. Initial Pop-up Bezier Movement
        if (_isBezierMoving)
        {
            _bezierTime += Time.deltaTime / BezierDuration;
            if (_bezierTime >= 1f)
            {
                _bezierTime = 1f;
                _isBezierMoving = false;
                _hasLanded = true;
                transform.position = _endPos; // Snap to final landing position
            }
            else
            {
                transform.position = CalculateQuadraticBezierPoint(_bezierTime, _startPos, _controlPos, _endPos);
            }
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
            return; // Don't process other movement logic during initial pop-up
        }

        // 3. Bezier Attraction Movement (Towards Player)
        if (_isBezierAttracting)
        {
            if (CoinInTransfrom != null)
            {
                _attractBezierTime += Time.deltaTime / AttractBezierDuration;

                // Dynamically update end point to player's current position for homing effect
                _attractEndPos = CoinInTransfrom.position;

                // Recalculate control point to arc from the original attraction start (_attractStartPos)
                // towards the current player position (_attractEndPos)
                Vector3 midPoint = (_attractStartPos + _attractEndPos) / 2f;
                _attractControlPos = midPoint + Vector3.up * AttractJumpHeight;

                if (_attractBezierTime >= 1f)
                {
                    _attractBezierTime = 1f; // Clamp time
                    // Bezier finished, ideally very close to player. The collection check above will handle it.
                    // Or, force snap if desired: transform.position = _attractEndPos;
                    _isBezierAttracting = false; // Stop Bezier attracting
                }

                transform.position = CalculateQuadraticBezierPoint(
                    _attractBezierTime,
                    _attractStartPos,
                    _attractControlPos,
                    _attractEndPos
                );
                transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
            }
            else // Player disappeared
            {
                _isBezierAttracting = false;
            }
            return; // Don't process other movement logic during Bezier attraction
        }

        // 4. Logic for when coin has landed and is NOT doing any Bezier movement
        if (_hasLanded) // This implies !_isBezierMoving and !_isBezierAttracting
        {
            // Keep rotating on the ground
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            // Check if we should START Bezier attraction
            if (CoinInTransfrom != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, CoinInTransfrom.position);
                if (distanceToPlayer < AttractRange)
                {
                    _isBezierAttracting = true; // Start Bezier attraction on the next frame
                    _attractBezierTime = 0f;
                    _attractStartPos = transform.position; // Attraction starts from current position
                    // _attractEndPos and _attractControlPos will be set dynamically inside the _isBezierAttracting block
                }
            }
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        t = Mathf.Clamp01(t); // Ensure t is between 0 and 1
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    private void OnTriggerEnter(Collider other) // This is an alternative way to collect
    {
        if (CoinInTransfrom != null && other.transform == CoinInTransfrom) // Check transform directly
        {
            CoinInPlayer();
        }
    }

    private void CoinInPlayer()
    {
        if (PlayerCore != null)
        {
            // Debug.Log("코인 획득 (CoinInPlayer)"); // Kept for debugging if needed
            PlayerCore.GetCoin();
        }
        else Debug.LogWarning("Coin collected but PlayerCore reference is missing!", this);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Attraction Range
        Gizmos.DrawWireSphere(transform.position, AttractRange);
        Gizmos.color = Color.green; // Collection Distance
        Gizmos.DrawWireSphere(transform.position, CollectionDistance);

        // For debugging the attraction Bezier if it's active
        if (_isBezierAttracting && CoinInTransfrom != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 tempEndPos = CoinInTransfrom.position;
            Vector3 tempMidPoint = (_attractStartPos + tempEndPos) / 2f;
            Vector3 tempControlPos = tempMidPoint + Vector3.up * AttractJumpHeight;

            Vector3 prevPoint = _attractStartPos;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 nextPoint = CalculateQuadraticBezierPoint(t, _attractStartPos, tempControlPos, tempEndPos);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }

    public static void SpawnCoinsInArea(Vector3 spawnPosition, float spawnRadius, int coinCount, GameObject coinPrefab, float baseJumpHeight = 1.5f, float bezierDuration = 0.5f)
    {
        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomSpawnPos = new Vector3(spawnPosition.x + randomCircle.x, spawnPosition.y, spawnPosition.z + randomCircle.y);
            Vector2 landingOffsetCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 landingOffsetHorizontal = new Vector3(landingOffsetCircle.x, 0f, landingOffsetCircle.y);

            if (coinPrefab != null)
            {
                // Instantiate with prefab's default rotation or a specific initial rotation if desired
                GameObject spawnedCoin = Instantiate(coinPrefab, randomSpawnPos, coinPrefab.transform.rotation);
                Coin coinComponent = spawnedCoin.GetComponent<Coin>();
                if (coinComponent != null)
                {
                    coinComponent.BezierDuration = bezierDuration;
                    coinComponent.SetInitialMovement(randomSpawnPos, landingOffsetHorizontal, baseJumpHeight);
                }
                else Debug.LogError("생성된 코인 오브젝트에 Coin 스크립트가 없습니다!", spawnedCoin);
            }
            else
            {
                Debug.LogError("Coin 프리팹이 설정되지 않았습니다!");
                return;
            }
        }
    }
}