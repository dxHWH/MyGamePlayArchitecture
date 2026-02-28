using UnityEngine;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 包含复杂比赛状态机的模式类，利用 TimerSystem 实现零GC流程控制
    /// </summary>
    public class AGameMode : AGameModeBase
    {
        // 使用 new 关键字隐藏基类属性，方便获取高级版 GameState
        public new AGameState GameState => base.GameState as AGameState;

        [Header("比赛规则")]
        public float WarmupTime = 3.0f; // 准备阶段倒计时

        protected override void InitGameState()
        {
            base.GameState = FindObjectOfType<AGameState>();
            if (base.GameState == null)
            {
                GameObject gsObj = new GameObject("GameState");
                base.GameState = gsObj.AddComponent<AGameState>();
            }
        }

        public override void StartPlay()
        {
            base.StartPlay();

            // 1. 切入等待阶段（此时 UI 监听到事件，可弹出 "3, 2, 1"）
            GameState?.SetMatchState(AGameState.EMatchState.WaitingToStart);
            Log.N($"[GameMode] 进入等待阶段，{WarmupTime} 秒后正式开始...");

            // 2. 【修复点】调用你框架中真正的 TimerSystem API：CreateTimer
            if (WarmupTime > 0)
            {
                // 参数1: 时长 (WarmupTime)
                // 参数2: 完成时的回调 (StartMatch)
                // (其它的如 interval, isLoop 等参数原作者已设置了恰当的默认值)
                TimerSystem.Instance.CreateTimer(WarmupTime, StartMatch);
            }
            else
            {
                StartMatch();
            }
        }

        protected virtual void StartMatch()
        {
            Log.N("[GameMode] 倒计时结束，正式比赛开始！");

            // 切入进行中阶段（UI 监听到事件，隐藏倒计时，显示血条；此处可执行玩家生成/附身逻辑）
            GameState?.SetMatchState(AGameState.EMatchState.InProgress);

            // 【极其重要】很多时候会忘了写这一行！
            // 比赛正式开始，裁判把玩家放进场！
        }

        public virtual void EndMatch()
        {
            Log.N("[GameMode] 比赛结束，准备结算！");
            GameState?.SetMatchState(AGameState.EMatchState.WaitingPostMatch);
        }
    }
}