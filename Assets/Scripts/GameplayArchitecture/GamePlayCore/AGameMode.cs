using UnityEngine;
using System;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 包含复杂比赛状态机的模式类。
    /// 仅定义状态与流程框架，具体流转条件（如倒计时）由子类实现。
    /// </summary>
    public class AGameMode : AGameModeBase
    {
        public new AGameState GameState => base.GameState as AGameState;

        public virtual Type GameStateClass => typeof(AGameState);

        override protected void InitGameState()
        {
            // 1. 获取应该生成的具体类型
            Type stateType = GameStateClass;

            // 2. 动态生成物体
            GameObject gsObj = new GameObject($"GameState_{this.GetType().Name}");

            // 3. 使用反射版的 AddComponent，动态挂载对应的子类计分板！
            base.GameState = gsObj.AddComponent(stateType) as AGameState;

            // 4. 权威担保：裁判亲自去世界中心注册！
            if (World.HasInstance)
            {
                World.Instance.RegisterGameState(this, GameState);
            }

            // 5. 动态日志，谁调用的打印谁的名字
            Log.N($"[{this.GetType().Name}] 专属计分板 ({stateType.Name}) 已就位，并由 GameMode 担保注册完毕！");
        }

        public override void StartPlay()
        {
            base.StartPlay();

            // 基类只负责切入基础状态，不负责具体的计时逻辑
            if (GameState != null)
            {
                GameState.SetMatchState(AGameState.EMatchState.WaitingToStart);
            }
            Log.N("[GameMode] 进入等待阶段...");
        }

        // 提供给子类调用的正式开始接口
        protected virtual void StartMatch()
        {
            Log.N("[GameMode] 比赛正式开始！");

            if (GameState != null)
            {
                GameState.SetMatchState(AGameState.EMatchState.InProgress);
            }
        }

        override public  void EndMatch()
        {
            Log.N("[GameMode] 比赛结束，准备结算！");

            if (GameState != null)
            {
                GameState.SetMatchState(AGameState.EMatchState.WaitingPostMatch);
            }
        }
    }
}