using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePlayArchitecture;

public class DemoMatchUI : MonoBehaviour
{
    public TextMeshProUGUI stateText;
    public Button possessButton;

    private AGameState.EMatchState _currentState;

    private void Start()
    {
        EventSystem.Instance.Register<MatchStateChangedEventArgs>(OnMatchStateChanged);
        stateText.text = "Loading...";

        if (possessButton != null)
        {
            possessButton.gameObject.SetActive(false);
            possessButton.onClick.AddListener(OnPossessButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (EventSystem.Instance != null)
            EventSystem.Instance.UnRegister<MatchStateChangedEventArgs>(OnMatchStateChanged);
    }

    // 【找回灵魂】：UI 重新拿回 Update，但这次它自己不算时间了，只负责“看表”！
    private void Update()
    {
        // 只有在等待开始的阶段，UI 才需要去查秒表
        if (_currentState == AGameState.EMatchState.WaitingToStart)
        {
            // 通过名字，直接去大管家那里查 GameMode 定下的那个闹钟！
            float remaining = TimerSystem.Instance.GetTimeRemaining("MatchWarmupTimer");

            // 为了防止拿到 0 或者负数导致显示奇怪，做个保护
            if (remaining > 0)
            {
                stateText.text = $"Ready... {Mathf.CeilToInt(remaining)}s";
            }
        }
    }

    private void OnMatchStateChanged(MatchStateChangedEventArgs e)
    {
        _currentState = e.NewState;

        switch (e.NewState)
        {
            case AGameState.EMatchState.WaitingToStart:
                stateText.color = Color.yellow;
                // 细节：这里不需要再创建计时器了，UI 只管在 Update 里去读取即可
                break;

            case AGameState.EMatchState.InProgress:
                stateText.text = "Waiting for Player...";
                stateText.color = Color.red;

                if (possessButton != null)
                {
                    possessButton.gameObject.SetActive(true);
                }
                break;
        }
    }

    private void OnPossessButtonClicked()
    {
        Log.D("[UI] 玩家点击了获取控制权按钮，发送广播...");

        EventSystem.Instance.Trigger(new PlayerRequestControlEventArgs());

        possessButton.gameObject.SetActive(false);
        stateText.text = "FIGHT!";
    }
}