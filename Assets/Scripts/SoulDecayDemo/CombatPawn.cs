using UnityEngine;
using GamePlayArchitecture;

public class CombatPawn : APawn
{
    [Header("肉体属性")]
    public float MoveSpeed = 5f;
    public float MaxLifespan = 5f; // 肉体保质期

    private Renderer _renderer;
    private TimerHandle _decayTimer; // 寿命倒计时把柄

    public override void BeginPlay()
    {
        base.BeginPlay();
        _renderer = GetComponent<Renderer>();
    }

    public void Move(Vector3 direction, float deltaTime)
    {
        transform.Translate(direction * MoveSpeed * deltaTime, Space.World);
    }

    // 变色逻辑升级：基于接口阵营的解耦变色
    protected override void OnPossess(AController newController)
    {
        base.OnPossess(newController);

        if (newController is IFactionMember factionMember)
        {
            switch (factionMember.FactionId)
            {
                case EFaction.Player:
                    _renderer.material.color = Color.cyan;
                    // 【严格遵守参数列表】：这里正好利用前两个参数 duration 和 onComplete
                    _decayTimer = TimerSystem.Instance.CreateTimer(MaxLifespan, OnBodyExploded);
                    Log.N($"<color=cyan>成功夺舍！肉体剩余寿命：{MaxLifespan}秒，快寻找下一个目标！</color>");
                    break;
                case EFaction.RedAI:
                    _renderer.material.color = Color.red;
                    break;
                case EFaction.BlueAI:
                    _renderer.material.color = Color.blue;
                    break;
            }
        }
    }

    protected override void OnUnPossess()
    {
        base.OnUnPossess();

        if (TimerSystem.HasInstance && _decayTimer != TimerHandle.Invalid)
        {
            TimerSystem.Instance.StopTimer(_decayTimer);
        }

        _renderer.material.color = Color.black;
        gameObject.name = "Dead Body";

        Collider col = GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    private void OnBodyExploded()
    {
        Log.N("<color=red>时间到！肉体无法承受灵魂的能量，爆炸了！GAME OVER！</color>");
        Destroy(gameObject);
    }

    // 【核心爽快机制】：大鱼吃小鱼的物理撞击真实加分
    private void OnCollisionEnter(Collision collision)
    {
        if (Controller is IFactionMember mySoul && mySoul.FactionId == EFaction.Player)
        {
            CombatPawn otherPawn = collision.gameObject.GetComponent<CombatPawn>();

            if (otherPawn != null && this.transform.localScale.x > otherPawn.transform.localScale.x)
            {
                Log.N("<color=yellow>碾碎了一个低级躯体！得分 +1</color>");

                Destroy(otherPawn.gameObject);

                // 接入计分板：向世界报告玩家得分！
                if (World.HasInstance && World.Instance.GameState is SurvivalGameState gameState)
                {
                    gameState.AddScore(EFaction.Player, 1);
                }
            }
        }
    }
}