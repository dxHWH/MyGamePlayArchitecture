using UnityEngine;

namespace GamePlayArchitecture
{
    public class AActor : MonoBehaviour
    {
        // 标记是否已经初始化
        public bool HasBegunPlay { get; private set; } = false;

        // --- Unity 底层接管区 ---

        protected virtual void Start()
        {
            // 游戏逻辑开始时，注册自己到世界
            World.RegisterAActor(this);
        }

        protected virtual void OnDestroy()
        {
            // 临死前从世界注销
            World.UnregisterAActor(this);
        }

        // --- 我们定义的 UE 风格生命周期 ---

        /// <summary>
        /// 替代 Start()。
        /// 当 World 决定这一帧游戏开始，或者物体生成完毕后调用。
        /// </summary>
        public virtual void BeginPlay()
        {
            HasBegunPlay = true;
            // 子类在这里写初始化逻辑
        }

        /// <summary>
        /// 替代 Update()。
        /// 由 World 统一驱动。
        /// </summary>
        /// <param name="deltaTime">这一帧的时间增量</param>
        public virtual void Tick(float deltaTime)
        {
            // 子类在这里写每帧逻辑
        }
    }
}