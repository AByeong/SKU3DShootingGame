using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameState_Start : GameState
{
    [SerializeField] private TextMeshProUGUI _startText;
    [SerializeField] private Button _startButton;
    [SerializeField] private Canvas _startCanvas;
    
    public override void Excute()
    {
        _startCanvas.gameObject.SetActive(true);
        
        
        _startText.text = "게임을 시작하시려면 버튼을 눌러주세용";    
        _startButton.onClick.AddListener(() => StartGame());
    }

    private void StartGame()
    {
        _startCanvas.gameObject.SetActive(false);
        ChangeState(GameManager.GameState.Wait);
        
    }
    
}
