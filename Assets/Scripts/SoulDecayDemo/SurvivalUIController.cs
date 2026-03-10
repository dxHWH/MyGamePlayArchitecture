using GamePlayArchitecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // 或者 using TMPro;

public class SurvivalUIController : MonoBehaviour
{
    [Header("计分板文本引用")]
    public TextMeshProUGUI PlayerScoreText;
    public TextMeshProUGUI RedAIScoreText;
    public TextMeshProUGUI BlueAIScoreText;

    [Header("寿命倒计时引用")]
    public TextMeshProUGUI LifespanText;

    // 用于保存当前正在生效的玩家计时器把柄
    private TimerHandle _currentPlayerTimer = TimerHandle.Invalid;

    private void OnEnable()
    {
        if (EventSystem.Instance)
        {
            EventSystem.Instance.Register<ScoreChangedEventArgs>(OnScoreChanged);
            EventSystem.Instance.Register<PlayerLifespanTimerEventArgs>(OnLifespanTimerChanged);
        }
    }

    private void OnDisable()
    {
        if (EventSystem.HasInstance)
        {
            EventSystem.Instance.UnRegister<ScoreChangedEventArgs>(OnScoreChanged);
            EventSystem.Instance.UnRegister<PlayerLifespanTimerEventArgs>(OnLifespanTimerChanged);
        }
    }

    // 收到计分板事件更新
    private void OnScoreChanged(ScoreChangedEventArgs args)
    {
        switch (args.Faction)
        {
            case EFaction.Player:
                if (PlayerScoreText != null) PlayerScoreText.text = $"Player: {args.NewScore}";
                break;
            case EFaction.RedAI:
                if (RedAIScoreText != null) RedAIScoreText.text = $"Red AI: {args.NewScore}";
                break;
            case EFaction.BlueAI:
                if (BlueAIScoreText != null) BlueAIScoreText.text = $"Blue AI: {args.NewScore}";
                break;
        }
    }

    // 收到了新的倒计时把柄
    private void OnLifespanTimerChanged(PlayerLifespanTimerEventArgs args)
    {
        _currentPlayerTimer = args.DecayTimerHandle;
    }

    // 每帧去问 TimerSystem 剩余时间
    private void Update()
    {
        if (LifespanText == null) return;

        // 安全校验：把柄是否有效
        if (TimerSystem.Instance && TimerSystem.Instance.IsHandleValid(_currentPlayerTimer))
        {
            float timeRemaining = TimerSystem.Instance.GetTimeRemaining(_currentPlayerTimer);
            LifespanText.text = $"Last Time: {timeRemaining:F1} s";

            // 视觉表现：倒计时低于 1.5 秒时，红白闪烁预警
            if (timeRemaining <= 1.5f)
            {
                LifespanText.color = Mathf.PingPong(Time.time * 8f, 1f) > 0.5f ? Color.red : Color.white;
            }
            else
            {
                LifespanText.color = Color.black;
            }
        }
        else
        {
            // 如果把柄失效（还没出生，或者已经原地爆炸了）
            LifespanText.text = "DIE";
            LifespanText.color = Color.red;
        }
    }
}