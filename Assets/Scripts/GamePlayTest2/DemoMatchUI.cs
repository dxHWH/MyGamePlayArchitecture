using UnityEngine;
using GamePlayArchitecture;
using TMPro;

public class DemoMatchUI : MonoBehaviour
{
    public TextMeshProUGUI stateText;

    private void Start()
    {
        EventSystem.Instance.Register<MatchStateChangedEventArgs>(OnMatchStateChanged);
        stateText.text = "Loading..."; // 改为英文
    }

    private void OnDestroy()
    {
        if (EventSystem.Instance != null)
        {
            EventSystem.Instance.UnRegister<MatchStateChangedEventArgs>(OnMatchStateChanged);
        }
    }

    private void OnMatchStateChanged(MatchStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case AGameState.EMatchState.WaitingToStart:
                stateText.text = "Ready... 3s"; // 改为英文
                stateText.color = Color.yellow;
                break;
            case AGameState.EMatchState.InProgress:
                stateText.text = "FIGHT!"; // 改为英文
                stateText.color = Color.red;
                break;
            case AGameState.EMatchState.WaitingPostMatch:
                stateText.text = "Game Over!"; // 改为英文
                break;
        }
    }
}