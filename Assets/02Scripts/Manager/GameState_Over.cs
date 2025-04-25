using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameState_Over : GameState
{
    [SerializeField] private Canvas _overCanvas;
[SerializeField] private Button _endButton;
    public override void Excute()
    {
        _overCanvas.gameObject.SetActive(true);
        _endButton.onClick.AddListener(() => Restart());
    }

    private void Restart()
    {
        SceneManager.LoadScene(0);

    }
}
