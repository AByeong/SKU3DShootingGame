using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

// T 타입의 컴포넌트를 가진 게임 오브젝트를 풀링하는 제네릭 클래스
// T는 반드시 Component를 상속받아야 함 
public class MultiGenericObjectPool<T> : MonoBehaviour where T : Component
{
    [Header("풀링 설정")]
    [Tooltip("풀링할 오브젝트의 프리팹 목록 (모든 요소는 T 타입 컴포넌트 포함)")]
    [SerializeField] public List<T> Prefabs; // 변경: 단일 프리팹에서 프리팹 리스트로

    [Tooltip("초기 풀 크기 (각 프리팹당이 아닌, 총 개수)")]
    [SerializeField] private int _initialPoolSize = 10; 

    [Tooltip("풀이 비었을 때 동적으로 생성 허용 여부")]
    [SerializeField] private bool _allowGrowth = true; 

    // 사용 가능한(비활성) 오브젝트를 저장하는 단일 큐
    private Queue<T> _pooledObjects = new Queue<T>();
    
    private void Awake()
    {
        // 프리팹 리스트 유효성 검사
        if (Prefabs == null || Prefabs.Count == 0)
        {
            Debug.LogError($"[{typeof(T).Name} Pool] 프리팹 리스트가 비어있거나 할당되지 않았습니다!");
            enabled = false; 
            return;
        }

        // 리스트 내의 각 프리팹 유효성 검사
        for (int i = 0; i < Prefabs.Count; i++)
        {
            if (Prefabs[i] == null)
            {
                 Debug.LogError($"[{typeof(T).Name} Pool] 프리팹 리스트의 {i}번째 요소가 비어있습니다!");
                 enabled = false;
                 return;
            }
             // 프리팹 자체에 T 컴포넌트가 있는지 다시 확인 (Instantiate 전에 확인하는 것이 더 안전)
            if (Prefabs[i].GetComponent<T>() == null)
            {
                 Debug.LogError($"[{typeof(T).Name} Pool] 프리팹 '{Prefabs[i].name}'에 '{typeof(T).Name}' 컴포넌트가 없습니다!", Prefabs[i]);
                 enabled = false;
                 return;
            }
        }

        InitializePool(); // 풀 초기화 함수 호출
    }

    // 풀을 초기화하는 함수
    private void InitializePool()
    {
        // 지정된 초기 크기만큼 오브젝트를 생성하여 풀에 추가
        // 각 생성 시 랜덤한 프리팹을 선택
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewObject(false); // 비활성 상태로 랜덤 프리팹 기반 오브젝트 생성 및 풀에 추가
        }
         Debug.Log($"[{typeof(T).Name} Pool] 초기화 완료. 풀 크기: {_pooledObjects.Count}");
    }

    // 새로운 오브젝트를 생성하고 풀에 추가하는 도우미 함수 (랜덤 프리팹 선택)
    private T CreateNewObject(bool activate = true)
    {
        // 리스트에서 랜덤 프리팹 선택 (Awake에서 리스트가 비어있지 않음을 보장)
        int randomIndex = Random.Range(0, Prefabs.Count);
        T selectedPrefab = Prefabs[randomIndex];

        // 선택된 프리팹으로부터 새 게임 오브젝트 인스턴스 생성
        T newInstance = Instantiate(selectedPrefab);

        // 생성된 인스턴스 유효성 확인 (이론상 Instantiate가 실패하지 않는 한 필요 없지만 안전 차원)
        if (newInstance == null)
        {
            Debug.LogError($"[{typeof(T).Name} Pool] 프리팹 '{selectedPrefab.name}' 인스턴스화 실패!", selectedPrefab);
            return null; 
        }

        // 생성된 오브젝트를 풀 관리자의 자식으로 설정 (Hierarchy 정리)
        newInstance.transform.SetParent(this.transform);

        // 활성화 상태 설정 및 풀에 추가 (비활성화 시)
        newInstance.gameObject.SetActive(activate);
        if (!activate)
        {
             _pooledObjects.Enqueue(newInstance); 
        }

        return newInstance; // 생성된 인스턴스 반환
    }

    /// <summary>
    /// 풀에서 사용 가능한 오브젝트를 가져옵니다. 
    /// 풀에 오브젝트가 있으면 큐에서 하나를 꺼내고, 
    /// 없으면 랜덤 프리팹 중 하나로 새로 생성할 수 있습니다.
    /// </summary>
    public T GetPooledObject()
    {
        // 풀(큐)에 사용 가능한 오브젝트가 있는지 확인
        if (_pooledObjects.Count > 0)
        {
            // 큐에서 하나를 꺼냄 (Dequeue) - 어떤 프리팹 기반인지는 큐 순서에 따름
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
            // CreateNewObject가 내부적으로 랜덤 프리팹을 선택함
            Debug.Log($"[{typeof(T).Name} Pool] 풀이 비어 랜덤 프리팹으로 새 오브젝트를 동적으로 생성합니다.");
            return CreateNewObject(true); 
        }
        // 풀이 비었고 동적 생성이 허용되지 않으면 null 반환
        else
        {
            //Debug.LogWarning($"[{typeof(T).Name} Pool] 풀이 비었고 동적 생성이 허용되지 않아 오브젝트를 가져올 수 없습니다!");
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

        // 다시 풀(큐)에 추가 (어떤 프리팹 기반이었는지는 상관없이 큐에 들어감)
        _pooledObjects.Enqueue(instance);
    }
}