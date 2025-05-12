using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GenericObjectPool<T> : MonoBehaviour where T : Component
{
    [Header("Addressable 프리팹 참조")]
    public AssetReferenceT<GameObject> PrefabReference;

    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private bool _allowGrowth = true;

    private readonly Queue<T> _pooledObjects = new Queue<T>();

    private void Start()
    {
        if (PrefabReference == null)
        {
            Debug.LogError($"[{typeof(T).Name} Pool] Addressable 프리팹이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        _ = InitializePoolAsync();
    }

    private async Task InitializePoolAsync()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            var instance = await CreateNewObjectAsync(false);
            if (instance != null)
                _pooledObjects.Enqueue(instance);
        }
    }

    private async Task<T> CreateNewObjectAsync(bool activate = true)
    {
        var handle = PrefabReference.InstantiateAsync(transform);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = handle.Result;
            T component = obj.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"[{typeof(T).Name} Pool] 프리팹에서 {typeof(T).Name} 컴포넌트를 찾을 수 없습니다!");
                Addressables.ReleaseInstance(obj);
                return null;
            }

            obj.SetActive(activate);
            if (!activate)
                _pooledObjects.Enqueue(component);

            return component;
        }
        else
        {
            Debug.LogError($"[{typeof(T).Name} Pool] Addressable 로드 실패: {PrefabReference.RuntimeKey}");
            return null;
        }
    }

    public async Task<T> GetPooledObjectAsync()
    {
        if (_pooledObjects.Count > 0)
        {
            var instance = _pooledObjects.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }
        else if (_allowGrowth)
        {
            return await CreateNewObjectAsync(true);
        }
        else
        {
            return null;
        }
    }

    public void ReturnPooledObject(T instance)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
        _pooledObjects.Enqueue(instance);
    }

    public void ReleaseObject(T instance)
    {
        if (instance != null)
            Addressables.ReleaseInstance(instance.gameObject);
    }
}
