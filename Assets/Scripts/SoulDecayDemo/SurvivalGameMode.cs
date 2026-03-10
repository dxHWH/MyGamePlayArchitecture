using UnityEngine;
using GamePlayArchitecture;
using System;
using Random = UnityEngine.Random;

public class SurvivalGameMode : AGameMode
{
    private Action _spawnAIAction;

    // 【核心架构威力】：权威生成我们刚刚写好的专属计分板！
    protected override void InitGameState()
    {
        base.InitGameState();
    }

    // 方便我们在 GameMode 内部快速获取强类型计分板
    public new SurvivalGameState GameState => base.GameState as SurvivalGameState;

    public override void BeginPlay()
    {
        base.BeginPlay();
        Log.N("<color=yellow>【夺舍生存战】开始！不断更换肉体活下去！</color>");

        _spawnAIAction = SpawnRandomAI;
        StartMatch(); // 切换到进行中状态

        // 1. 生成玩家的初始肉体（给玩家一个敏捷型的球体起手）
        CombatPawn playerBody = CreatePawn(Vector3.zero, isHeavy: false);
        SoulPlayerController playerSoul = new GameObject("Player_Controller").AddComponent<SoulPlayerController>();

        // 【阵营赋予】：你是玩家！
        playerSoul.FactionId = EFaction.Player;
        playerSoul.Possess(playerBody);

        // 2. 开启每3秒一次的零GC无尽刷怪循环
        TimerSystem.Instance.CreateTimer(
            duration: 3.0f,
            onComplete: _spawnAIAction,
            isLoop: true,
            timerName: "AISpawner"
        );
    }

    private void SpawnRandomAI()
    {
        // 比赛结束后停止刷怪
        if (base.GameState != null && base.GameState.MatchState == AGameState.EMatchState.WaitingPostMatch)
            return;

        Vector3 randomPos = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        bool isHeavy = Random.value > 0.5f;

        CombatPawn aiBody = CreatePawn(randomPos, isHeavy);
        SimpleAIController aiSoul = new GameObject("AI_Controller").AddComponent<SimpleAIController>();

        // 【阵营赋予】：50%概率是红队，50%概率是蓝队
        //aiSoul.FactionId = Random.value > 0.5f ? EFaction.RedAI : EFaction.BlueAI;

        aiSoul.Possess(aiBody);
    }

    // 数据驱动工厂：用代码捏造截然不同的肉体
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
            pawn.MaxLifespan = 8f;
        }
        else
        {
            pawn.transform.localScale = Vector3.one * 0.8f;
            pawn.MoveSpeed = 8f;
            pawn.MaxLifespan = 3.5f;
        }

        return pawn;
    }

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