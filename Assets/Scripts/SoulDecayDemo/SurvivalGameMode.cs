using GamePlayArchitecture;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SurvivalGameMode : AGameMode
{
    // 需声明类型，父类会自动完成生成和 World 担保注册！
    public override Type GameStateClass => typeof(SurvivalGameState);

    //定义对象池
    private ObjectPool<GameObjectWrapper> _heavyPawnPool;
    private ObjectPool<GameObjectWrapper> _lightPawnPool;
    private ObjectPool<GameObjectWrapper> _aiControllerPool;
    //记录字典，方便回收时找到对应的池子
    private Dictionary<GameObject, PoolObject<GameObjectWrapper>> _activeWrappers = new Dictionary<GameObject, PoolObject<GameObjectWrapper>>();

    // 方便内部获取
    public new SurvivalGameState GameState => base.GameState as SurvivalGameState;

    private Action _spawnAIAction;

    public override void BeginPlay()
    {
        base.BeginPlay();
        Log.N("<color=yellow>【夺舍生存战】开始！不断更换肉体活下去！</color>");

        // 初始化对象池容量
        //_heavyPawnPool = new ObjectPool<GameObjectWrapper>(20);
        //_lightPawnPool = new ObjectPool<GameObjectWrapper>(20);
        //_aiControllerPool = new ObjectPool<GameObjectWrapper>(20);

        _spawnAIAction = SpawnRandomAI;
        StartMatch(); // 切换到进行中状态

        CombatPawn playerBody = CreatePawn(Vector3.zero, isHeavy: false);
        SoulPlayerController playerSoul = new GameObject("Player_Controller").AddComponent<SoulPlayerController>();

        // 阵营赋予
        playerSoul.FactionId = EFaction.Player;
        playerSoul.Possess(playerBody);

        // 对齐 TimerSystem 的签名，使用具名参数避免错位
        TimerSystem.Instance.CreateTimer(
            duration: 3.0f,
            onComplete: _spawnAIAction,
            timerName: "AISpawner",
            isLoop: true
        );
    }

    /// --- 通过对象池获取肉体 ---
    private CombatPawn GetOrCreatePawn(Vector3 position, bool isHeavy)
    {
        var pool = isHeavy ? _heavyPawnPool : _lightPawnPool;

        if (pool.Acquire(out var wrapper))
        {
            // 如果池子里取出来的这个 wrapper 是第一次用，它里面的 Instance 是 null，需要初始化
            if (wrapper.elem.Instance == null)
            {
                GameObject bodyObj = isHeavy ? GameObject.CreatePrimitive(PrimitiveType.Cube) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Rigidbody rb = bodyObj.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                CombatPawn p = bodyObj.AddComponent<CombatPawn>();

                if (isHeavy)
                {
                    p.transform.localScale = Vector3.one * 1.5f;
                    p.MoveSpeed = 3f;
                    p.MaxLifespan = 8f;
                }
                else
                {
                    p.transform.localScale = Vector3.one * 0.8f;
                    p.MoveSpeed = 8f;
                    p.MaxLifespan = 4.5f;
                }
                wrapper.elem.Init(bodyObj);
            }

            // 获取出来后，重置位置和状态
            GameObject go = wrapper.elem.Instance;
            go.transform.position = position;
            go.name = isHeavy ? "HeavyPawn" : "LightPawn"; // 恢复名字（因为可能死的时候被改成了 Dead Body）

            // 极其重要：确保碰撞体在被回收剥夺后，能够重新加回来
            if (go.GetComponent<Collider>() == null)
            {
                if (isHeavy) go.AddComponent<BoxCollider>();
                else go.AddComponent<SphereCollider>();
            }

            // 登记这具肉体对应的池子票据
            _activeWrappers[go] = wrapper;

            return go.GetComponent<CombatPawn>();
        }
        return null;
    }

    // --- 通过对象池获取灵魂 ---
    private SimpleAIController GetOrCreateAIController()
    {
        if (_aiControllerPool.Acquire(out var wrapper))
        {
            if (wrapper.elem.Instance == null)
            {
                GameObject aiObj = new GameObject("AI_Controller");
                aiObj.AddComponent<SimpleAIController>();
                wrapper.elem.Init(aiObj);
            }

            GameObject go = wrapper.elem.Instance;
            _activeWrappers[go] = wrapper;
            return go.GetComponent<SimpleAIController>();
        }
        return null;
    }

    private void SpawnRandomAI()
    {
        // 比赛结束后停止刷怪
        if (base.GameState != null && ((SurvivalGameState)base.GameState).MatchState == AGameState.EMatchState.WaitingPostMatch)
            return;

        Vector3 randomPos = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        bool isHeavy = Random.value > 0.5f;

        //凭空创建的逻辑
        CombatPawn aiBody = CreatePawn(randomPos, isHeavy);
        SimpleAIController aiSoul = new GameObject("AI_Controller").AddComponent<SimpleAIController>();

        //使用对象池
        //CombatPawn aiBody = GetOrCreatePawn(randomPos, isHeavy);
        //SimpleAIController aiSoul = GetOrCreateAIController();

        // 阵营赋予：50%概率是红队，50%概率是蓝队
        aiSoul.FactionId = Random.value > 0.5f ? EFaction.RedAI : EFaction.BlueAI;

        aiSoul.Possess(aiBody);
    }

    // 完整的数据驱动工厂方法
    private CombatPawn CreatePawn(Vector3 position, bool isHeavy)
    {
        GameObject bodyObj = isHeavy
            ? GameObject.CreatePrimitive(PrimitiveType.Cube)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        bodyObj.transform.position = position;

        Rigidbody rb = bodyObj.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        CombatPawn pawn = bodyObj.AddComponent<CombatPawn>();

        if (isHeavy)
        {
            pawn.transform.localScale = Vector3.one * 1.5f;
            pawn.MoveSpeed = 3f;
            pawn.MaxLifespan = 30f;
        }
        else
        {
            pawn.transform.localScale = Vector3.one * 0.8f;
            pawn.MoveSpeed = 8f;
            pawn.MaxLifespan = 5f;
        }

        return pawn;
    
    
    }
    // --- 【新增】：开放给外界的统一回收接口 ---
    public void RecycleObject(GameObject go, bool isHeavyPawn)
    {
        if (_activeWrappers.TryGetValue(go, out var wrapper))
        {
            if (go.GetComponent<SimpleAIController>() != null)
            {
                _aiControllerPool.Recycle(ref wrapper);
            }
            else
            {
                var pool = isHeavyPawn ? _heavyPawnPool : _lightPawnPool;
                pool.Recycle(ref wrapper);
            }
            _activeWrappers.Remove(go);
        }
        else
        {
            // 如果不在池子里，就直接销毁兜底
            Destroy(go);
        }
    }


    // AGameMode 中有此方法，用于结束比赛
    public override void EndMatch()
    {
        base.EndMatch();
        Log.N("<color=red>[SurvivalGameMode] 比赛结束！停止刷怪。</color>");

        if (TimerSystem.HasInstance)
        {
            TimerSystem.Instance.StopTimer("AISpawner");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (TimerSystem.HasInstance)
        {
            TimerSystem.Instance.StopTimer("AISpawner");
        }
    }
}