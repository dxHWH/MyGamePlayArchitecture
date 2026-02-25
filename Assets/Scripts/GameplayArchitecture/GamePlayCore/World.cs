using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    public class World : MonoSingleton<World>
    {
        // 所有的AActor名单
        private List<AActor> _actors = new List<AActor>();
        private List<AActor> _pendingAActors = new List<AActor>();

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
                        _actors.Add(newAActor);
                        newAActor.BeginPlay();
                    }
                }
            }

            // 2. 驱动所有 AActor 的 Tick
            float dt = Time.deltaTime;

            // 倒序遍历，安全删除
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

        // --- 静态 API ---

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