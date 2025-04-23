using UnityEngine;
using UnityEditor;

// MySingleton<T> 및 그 자식 클래스들을 타겟으로 함
[CustomEditor(typeof(MySingleton<>), true)]
[CanEditMultipleObjects]
public class MySingletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 인스펙터 기본 UI (예: GameManager의 Dummy 필드 등)
        DrawDefaultInspector();

        // target은 MySingleton<T>를 상속받은 컴포넌트 인스턴스입니다.
        // 이 에디터는 MySingleton<T> 파생 클래스에만 적용되므로,
        // target이 관련 타입이라는 것은 보장됩니다.
        // 하지만 static 변수에 접근해야 하므로 target 인스턴스 자체보다는
        // static 멤버에 접근하는 방법을 사용합니다.

        // isInitializeOnStart는 static 변수이므로 특정 인스턴스가 아닌 클래스 레벨에서 접근합니다.
        // 어떤 T 타입이든 동일한 static 변수를 공유하므로,
        // 접근 가능한 아무 MonoBehaviour 타입 (예: MonoBehaviour 자체 또는 GameManager)을
        // 제네릭 파라미터로 사용하여 static 멤버에 접근할 수 있습니다.
        // (컴파일러가 static 멤버를 처리하는 방식 덕분)
        // 여기서는 MySingleton<MonoBehaviour>를 사용해 접근해봅니다.
        // (또는 방금 추가한 GetInitializeOnStart 사용)

        bool currentValue;
        // 만약 GetInitializeOnStart 메서드를 추가했다면:
        try // 안전을 위해 try-catch 사용 가능 (필수는 아님)
        {
             // GetInitializeOnStart() 메서드를 호출하여 현재 값을 가져옵니다.
             // 실제로는 어떤 MonoBehaviour를 넣어도 동일한 static 메서드를 호출하게 됩니다.
            var method = typeof(MySingleton<>).MakeGenericType(typeof(MonoBehaviour))
                                             .GetMethod("GetInitializeOnStart", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            currentValue = (bool)method.Invoke(null, null);
        }
        catch
        {
            // 만약 메서드 호출에 실패하면 기본값 표시 (오류 로깅 추가 가능)
            currentValue = true; // 또는 다른 기본값
            EditorGUILayout.HelpBox("Could not read static 'isInitializeOnStart' value.", MessageType.Warning);
        }


        // 토글 UI를 그리고, 사용자 입력을 받습니다.
        EditorGUI.BeginChangeCheck(); // 변경 감지 시작
        bool newValue = EditorGUILayout.Toggle("Initialize On Start (Static)", currentValue);
        if (EditorGUI.EndChangeCheck()) // 변경이 감지되었다면
        {
             // SetInitializeOnStart static 메서드를 호출하여 값을 변경합니다.
             // 마찬가지로 어떤 MonoBehaviour를 넣어도 동일한 static 메서드를 호출합니다.
            try
            {
                var method = typeof(MySingleton<>).MakeGenericType(typeof(MonoBehaviour))
                                                 .GetMethod("SetInitializeOnStart", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                method.Invoke(null, new object[] { newValue });

                // 변경 사항을 즉시 반영하기 위해 Repaint 요청 (선택 사항)
                Repaint();
            }
            catch
            {
                EditorGUILayout.HelpBox("Could not set static 'isInitializeOnStart' value.", MessageType.Error);
            }
        }


        // 경고 메시지 추가 (static 변수의 의미 설명)
        EditorGUILayout.HelpBox("This 'Initialize On Start' setting is static and shared across ALL MySingleton<T> derived classes in the project.", MessageType.Info);
    }
}