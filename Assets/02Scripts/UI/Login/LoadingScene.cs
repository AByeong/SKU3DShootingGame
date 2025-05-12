using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders; // SceneInstance를 위해 필요

public class LoadingScene : MonoBehaviour
{
    // Addressables에서 로드할 씬의 주소
    public string SceneAddress = "GameScene"; 

    public Slider ProgressSlider;
    public TextMeshProUGUI ProgressText;

    private void Start()
    {
        StartCoroutine(LoadNextScene_Addressable());
    }

    private IEnumerator LoadNextScene_Addressable()
    {
        // Addressables를 이용해 씬 로드 시작 (씬 비활성화 상태로 로딩)
        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(SceneAddress, activateOnLoad:false); // <-- activateOnLoad:false 유지

        // 오류 처리: 로딩 시작 전부터 실패 상태인지 확인
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError($"Addressable scene loading failed immediately for address: {SceneAddress}. Exception: {handle.OperationException}");
            // UI에 오류 메시지를 표시하거나, 다른 씬으로 돌아가는 등의 처리
            yield break; // 코루틴 종료
        }

        while (!handle.IsDone) // 씬 데이터 로드가 완료될 때까지 대기 (활성화는 아직 아님)
        {
            // 루프 안에서 지속적으로 실패 상태를 확인
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Addressable scene loading failed during operation for address: {SceneAddress}. Progress: {handle.PercentComplete * 100f}%. Exception: {handle.OperationException}");
                yield break; // 코루틴 종료
            }

            float progress = handle.PercentComplete;

            if (ProgressSlider != null) ProgressSlider.value = progress;
            if (ProgressText != null) ProgressText.text = $"{(progress * 100f):F0}%";
            Debug.Log($"로딩 중: {progress * 100f}%");

            // 씬 데이터 로드가 거의 완료되었을 때 (활성화 대기)
            // 이 부분에서는 더 이상 ActivateAsync()를 직접 호출하지 않고,
            // handle.IsDone이 된 후 (아래 if 문) 활성화를 진행할 것입니다.
            // 즉, progress >= 0.9f 조건은 단지 UI 업데이트를 위한 로깅/프로그레스바 용도로만 사용됩니다.
            if (progress >= 0.9f)
            {
                Debug.Log("어드레서블 씬 데이터 로딩 거의 완료됨. 최종 완료 및 활성화 대기 중...");
            }


            yield return null; // 다음 프레임까지 대기
        }

        // handle.IsDone이 true가 되면 이 부분이 실행됩니다.
        // 이 시점에는 씬 데이터 로딩이 완료되었지만, activateOnLoad: false 때문에 아직 활성화되지는 않았습니다.
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"씬 데이터 로딩 완료: {SceneAddress}. 이제 씬을 활성화합니다.");
            
            // 씬 데이터를 로드했지만 아직 활성화되지 않은 상태이므로, 여기서 활성화합니다.
            AsyncOperation activateOperation = handle.Result.ActivateAsync();

            // 씬 활성화 작업이 완료될 때까지 대기합니다.
            // 이 코루틴은 씬 활성화가 완전히 끝날 때까지 다음 프레임으로 넘어가지 않습니다.
            yield return new WaitUntil(() => activateOperation.isDone); 

            Debug.Log($"최종 씬 활성화 완료 및 씬 이동: {SceneAddress}");
        }
        else
        {
            Debug.LogError($"Addressable scene loading finished with status: {handle.Status}. Exception: {handle.OperationException}");
        }

        // 주의: LoadSceneAsync로 로드된 씬 핸들은 일반적으로 씬이 언로드되거나 게임이 종료될 때 자동으로 해제됩니다.
        // 따라서 명시적으로 Addressables.Release(handle);를 호출할 필요는 대부분 없습니다.
    }
}