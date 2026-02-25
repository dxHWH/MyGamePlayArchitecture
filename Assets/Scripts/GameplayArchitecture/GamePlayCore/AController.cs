using UnityEngine;

namespace GamePlayArchitecture
{
    public class AController : AActor
    {
        // 我当前控制着谁？
        public APawn ControlledPawn { get; private set; }

        // --- 核心机制：附身 ---

        /// <summary>
        /// 核心方法：去控制一个 APawn
        /// </summary>
        public void Possess(APawn pawnToPossess)
        {
            if (pawnToPossess == null) return;
            if (ControlledPawn == pawnToPossess) return; // 已经在控制它了

            // 1. 如果我现在控制着别人，先抛弃它
            if (ControlledPawn != null)
            {
                UnPossess();
            }

            // 2. 如果那个 APawn 已经被别人控制了，把那个人踢掉 (可选，UE默认会踢掉)
            if (pawnToPossess.Controller != null)
            {
                pawnToPossess.Controller.UnPossess();
            }

            // 3. 建立连接（双向绑定）
            pawnToPossess.PossessedBy(this);
            ControlledPawn = pawnToPossess;

            // 4. 通知自己（比如切换 UI）
            OnPossess(pawnToPossess);

            Log.D($"{name} 附身控制了 {pawnToPossess.name}");
        }

        /// <summary>
        /// 核心方法：放弃当前的控制权
        /// </summary>
        public void UnPossess()
        {
            if (ControlledPawn == null) return;

            Log.D($"{name} 放弃了 {ControlledPawn.name} 的控制权");

            // 1. 通知 APawn 它自由了
            ControlledPawn.UnPossessed();

            // 2. 通知自己
            OnUnPossess(ControlledPawn);

            // 3. 断开引用
            ControlledPawn = null;
        }

        // --- 回调函数 ---
        protected virtual void OnPossess(APawn pawn) { }
        protected virtual void OnUnPossess(APawn pawn) { }
    }
}