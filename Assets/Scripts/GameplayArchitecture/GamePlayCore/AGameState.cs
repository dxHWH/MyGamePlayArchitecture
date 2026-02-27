using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 包含复杂比赛状态机的计分板，状态改变时主动通过 EventSystem 广播
    /// </summary>
    public class AGameState : AGameStateBase
    {
        public enum EMatchState
        {
            EnteringMap,      // 刚进地图
            WaitingToStart,   // 倒计时准备阶段
            InProgress,       // 比赛进行中
            WaitingPostMatch  // 比赛结束，展示结算画面
        }

        public EMatchState MatchState { get; protected set; } = EMatchState.EnteringMap;

        // 供裁判调用的唯一修改接口
        public void SetMatchState(EMatchState newState)
        {
            if (MatchState == newState) return;

            EMatchState oldState = MatchState;
            MatchState = newState;

            // [日志系统融合]
            Log.D($"[GameState] 比赛阶段切换: {oldState} -> {newState}");

            // [事件系统融合] 组装信件并寄出！UI 监听此事件即可，彻底告别 Update
            MatchStateChangedEventArgs evt = new MatchStateChangedEventArgs()
            {
                OldState = oldState,
                NewState = newState
            };
            EventSystem.Instance.Trigger(evt);
        }
    }
}
