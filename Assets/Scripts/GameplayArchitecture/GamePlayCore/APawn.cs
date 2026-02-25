namespace GamePlayArchitecture
{
    public class APawn : AActor
    {
        // 当前是谁在控制我？（如果是 null，说明我是个没灵魂的尸体/摆件）
        public AController Controller { get; private set; }

        // --- 核心机制：被附身 ---

        /// <summary>
        /// 被控制器附身时调用（底层逻辑）
        /// </summary>
        public void PossessedBy(AController newController)
        {
            Controller = newController;
            OnPossess(newController); // 通知子类
        }

        /// <summary>
        /// 被控制器抛弃时调用（底层逻辑）
        /// </summary>
        public void UnPossessed()
        {
            Controller = null;
            OnUnPossess(); // 通知子类
        }

        // --- 给子类重写的“回调” ---

        // 当灵魂进入身体那一刻（比如：眼睛发光、播放音效）
        protected virtual void OnPossess(AController newController) { }

        // 当灵魂离开身体那一刻（比如：瘫软在地、眼睛熄灭）
        protected virtual void OnUnPossess() { }
    }
}