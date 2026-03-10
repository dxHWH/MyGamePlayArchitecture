namespace GamePlayArchitecture
{
    public class GameInstance : MonoSingleton<GameInstance>
    {
        // 注意：这里要改成 protected override void Awake()
        protected override void Awake()
        {
            base.Awake(); // 【极其重要】：先调基类，如果我是多余的，基类会把我杀掉！

            // 防御性编程：如果基类发现我是多余的，已经调了 Destroy，后续代码就不应该再执行了
            if (this == null || Instance != this) return;

            // 只有合法的正牌单例，才有资格挂载全局免死金牌
            DontDestroyOnLoad(this.gameObject);

#if ENABLE_LOGSAVE
            Log.InitLogSave();
#endif
            Log.N("[GameInstance] 全局游戏实例已启动，准备掌控所有场景！");
        }

        public World CurrentWorld => World.Instance;
    }
}