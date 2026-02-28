using UnityEngine;
using GamePlayArchitecture;

public class DemoHeroPawn : APawn
{
    private Renderer _renderer;

    // 【修改点 1】：利用 Unity 原生的 Awake，确保在 Instantiate 瞬间就拿到组件！
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public override void BeginPlay()
    {
        base.BeginPlay();
        // 此时 _renderer 绝对不会是空了
        if (Controller == null)
        {
            _renderer.material.color = Color.grey;
        }
        //_renderer.material.color = Color.grey;
    }

    protected override void OnPossess(AController newController)
    {
        base.OnPossess(newController);
        Log.N($"<color=green>【DemoPawn】成功被 {newController.name} 附身！激活！</color>");

        // 【修复完毕】：此时 _renderer 早在 Awake 就被赋值了，安全变红！
        if (_renderer != null)
        {
            _renderer.material.color = Color.red;
        }
    }

    protected override void OnUnPossess()
    {
        base.OnUnPossess();
        if (_renderer != null)
        {
            _renderer.material.color = Color.grey;
        }
    }
}