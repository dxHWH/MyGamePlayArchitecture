using UnityEngine;
using GamePlayArchitecture;

public class DemoGameMode : AGameMode
{
    [Header("Demo 专属配置")]
    public string WelcomeMessage = "欢迎来到 GameMode 终极测试关卡！";

    private AController _pendingController;
    private APawn _pendingPawn;

    // 【新增】：裁判手中的“最高权力秒表”
    private TimerHandle _warmupTimerHandle;

    public override void BeginPlay()
    {
        Log.N($"<color=cyan>[DemoGameMode] {WelcomeMessage}</color>");
        base.BeginPlay();

        // 裁判上班第一件事，监听玩家获取控制权的请求
        EventSystem.Instance.Register<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);

        // 【核心重构：掌控时间】：
        // 游戏刚开始，裁判按下秒表，进入 Warmup (热身) 阶段
        Log.N($"<color=yellow>[DemoGameMode] 比赛进入热身阶段，{WarmupTime}秒后正式开始！</color>");

        _warmupTimerHandle = TimerSystem.Instance.CreateTimer(
            duration: WarmupTime, // 这里的 WarmupTime 是从你父类 AGameMode 继承来的属性
            onComplete: () =>
            {
                // 【倒计时结束】：裁判吹哨，比赛正式开始！
                StartMatch();
            },
            timerName: "MatchWarmupTimer" // 【关键】：我们给这个计时器起个名字，方便全宇宙（包括 UI）来查时间
        );
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventSystem.Instance != null)
        {
            EventSystem.Instance.UnRegister<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);
        }

        // 【安全防线】：如果 GameMode 被意外销毁（比如强退关卡），立刻掐死秒表
        TimerSystem.Instance.StopTimer(_warmupTimerHandle);
    }

    protected override void StartMatch()
    {
        base.StartMatch(); // 父类在这里面大概率会改变状态（变成 InProgress）并广播事件给全服

        Log.N("<color=cyan>[DemoGameMode] 倒计时结束，生成灰色角色...</color>");
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (DefaultPawnClass == null || DefaultControllerClass == null) return;

        _pendingPawn = Instantiate(DefaultPawnClass, new Vector3(0, 0.5f, 0), Quaternion.identity);
        _pendingPawn.name = "DemoCube_Runtime";

        _pendingController = Instantiate(DefaultControllerClass);
        _pendingController.name = "DemoController_Runtime";
    }

    private void OnPlayerRequestedControl(PlayerRequestControlEventArgs e)
    {
        if (_pendingController != null && _pendingPawn != null)
        {
            _pendingController.Possess(_pendingPawn);
            Log.N("<color=cyan>[DemoGameMode] 收到玩家请求，灵魂注入完毕，可以移动！</color>");
        }
    }
}