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

            Log.D($"[GameState] 比赛阶段切换: {oldState} -> {newState}");

            // [事件系统] 组装信件并寄出！UI 监听此事件即可，彻底告别 Update
            MatchStateChangedEventArgs evt = new MatchStateChangedEventArgs()
            {
                OldState = oldState,
                NewState = newState
            };
            EventSystem.Instance.Trigger(evt);
        }


        // 【核心防线】：利用基类的 OnDestroy 进行自清理
        protected override void OnDestroy()
        {
            // 注意这里依然要用 HasInstance 防御“幽灵单例”报错！
            if (World.HasInstance)
            {
                World.Instance.UnRegisterGameState(this);
            }
            // 先注销world对GameState的绑定，再注销actor
            base.OnDestroy();
        }
    }
}
