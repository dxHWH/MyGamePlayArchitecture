namespace GamePlayArchitecture
{
    public class GameInstance : MonoSingleton<GameInstance>
    {
        private void Awake()
        {
            // 加上这一句！告诉 Unity：切换场景时，绝对不要销毁挂着这个脚本的全局大管家！
            DontDestroyOnLoad(this.gameObject);

#if ENABLE_LOGSAVE
            Log.InitLogSave();
#endif
            Log.N("[GameInstance] 全局游戏实例已启动，准备掌控所有场景！");
        }

        // 【新增】：用属性动态获取当前的 World，而不是在 Awake 里写死引用
        // 这样无论怎么切场景，都能拿到当前场景最新鲜的 World
        public World CurrentWorld => World.Instance;
    }
}