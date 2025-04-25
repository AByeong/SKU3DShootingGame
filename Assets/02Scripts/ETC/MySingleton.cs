using UnityEngine;
using UnityEditor;

public class MySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static bool m_ShutingDown = false;
    private static object m_Lock = new object();
    private static T m_Instance;

    private static bool isInitializeOnStart = true;

    public static T Instance
    {
        get
        {
            if (m_ShutingDown)
            {
                Debug.LogWarning(typeof(T) + " 는 이미 없어졌습니다, null을 리턴합니다.");
                return null;
            }

            lock (m_Lock)
            {
                if (m_Instance == null)
                {
                    if (isInitializeOnStart)
                    {
                        InitializeSingleton();
                    }
                }
                return m_Instance;
            }
        }
    }

    private static void InitializeSingleton()
    {
        m_Instance = (T)FindObjectOfType(typeof(T));

        if (m_Instance == null)
        {
            var singletonObject = new GameObject();
            m_Instance = singletonObject.AddComponent<T>();
            singletonObject.name = typeof(T).Name + " (Singleton)";
            DontDestroyOnLoad(singletonObject);
        }
        else
        {
            // 중복 인스턴스 제거
            var allInstances = FindObjectsOfType<T>();
            foreach (var instance in allInstances)
            {
                if (instance != m_Instance)
                {
                    Debug.LogWarning(typeof(T) + " 중복 인스턴스를 제거합니다.");
                    Destroy(instance.gameObject);
                }
            }
        }
    }

    protected virtual void Awake()
    {
        if (m_Instance == null)
        {
            m_Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_Instance != this)
        {
            Debug.LogWarning(typeof(T) + " 싱글톤 인스턴스가 이미 존재합니다. 중복된 인스턴스를 파괴합니다.");
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        m_ShutingDown = true;
    }

    private void OnDestroy()
    {
        m_ShutingDown = true;
    }

    public static void SetInitializeOnStart(bool value)
    {
        isInitializeOnStart = value;
    }

    public void InitializeAtRuntime()
    {
        if (!isInitializeOnStart)
        {
            InitializeSingleton();
        }
    }

    public static bool GetInitializeOnStart()
    {
        return isInitializeOnStart;
    }
}
