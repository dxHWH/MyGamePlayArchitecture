using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlayArchitecture
{
    public class AUnityController : AController
    {
        private GameInput _inputControls;
        public InputAction _moveAction;
        public override void BeginPlay()
        {
            base.BeginPlay();
            // 1. 初始化输入实例
            _inputControls = new GameInput();
            // 2. 获取具体的 Action
            _moveAction = _inputControls.Player.Move;

            // 3. 启用输入 map
            _inputControls.Player.Enable();
            Log.D("已注册输入系统");
        }

        protected override void OnDestroy()
        {
            if (_inputControls != null)
            {
                _inputControls.Player.Disable();
            }
            base.OnDestroy();
        }

        public override void Tick(float deltaTime)
        {
            // 如果没有控制任何 APawn，就没有必要处理移动逻辑
            if (ControlledPawn == null) return;

            // 1. 从 Input System 读取值
            Vector2 inputVector = _moveAction.ReadValue<Vector2>();

            // 2. 如果有输入，处理逻辑
            if (inputVector.sqrMagnitude > 0.01f)
            {
                HandleMovement(inputVector, deltaTime);
            }
        }

        private void HandleMovement(Vector2 input, float deltaTime)
        {
            // 将 2D 输入转换为 3D 移动方向 (假设 Y 是向上)
            Vector3 moveDir = new Vector3(input.x, 0, input.y);

            // 直接驱动 APawn 的 Transform
            // 注意：这里以后应该调用 APawn.AddMovementInput()，现在暂时直接动 Transform
            ControlledPawn.transform.Translate(moveDir * 5f * deltaTime, Space.World);
        }


    }
}

