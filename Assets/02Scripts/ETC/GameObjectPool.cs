using System.Collections.Generic;
using UnityEngine;

// T 타입의 컴포넌트를 가진 게임 오브젝트를 풀링하는 제네릭 클래스
// T는 반드시 Component를 상속받아야 함 
public class GenericObjectPool<T> : MonoBehaviour where T : Component
{
    [Header("풀링 설정")]
    [Tooltip("풀링할 오브젝트의 프리팹 (T 타입 컴포넌트 포함)")]
    [SerializeField] private T _prefab; // 풀링할 원본 프리팹 (T 타입 컴포넌트가 있어야 함)

    [Tooltip("초기 풀 크기")]
    [SerializeField] private int _initialPoolSize = 10; // 처음에 생성해 둘 오브젝트 개수

    [Tooltip("풀이 비었을 때 동적으로 생성 허용 여부")]
    [SerializeField] private bool _allowGrowth = true; // 풀이 비었을 때 추가 생성 허용 여부

    // 사용 가능한(비활성) 오브젝트를 저장하는 큐(Queue)
    private Queue<T> _pooledObjects = new Queue<T>();
    
    private void Awake()
    {
        // 프리팹이 할당되었는지 확인
        if (_prefab == null)
        {
            Debug.LogError($"[{typeof(T).Name} Pool] 프리팹이 할당되지 않았습니다!");
            enabled = false; // 오류 발생 시 스크립트 비활성화
            return;
        }

        InitializePool(); // 풀 초기화 함수 호출
    }

    // 풀을 초기화하는 함수
    private void InitializePool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewObject(false); // 비활성 상태로 새 오브젝트 생성 및 풀에 추가
        }
    }

    // 새로운 오브젝트를 생성하고 풀에 추가하는 도우미 함수
    private T CreateNewObject(bool activate = true)
    {
        // 프리팹으로부터 새 게임 오브젝트 인스턴스 생성
        T newInstance = Instantiate(_prefab);

        // 생성된 인스턴스가 T 타입 컴포넌트를 가지고 있는지 확인 (Instantiate는 원본 프리팹과 동일한 컴포넌트를 보장함)
        if (newInstance == null)
        {
            Debug.LogError($"[{typeof(T).Name} Pool] 프리팹 '{_prefab.name}'에서 컴포넌트 '{typeof(T).Name}'을 찾을 수 없습니다!", _prefab);
            return null; // 오류 시 null 반환
        }

        // 생성된 오브젝트를 풀 관리자(이 게임 오브젝트)의 자식으로 넣어 Hierarchy 정리 (선택 사항)
        newInstance.transform.SetParent(this.transform);

        // 기본적으로 비활성화 상태로 만들고 풀에 추가
        newInstance.gameObject.SetActive(activate);
        if (!activate)
        {
             _pooledObjects.Enqueue(newInstance); // 비활성화 상태면 큐에 바로 추가
        }

        return newInstance; // 생성된 인스턴스 반환
    }

    /// <summary>
    /// 풀에서 사용 가능한 오브젝트를 가져옵니다. 없으면 새로 생성할 수 있습니다.
    /// </summary>
    
    public T GetPooledObject()
    {
        // 풀(큐)에 사용 가능한 오브젝트가 있는지 확인
        if (_pooledObjects.Count > 0)
        {
            // 큐에서 하나를 꺼냄 (Dequeue)
            T instance = _pooledObjects.Dequeue();
            // 오브젝트를 활성화
            instance.gameObject.SetActive(true);
            // 활성화된 인스턴스 반환
            return instance;
        }
        // 풀이 비어있는 경우
        else if (_allowGrowth)
        {
            // 동적 생성이 허용되면 새 오브젝트를 '활성화된 상태'로 생성하고 바로 반환
            Debug.Log($"[{typeof(T).Name} Pool] 풀이 비어 새 오브젝트를 동적으로 생성합니다.");
            return CreateNewObject(true);
        }
        // 동적 생성이 허용되지 않으면 null 반환 (또는 오류 처리)
        else
        {
            Debug.LogWarning($"[{typeof(T).Name} Pool] 풀이 비었고 동적 생성이 허용되지 않았습니다!");
            return null;
        }
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀에 반환합니다.
    /// </summary>
    public void ReturnPooledObject(T instance)
    {
        // 반환하려는 인스턴스가 유효한지 확인
        if (instance == null)
        {
            Debug.LogWarning($"[{typeof(T).Name} Pool] Null 인스턴스를 반환하려고 시도했습니다.");
            return;
        }

        // 오브젝트를 비활성화
        instance.gameObject.SetActive(false);

        // 풀(큐)에 다시 추가
        _pooledObjects.Enqueue(instance);
    }
}