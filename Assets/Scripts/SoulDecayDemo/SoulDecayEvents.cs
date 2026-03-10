using GamePlayArchitecture;

// ============================================
// 1. 分数更新事件
// ============================================
public class ScoreChangedEventArgs : AbstractEventArgs
{
    public EFaction Faction;
    public int NewScore;

    public ScoreChangedEventArgs(EFaction faction, int newScore)
    {
        Faction = faction;
        NewScore = newScore;
        // UI 更新属于表现层，通常可以使用较低的优先级
        Priority = Priority.Appearance;
    }
}

// ============================================
// 2. 寿命倒计时把柄更新事件
// ============================================
public class PlayerLifespanTimerEventArgs : AbstractEventArgs
{
    public TimerHandle DecayTimerHandle;

    public PlayerLifespanTimerEventArgs(TimerHandle handle)
    {
        DecayTimerHandle = handle;
        // 这决定了 UI 去查哪一个计时器，属于比较重要的逻辑同步
        Priority = Priority.Logic;
    }
}