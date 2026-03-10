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

    // 暴露给 Controller 调用的移动接口，参数由 World 的 Tick 传递过来
    public void Move(Vector3 direction, float deltaTime)
    {
        transform.Translate(direction * MoveSpeed * deltaTime, Space.World);
    }

    // 【核心架构威力】：感受到灵魂注入时的反应
    protected override void OnPossess(AController newController)
    {
        base.OnPossess(newController);

        if (newController is SoulPlayerController)
        {
            _renderer.material.color = Color.cyan; // 玩家附身发蓝光

            // 【核心玩法】：玩家一附身，死神就开始倒数！
            _decayTimer = TimerSystem.Instance.CreateTimer(MaxLifespan, OnBodyExploded);
            Log.N($"<color=cyan>成功夺舍！肉体剩余寿命：{MaxLifespan}秒，快寻找下一个目标！</color>");
        }
        else
        {
            _renderer.material.color = Color.red; // AI附身发红光
        }
    }

    protected override void OnUnPossess()
    {
        base.OnUnPossess();

        // 玩家抽离灵魂，掐死自爆倒计时（注意我们上节课加的 HasInstance 护城河！）
        if (TimerSystem.HasInstance && _decayTimer != null)
        {
            TimerSystem.Instance.StopTimer(_decayTimer);
        }

        _renderer.material.color = Color.black;
        gameObject.name = "Dead Body";

        // 剥夺碰撞体，变成一具纯背景尸体
        Collider col = GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    private void OnBodyExploded()
    {
        Log.N("<color=red>时间到！肉体无法承受灵魂的能量，爆炸了！GAME OVER！</color>");
        Destroy(gameObject);
        // 稍后我们会在这里加上弹窗或重新开始的逻辑
    }

    // 大鱼吃小鱼的撞击逻辑
    private void OnCollisionEnter(Collision collision)
    {
        if (Controller is SoulPlayerController) // 只有玩家能碾碎别人
        {
            CombatPawn otherPawn = collision.gameObject.GetComponent<CombatPawn>();
            // 如果我是大方块(重装)，对方是小球(敏捷)，直接碾碎对方！
            if (otherPawn != null && this.transform.localScale.x > otherPawn.transform.localScale.x)
            {
                Log.N("<color=yellow>碾碎了一个低级躯体！得分 +1</color>");
                Destroy(otherPawn.gameObject);
            }
        }
    }
}