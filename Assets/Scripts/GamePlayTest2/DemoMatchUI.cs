using UnityEngine;
using UnityEngine.UI; // 引入 UI 命名空间以使用 Button
using TMPro;
using GamePlayArchitecture;

public class DemoMatchUI : MonoBehaviour
{
    public TextMeshProUGUI stateText;
    public Button possessButton; // 拖入你新建的获取控制权按钮

    private AGameState.EMatchState _currentState;
    private float _currentWarmupTime; // 用于 UI 实时倒计时的本地变量

    private void Start()
    {
        EventSystem.Instance.Register<MatchStateChangedEventArgs>(OnMatchStateChanged);
        stateText.text = "Loading...";

        // 游戏刚开始时，隐藏这个按钮
        if (possessButton != null)
        {
            possessButton.gameObject.SetActive(false);
            // 绑定按钮点击事件
            possessButton.onClick.AddListener(OnPossessButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (EventSystem.Instance != null)
            EventSystem.Instance.UnRegister<MatchStateChangedEventArgs>(OnMatchStateChanged);
    }

    // 功能 ①：实时倒计时更新
    private void Update()
    {
        if (_currentState == AGameState.EMatchState.WaitingToStart)
        {
            _currentWarmupTime -= Time.deltaTime;
            if (_currentWarmupTime > 0)
            {
                // Mathf.CeilToInt 可以让 2.1 秒显示为 3，看着更舒服
                stateText.text = $"Ready... {Mathf.CeilToInt(_currentWarmupTime)}s";
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

                // 从裁判那里动态获取总的等待时间
                if (World.Instance.AuthorityGameMode is AGameMode gameMode)
                    _currentWarmupTime = gameMode.WarmupTime;
                break;

            case AGameState.EMatchState.InProgress:
                stateText.text = "Waiting for Player...";
                stateText.color = Color.red;

                // 功能 ②：比赛开始后，弹出获取控制权的按钮
                if (possessButton != null)
                {
                    possessButton.gameObject.SetActive(true);
                }
                break;
        }
    }

    // 功能 ③：玩家点击了按钮
    // 功能 ③：玩家点击了按钮
    private void OnPossessButtonClicked()
    {
        Log.D("[UI] 玩家点击了获取控制权按钮，发送广播...");

        // 【纯洁的事件驱动】：UI 不再去找 GameMode，直接广播事件！
        EventSystem.Instance.Trigger(new PlayerRequestControlEventArgs());

        // 按钮使命完成，隐藏自己
        possessButton.gameObject.SetActive(false);
        stateText.text = "FIGHT!";
    }
}