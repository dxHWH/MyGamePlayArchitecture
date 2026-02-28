using GamePlayArchitecture;

// 这封信由 UI 发出，表示当前机器上的玩家点击了“请求控制/部署”按钮
public class PlayerRequestControlEventArgs : AbstractEventArgs
{
    public PlayerRequestControlEventArgs()
    {
        // 玩家输入属于极其核心的操作，赋予最高逻辑优先级
        Priority = Priority.Logic;
    }
}