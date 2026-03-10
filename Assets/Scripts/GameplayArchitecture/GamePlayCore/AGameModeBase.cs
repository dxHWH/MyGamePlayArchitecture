using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    /// <summary>
    /// 最轻量级的模式基类，负责实体生成和基础流程
    /// </summary>
    public class AGameModeBase : AActor
    {
        public AGameStateBase GameState { get; protected set; }

        [Header("Default Classes")]
        public APawn DefaultPawnClass;
        public AController DefaultControllerClass;

        public override void BeginPlay()
        {
            base.BeginPlay();
            //一定要先注册自身，再去注册GameState
            World.RegisterGameMode(this);
            InitGameState();
            StartPlay();
            
        }

        protected virtual void InitGameState()
        {
            //旧版本，不保险的方法
            GameState = FindObjectOfType<AGameStateBase>();
            if (GameState == null)
            {
                GameObject gsObj = new GameObject("GameStateBase");
                GameState = gsObj.AddComponent<AGameStateBase>();
            }
            Log.N("[GameModeBase] 基础计分板已就位");
        }

        public virtual void StartPlay()
        {
            Log.N("[GameModeBase] 游戏基础流程开始");
            // 简单单机游戏在这里直接生成 DefaultPawnClass 即可
        }
    }
}
