using GamePlayArchitecture;
using System; // 必须引入，为了使用 Action
using UnityEngine;

public class DemoGameMode : AGameMode
{
    [Header("Demo 专属配置")]
    public string WelcomeMessage = "欢迎来到 GameMode 终极测试关卡！";

    // 【修改点】：将倒计时参数下放到具体的业务模式中
    public float WarmupTime = 3.0f;

    private AController _pendingController;
    private APawn _pendingPawn;

    // 【新增】：零GC优化所需缓存
    private Action _startMatchAction;

    public override void BeginPlay()
    {
        Log.N($"<color=cyan>[DemoGameMode] {WelcomeMessage}</color>");

        // 在游戏最早期缓存委托，后续传给 TimerSystem 时实现完全无 GC 分配
        _startMatchAction = StartMatch;

        // 裁判上班第一件事，监听玩家获取控制权的请求
        EventSystem.Instance.Register<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);

        // base.BeginPlay() 内部会按照顺序去调用 StartPlay()
        base.BeginPlay();
    }

    // 【核心修改】：重写 StartPlay，在这里接管热身逻辑
    public override void StartPlay()
    {
        // 1. 让父类先把状态切到 WaitingToStart
        base.StartPlay();

        // 2. 子类实现倒计时业务逻辑
        Log.N($"<color=yellow>[DemoGameMode] 比赛进入热身阶段，{WarmupTime}秒后正式开始！</color>");

        if (WarmupTime > 0)
        {
            TimerSystem.Instance.CreateTimer(
                duration: WarmupTime,
                onComplete: _startMatchAction,
                timerName: "MatchWarmupTimer" // 命名规范，方便 UI 查表
            );
        }
        else
        {
            StartMatch();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventSystem.HasInstance)
        {
            EventSystem.Instance.UnRegister<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);
        }
        // 关卡销毁时，清理自己设立的闹钟
        if (TimerSystem.HasInstance)//在unity环境下 通过loadScean切换场景时，需要先判断计时器系统是否存在（理论上，对任何单例都应如此）。
        {
            TimerSystem.Instance.StopTimer("MatchWarmupTimer");
        }

     
    }

    protected override void StartMatch()
    {
        // 1. 让父类先把状态切到 InProgress，并广播给全服 UI
        base.StartMatch();

        // 2. 执行本关卡独有的业务逻辑：生成灰色测试方块
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