using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EnemyPool : MonoBehaviour
{
    [Header("Addressable 프리팹 참조")]
    public AssetReferenceT<GameObject> EnemyPrefab;

    [Header("Enemy 스폰 특정 설정")]
    public Transform SpawnPoint;
    [Tooltip("순찰 지점 (월드 좌표 사용 가정)")]
    public PatrolPoints PatrolPoints;
    
    [Tooltip("스폰 활성화 여부")]
    public bool SpawnSwitch = true;

    [Header("Enemy 스폰 파라미터")]
    public float SpawnInterval = 2f;
    public float Radius = 5f;

    private float _timer = 0f;

    // 풀링된 객체들
    private readonly Queue<Enemy> _enemyPool = new();

    [Tooltip("초기 생성할 적 수")]
    public int InitialSize = 10;

    [Tooltip("풀 크기 초과 시 생성 허용 여부")]
    public bool AllowGrowth = true;

    private async void Start()
    {
        await InitializePoolAsync();
    }

    private async Task InitializePoolAsync()
    {
        for (int i = 0; i < InitialSize; i++)
        {
            var enemy = await CreateNewEnemyAsync(false);
            if (enemy != null)
                _enemyPool.Enqueue(enemy);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Play &&
            SpawnSwitch)
        {
            _timer += Time.deltaTime;

            if (_timer >= SpawnInterval)
            {
                _ = SpawnEnemyAsync();
                _timer = 0f;
            }
        }
        else
        {
            _timer = 0f;
        }
    }

    // 외부에서 가져오기 위한 래퍼
    public async Task<Enemy> GetObjectFromPool()
    {
        if (_enemyPool.Count > 0)
        {
            var enemy = _enemyPool.Dequeue();
            enemy.gameObject.SetActive(true);
            return enemy;
        }
        else if (AllowGrowth)
        {
            return await CreateNewEnemyAsync(true);
        }
        else
        {
            return null;
        }
    }

    // 외부에서 반환하는 함수
    public void EnemyDie(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.gameObject.SetActive(false);
        _enemyPool.Enqueue(enemy);
    }

    private async Task<Enemy> CreateNewEnemyAsync(bool activate = true)
    {
        var handle = EnemyPrefab.InstantiateAsync(transform);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var obj = handle.Result;
            var enemy = obj.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogError("Enemy 컴포넌트를 찾을 수 없습니다.");
                Addressables.ReleaseInstance(obj);
                return null;
            }

            obj.SetActive(activate);
            if (!activate)
                obj.transform.SetParent(transform); // 풀에 보관

            return enemy;
        }
        else
        {
            Debug.LogError($"Enemy Addressable 로드 실패: {EnemyPrefab.RuntimeKey}");
            return null;
        }
    }

    private async Task SpawnEnemyAsync()
    {
        var enemyInstance = await GetObjectFromPool();

        if (enemyInstance != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-Radius, Radius), 0f, Random.Range(-Radius, Radius)
            );

            spawnPos.y = 1.1f;

            enemyInstance.transform.position = spawnPos;
            enemyInstance.transform.rotation = Quaternion.identity;

            enemyInstance.PatrolPoints = PatrolPoints;
            enemyInstance.name = this.name + "_Enemy";
            enemyInstance.StartPosition = spawnPos;

            enemyInstance.DebugPosition("소환된");
            enemyInstance.Initialize();
        }
        else
        {
            Debug.LogWarning("[EnemyPool] 사용 가능한 Enemy 인스턴스를 가져오지 못했습니다.");
        }
    }
}
