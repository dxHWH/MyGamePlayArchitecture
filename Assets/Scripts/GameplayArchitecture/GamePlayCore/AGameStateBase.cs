using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 最轻量级的游戏状态基类，只管基础数据
    /// </summary>
    public class AGameStateBase : AActor
    {
        // 游戏自启动以来的运行时间（供全局访问）
        public float ServerWorldTimeSeconds { get; protected set; }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            ServerWorldTimeSeconds += deltaTime;
        }
    }
}