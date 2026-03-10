using UnityEngine;
using UnityEngine.InputSystem;
using GamePlayArchitecture;

// 继承底层基类，并挂载业务层的阵营接口
public class SoulPlayerController : AUnityController, IFactionMember
{
    // 实现接口：玩家生来就是 Player 阵营
    public EFaction FactionId { get; set; } = EFaction.Player;

    public override void Tick(float deltaTime)
    {
        // 1. 调用基类的 Tick 处理输入读取
        base.Tick(deltaTime);

        // 如果没有肉体，或者时间暂停，不能思考
        if (Time.timeScale == 0f || ControlledPawn == null) return;

        // 2. 夺舍专属逻辑：鼠标左键发射射线
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ExecuteSoulTransfer();
        }
    }

    // 拦截基类的默认平移，把移动指令下达给有数值差异的 CombatPawn
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

            if (targetPawn != null && targetPawn != ControlledPawn && targetPawn.Controller != null)
            {
                if (targetPawn.Controller is IFactionMember targetSoul)
                {
                    if (targetSoul.FactionId != this.FactionId)
                    {
                        // 霸道剥夺
                        targetPawn.Controller.UnPossess();

                        // 灵魂转移
                        this.Possess(targetPawn);

                        // 【核心修正】：通过 World 枢纽，安全、规范地获取当前局的计分板
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