using UnityEngine;
using GamePlayArchitecture; // 引入底层框架命名空间

// 这个类属于具体的业务逻辑，不再属于底层框架
public class DemoGameMode : AGameMode
{
    [Header("Demo 专属配置")]
    public string WelcomeMessage = "欢迎来到 GameMode 终极测试关卡！";

    public override void BeginPlay()
    {
        // 1. 关卡刚加载时，打印属于这个 Demo 的专属欢迎语
        Log.N($"<color=cyan>[DemoGameMode] {WelcomeMessage}</color>");

        // 2. 必须调用基类的方法！让底层框架去初始化计分板、开启倒计时等
        base.BeginPlay();
    }

    protected override void StartMatch()
    {
        // 1. 调用基类的 StartMatch，让框架去完成“改变计分板状态”和“生成玩家”的核心工作
        base.StartMatch();
        SpawnPlayer();


        // 2. 核心工作做完后，在这里写 Demo 的专属业务逻辑
        // 比如：播放一声震撼的开场音效、生成几个小怪、或者给玩家发放初始武器
        Log.N("<color=cyan>[DemoGameMode] 专属逻辑：比赛正式打响，神装已发放！</color>");
    }

    private void SpawnPlayer()
    {
        // 确保 Inspector 面板里拖入了预制体
        if (DefaultPawnClass == null || DefaultControllerClass == null)
        {
            Log.E("[DemoGameMode] 错误：DefaultPawnClass 或 DefaultControllerClass 未在面板中配置！");
            return;
        }

        // 步骤 A：生成肉体 (Pawn)
        APawn newPawn = Instantiate(DefaultPawnClass, new Vector3(0, 0.5f, 0), Quaternion.identity);
        newPawn.name = "DemoCube_Runtime";

        // 步骤 B：生成灵魂 (Controller)
        AController newController = Instantiate(DefaultControllerClass);
        newController.name = "DemoController_Runtime";

        // 步骤 C：灵魂注入！
        newController.Possess(newPawn);

        Log.N("<color=cyan>[DemoGameMode] 玩家生成完毕，控制权已移交！</color>");
    }

    public override void EndMatch()
    {
        base.EndMatch();
        Log.N("<color=cyan>[DemoGameMode] 专属逻辑：正在结算当前关卡的特殊积分...</color>");
    }
}