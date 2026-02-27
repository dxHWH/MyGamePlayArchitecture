using System.Collections;
using System.Collections.Generic;
using GamePlayArchitecture;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 当比赛阶段发生变化时，由 GameState 抛出的全局事件
    /// </summary>
    public class MatchStateChangedEventArgs : AbstractEventArgs
    {
        public AGameState.EMatchState OldState;
        public AGameState.EMatchState NewState;

        public MatchStateChangedEventArgs()
        {
            // 阶段切换是极其核心的逻辑，优先级设为最高 (Logic = 0)
            Priority = Priority.Logic;
        }
    }
}