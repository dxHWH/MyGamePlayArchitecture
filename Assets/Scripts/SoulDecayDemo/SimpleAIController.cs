using UnityEngine;
using GamePlayArchitecture;

public class SimpleAIController : AController, IFactionMember
{
    // 实现接口：默认是中立，GameMode 会在生成它时给它重新赋值为 RedAI 或 BlueAI
    public EFaction FactionId { get; set; } = EFaction.Neutral;

    private float _randomOffset;

    public override void BeginPlay()
    {
        base.BeginPlay();
        // 给柏林噪声一个随机种子，保证每个 AI 走的路线不一样
        _randomOffset = Random.Range(0f, 100f);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        // 如果没有肉体，或者时间暂停，AI 停止思考
        if (Time.timeScale == 0f || ControlledPawn == null) return;

        // 使用柏林噪声生成一个平滑的随机移动方向，模拟瞎逛
        float x = Mathf.PerlinNoise(Time.time * 0.5f, _randomOffset) * 2 - 1;
        float z = Mathf.PerlinNoise(_randomOffset, Time.time * 0.5f) * 2 - 1;

        // 驱动肉体移动
        if (ControlledPawn is CombatPawn combatPawn)
        {
            combatPawn.Move(new Vector3(x, 0, z).normalized, deltaTime);
        }
    }
}