namespace GamePlayArchitecture
{
    public class GameInstance : MonoSingleton<GameInstance>
{
        public World world;
        private void Awake()
        {
            // 加上这一句！告诉 Unity：切换场景时，绝对不要销毁挂着这个脚本的木头人！
            DontDestroyOnLoad(this.gameObject);
#if ENABLE_LOGSAVE
            Log.InitLogSave();
#endif
            world = World.Instance;
        }
    }
}

