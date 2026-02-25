namespace GamePlayArchitecture
{
    public class GameInstance : MonoSingleton<GameInstance>
{
        public World world;
        private void Awake()
        {
#if ENABLE_LOGSAVE
            Log.InitLogSave();
#endif
            world = World.Instance;
        }
    }
}

