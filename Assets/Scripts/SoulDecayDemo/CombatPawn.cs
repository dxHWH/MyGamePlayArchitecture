using UnityEngine;
using GamePlayArchitecture;

public class CombatPawn : APawn
{
    [Header("肉体属性")]
    public float MoveSpeed = 5f;
    public float MaxLifespan = 5f; // 肉体保质期

    private Renderer _renderer;
    private TimerHandle _decayTimer; // 寿命倒计时把柄

    // 1. 将获取引用的逻辑提前到 Awake
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        // 如果有 Rigidbody 或 Collider 的获取，也全放这里
    }

    public override void BeginPlay()
    {
        base.BeginPlay();
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

 // Assets/Scripts/SoulDecayDemo/CombatPawn.cs

protected override void OnUnPossess()
{
    // 获取当前正在离开的控制器引用
    var leavingController = this.Controller;

    base.OnUnPossess();

    // 无论谁离开，都先停止倒计时器（防止 AI 身上如果有计时器产生干扰）
    if (TimerSystem.HasInstance && _decayTimer != TimerHandle.Invalid)
    {
        TimerSystem.Instance.StopTimer(_decayTimer);
        _decayTimer = TimerHandle.Invalid;
    }
    // 只有当离开的灵魂是“玩家”时，才触发枯萎和销毁逻辑
    if (leavingController is SoulPlayerController)
    {
        _renderer.material.color = Color.black;
        gameObject.name = "Dead Body (Player's Old Shell)";

        // 建议：如果你想让尸体还能被撞到，可以用 enabled = false
        // 如果想彻底穿透，就 Destroy 碰撞体
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false; 

        // 物理销毁（回收内存）
        Destroy(gameObject, 3.0f);
    }
    else
    {
        // 如果离开的是 AI（例如正在被玩家夺舍），我们保持身体状态“新鲜”
        // 这样玩家进去后，看到的就是彩色的、有碰撞的正常身体
        Log.N($"AI 灵魂已迁出 {gameObject.name}，等待新主入驻...");
    }
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

                //Destroy(otherPawn.gameObject);
                otherPawn.Die(isExplosion: false);

                // 接入计分板：向世界报告玩家得分！
                if (World.HasInstance && World.Instance.GameState is SurvivalGameState gameState)
                {
                    gameState.AddScore(EFaction.Player, 1);
                }
            }
        }
    }

    public void Die(bool isExplosion = false)
    {
        if (isExplosion)
            Log.N("<color=red>肉体爆炸！</color>");

        // 1. 如果当前有灵魂在驾驶，必须按规矩通知它解绑！
        if (Controller != null)
        {
            Controller.UnPossess();
        }

        // 2. 切断计时器
        if (TimerSystem.HasInstance && _decayTimer != TimerHandle.Invalid)
        {
            TimerSystem.Instance.StopTimer(_decayTimer);
        }

        // 3. 通知裁判终止比赛 (仅当死的是玩家时)
        if (isExplosion && Controller is IFactionMember member && member.FactionId == EFaction.Player)
        {
            // 只有明确查明这具死掉的身体里，装的是玩家的灵魂时，才宣告游戏失败
            World.Instance.AuthorityGameMode.EndMatch();
        }

        // 4. 最后才能体面地销毁自己
        Destroy(gameObject);
    }
}