using UnityEngine;
using GamePlayArchitecture;


public class CombatPawn : APawn
{
    [Header("肉体属性")]
    public float MoveSpeed = 5f;
    public float MaxLifespan = 5f; // 肉体保质期
    bool isDying = false;//爆炸中，防止重复触发死亡逻辑

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
                    //这里正好利用前两个参数 duration 和 onComplete
                    _decayTimer = TimerSystem.Instance.CreateTimer(MaxLifespan, OnBodyExploded);
                    Log.N($"<color=cyan>成功夺舍！肉体剩余寿命：{MaxLifespan}秒，快寻找下一个目标！</color>");
                    // 把这具身体的倒计时票据，通过事件发给 UI
                    if (EventSystem.HasInstance)
                    {
                        EventSystem.Instance.Trigger(new PlayerLifespanTimerEventArgs(_decayTimer));
                    }
                    // -----------------------------------------------------------
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
        if(!isDying)
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
        Die(true);
    }

    // 
    // 大鱼吃小鱼的物理撞击真实加分（全阵营通用）
    private void OnCollisionEnter(Collision collision)
    {
        // 1. 确保自己当前是被灵魂控制的“活体”，死尸不具备主动碾碎别人的能力
        if (Controller is IFactionMember mySoul)
        {
            CombatPawn otherPawn = collision.gameObject.GetComponent<CombatPawn>();

            // 2. 对方必须也是个肉体，且【我的体型必须严格大于对方】
            // 因为碰撞是双向触发的，大小判断保证了只有大的一方会执行这段击杀逻辑
            if (otherPawn != null && this.transform.localScale.x > otherPawn.transform.localScale.x)
            {
                // [可选细节] 友军保护：如果对方也是活物，且跟我是同一阵营，就不互相碾压（防止红队吃红队）
                // 如果你想看纯粹的混沌大乱斗，可以把这行 if 注释掉
                if (otherPawn.Controller is IFactionMember otherSoul && otherSoul.FactionId == mySoul.FactionId)
                {
                    return;
                }

                // 3. 判定受害者身份（如果碾碎的是玩家，后果很严重）
                bool isCrushingPlayer = otherPawn.Controller is IFactionMember victim && victim.FactionId == EFaction.Player;

                if (isCrushingPlayer)
                {
                    Log.N($"<color=red>惨烈！玩家被 {mySoul.FactionId} 碾碎了！</color>");
                    // 【注意】：因为你之前的 Die 方法里规定只有 isExplosion=true 时才会结束游戏
                    // 为了让玩家被碾死时也能触发 Game Over，这里强行传 true
                    otherPawn.Die(isExplosion: true);
                }
                else
                {
                    Log.N($"<color=yellow>{mySoul.FactionId} 碾碎了一个低级躯体！得分 +1</color>");
                    otherPawn.Die(isExplosion: false);
                }

                // 4. 动态接入计分板：谁碾碎的，就给谁加分！
                if (World.HasInstance && World.Instance.GameState is SurvivalGameState gameState)
                {
                    gameState.AddScore(mySoul.FactionId, 1);
                }
            }
        }
    }

    public void Die(bool isExplosion = false)
    {
        isDying = true;

        if (isExplosion)
            Log.N("<color=red>肉体爆炸！</color>");

        // 3. 通知裁判终止比赛 (仅当死的是玩家时)
        if (isExplosion && Controller is IFactionMember member && member.FactionId == EFaction.Player)
        {
            // 只有明确查明这具死掉的身体里，装的是玩家的灵魂时，才宣告游戏失败
            World.Instance.AuthorityGameMode.EndMatch();
        }

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

        // 4. 最后才能体面地销毁自己
        Destroy(gameObject);
    }
}