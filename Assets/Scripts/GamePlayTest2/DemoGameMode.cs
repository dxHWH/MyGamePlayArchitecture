using UnityEngine;
using GamePlayArchitecture;

public class DemoGameMode : AGameMode
{
    [Header("Demo 专属配置")]
    public string WelcomeMessage = "欢迎来到 GameMode 终极测试关卡！";

    private AController _pendingController;
    private APawn _pendingPawn;

    public override void BeginPlay()
    {
        Log.N($"<color=cyan>[DemoGameMode] {WelcomeMessage}</color>");
        base.BeginPlay();

        // 【事件订阅】：裁判上班第一件事，就是监听玩家有没有点击入场按钮
        EventSystem.Instance.Register<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);
    }

    // 生命周期管理：对象销毁时必须注销事件，防止内存泄漏！
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (EventSystem.Instance != null)
        {
            EventSystem.Instance.UnRegister<PlayerRequestControlEventArgs>(OnPlayerRequestedControl);
        }
    }

    protected override void StartMatch()
    {
        base.StartMatch();
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

    // 【事件回调】：当裁判听到 UI 大喊“玩家点按钮了”，执行灵魂注入！
    private void OnPlayerRequestedControl(PlayerRequestControlEventArgs e)
    {
        if (_pendingController != null && _pendingPawn != null)
        {
            _pendingController.Possess(_pendingPawn);
            Log.N("<color=cyan>[DemoGameMode] 收到玩家请求，灵魂注入完毕，可以移动！</color>");
        }
    }
}