using UnityEngine;
using GamePlayArchitecture;
using System;
using Random = UnityEngine.Random;

public class SurvivalGameMode : AGameMode
{
    // 【新架构红利】：只需声明类型，父类会自动完成生成和 World 担保注册！
    public override Type GameStateClass => typeof(SurvivalGameState);

    // 方便内部获取
    public new SurvivalGameState GameState => base.GameState as SurvivalGameState;

    private Action _spawnAIAction;

    public override void BeginPlay()
    {
        base.BeginPlay();
        Log.N("<color=yellow>【夺舍生存战】开始！不断更换肉体活下去！</color>");

        _spawnAIAction = SpawnRandomAI;
        StartMatch(); // 切换到进行中状态

        CombatPawn playerBody = CreatePawn(Vector3.zero, isHeavy: false);
        SoulPlayerController playerSoul = new GameObject("Player_Controller").AddComponent<SoulPlayerController>();

        // 阵营赋予
        playerSoul.FactionId = EFaction.Player;
        playerSoul.Possess(playerBody);

        // 对齐你 TimerSystem 的签名，使用具名参数避免错位！
        TimerSystem.Instance.CreateTimer(
            duration: 3.0f,
            onComplete: _spawnAIAction,
            timerName: "AISpawner",
            isLoop: true
        );
    }

    private void SpawnRandomAI()
    {
        // 比赛结束后停止刷怪
        if (base.GameState != null && ((SurvivalGameState)base.GameState).MatchState == AGameState.EMatchState.WaitingPostMatch)
            return;

        Vector3 randomPos = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        bool isHeavy = Random.value > 0.5f;

        CombatPawn aiBody = CreatePawn(randomPos, isHeavy);
        SimpleAIController aiSoul = new GameObject("AI_Controller").AddComponent<SimpleAIController>();

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
            pawn.MaxLifespan = 30f;
        }

        return pawn;
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