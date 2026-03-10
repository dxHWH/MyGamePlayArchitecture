using UnityEngine;
using UnityEngine.InputSystem;
using GamePlayArchitecture;

// 继承底层基类管理输入，并挂载业务层的阵营接口
public class SoulPlayerController : AUnityController, IFactionMember
{
    // 实现接口：玩家生来就是 Player 阵营
    public EFaction FactionId { get; set; } = EFaction.Player;

    public override void Tick(float deltaTime)
    {
        // 1. 调用基类的 Tick 处理输入读取
        base.Tick(deltaTime);

        // 如果没有肉体，或者时间暂停，不能思考和释放技能
        if (Time.timeScale == 0f || ControlledPawn == null) return;

        // 2. 夺舍专属逻辑：鼠标左键发射射线
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ExecuteSoulTransfer();
        }
    }

    // 【多态的威力】：拦截基类的默认平移，把移动指令下达给有数值差异的 CombatPawn
    protected override void HandleMovement(Vector2 input, float deltaTime)
    {
        if (ControlledPawn is CombatPawn combatPawn)
        {
            Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;
            combatPawn.Move(moveDir, deltaTime);
        }
    }

    private void ExecuteSoulTransfer()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CombatPawn targetPawn = hit.collider.GetComponent<CombatPawn>();

            // 规则验证：点中的是合法的肉体、不是自己现在的肉体、并且对方还活着
            if (targetPawn != null && targetPawn != ControlledPawn && targetPawn.Controller != null)
            {
                // 核心阵营判断：看看目标体内的灵魂有没有签“阵营协议”
                if (targetPawn.Controller is IFactionMember targetSoul)
                {
                    // 只有阵营不同，才能夺舍！
                    if (targetSoul.FactionId != this.FactionId)
                    {
                        // 1. 霸道剥夺：踢出目标体内原有的敌对灵魂
                        targetPawn.Controller.UnPossess();

                        // 2. 灵魂转移：注入新身体！
                        this.Possess(targetPawn);

                        // 3. 【完美契合新框架】：通过 World 枢纽，安全、规范地获取当前局的专属计分板
                        if (World.HasInstance && World.Instance.GameState is SurvivalGameState gameState)
                        {
                            gameState.AddScore(EFaction.Player, 1);
                            Log.N($"<color=green>夺舍成功！当前世界总分已更新。</color>");
                        }
                    }
                }
            }
        }
    }
}