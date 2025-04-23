using UnityEngine;
using UnityEditor;
using UnityEngine;

public class MySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static bool m_ShutingDown = false;
    private static object m_Lock = new object();
    private static T m_Instance;

    // 정적 변수로 초기화 여부를 설정하는 bool
    private static bool isInitializeOnStart = true; // 기본값은 true, 시작 시 초기화
    
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
    }

    private void OnApplicationQuit()
    {
        m_ShutingDown = true;
    }

    private void OnDestroy()
    {
        m_ShutingDown = true;
    }

    // 인스펙터에서 값을 설정할 수 있도록 static 메서드 제공
    public static void SetInitializeOnStart(bool value)
    {
        isInitializeOnStart = value;
    }

    // Start()나 다른 함수에서 초기화를 진행할 수도 있도록 변경
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



