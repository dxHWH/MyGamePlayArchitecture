using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    //CRTP（Curiously Recurring Template Pattern，奇异递归模板模式
    public class World : MonoSingleton<World>
    {
        // ================= 添加部分 =================
        /// <summary>
        /// 当前世界的最高权限规则裁判
        /// </summary>
        public AGameModeBase AuthorityGameMode { get; private set; }
        // ============================================

        // 所有的AActor名单
        // 双缓冲思想 新产生的AActor放入_pendingAActors中
        private List<AActor> _actors = new List<AActor>();
        private List<AActor> _pendingAActors = new List<AActor>();

        private void Awake()
        {
            // ================= 添加部分 =================
            // 在世界诞生时，寻找场景中配置的 GameMode
            AuthorityGameMode = GameObject.FindObjectOfType<AGameModeBase>();
            if (AuthorityGameMode != null)
            {
                // 【修正】使用 Log.N 替代 Log.I
                Log.N($"[World] 已挂载当前关卡的游戏模式: {AuthorityGameMode.GetType().Name}");
            }
            else
            {
                // 【修正】使用 Log.E 替代 Log.W，因为缺少 GameMode 是个严重的架构缺失
                Log.E("[World] 警告：当前场景未找到任何继承自 AGameModeBase 的游戏模式！");
            }
            // ============================================
        }

        private void Start()
        {
            // 游戏启动：通知场景中现存的 AActor
            // 复制一份列表以防在 BeginPlay 中修改了 _actors
            var actorsToBegin = new List<AActor>(_actors);
            foreach (var actor in actorsToBegin)
            {
                actor.BeginPlay();
            }
        }

        private void Update()
        {
            // 1. 处理新增的 AActor (如有)
            if (_pendingAActors.Count > 0)
            {
                // 拷贝一份防止遍历时修改
                var newAActors = new List<AActor>(_pendingAActors);
                _pendingAActors.Clear();

                foreach (var newAActor in newAActors)
                {
                    if (!_actors.Contains(newAActor))
                    {
                        _actors.Add(newAActor); //现有名单不包含新产生的actor,更新到名单去
                        newAActor.BeginPlay();  //加入到名单时，开始执行游戏逻辑。
                    }
                }
            }

            // 2. 驱动所有 AActor 的 Tick
            float dt = Time.deltaTime;

            // 倒序遍历，安全删除 //保证补位索引的元素是已经被遍历过的元素，不会漏读
            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                if (_actors[i] == null)
                {
                    _actors.RemoveAt(i);
                    continue;
                }

                // 只有已经 BeginPlay 的 AActor 才执行 Tick
                if (_actors[i].HasBegunPlay)
                {
                    _actors[i].Tick(dt);
                }
            }
        }

        // --- 静态 API --- 外观模式（Facade）思想

        public static void RegisterAActor(AActor actor)
        {
            // 只有 World 存在时才注册
            if (Instance == null) return;

            // 放入 pending 列表等待下一帧处理，避免在 foreach 循环中修改列表
            if (!Instance._actors.Contains(actor) && !Instance._pendingAActors.Contains(actor))
            {
                Instance._pendingAActors.Add(actor);
            }
        }

        //注销actor
        public static void UnregisterAActor(AActor actor)
        {
            if (Instance == null) return;

            if (Instance._actors.Contains(actor))
            {
                Instance._actors.Remove(actor);
            }
            if (Instance._pendingAActors.Contains(actor))
            {
                Instance._pendingAActors.Remove(actor);
            }
        }

        //析构
        protected override void OnDestroy()
        {
            _actors.Clear();
            _pendingAActors.Clear();
            _actors = null;
            _pendingAActors = null;
            base.OnDestroy();
        }
    }
}