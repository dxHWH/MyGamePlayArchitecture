using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamePlayArchitecture;

public class MyHeroPawn : APawn
{
    private Renderer _renderer;

    public override void BeginPlay()
    {
        base.BeginPlay();
        _renderer = GetComponent<Renderer>();
        // 初始状态是灰色的（没有灵魂）
        _renderer.material.color = Color.grey;
    }

    // 当灵魂进入身体那一刻
    protected override void OnPossess(AController newController)
    {
        base.OnPossess(newController);
        Debug.Log($"<color=green>【肉体】我被灵魂 {newController.name} 附身了！我可以动了！</color>");
        // 变红，表示激活
        _renderer.material.color = Color.red;
    }

    // 当灵魂离开身体那一刻
    protected override void OnUnPossess()
    {
        base.OnUnPossess();
        Debug.Log($"<color=grey>【肉体】灵魂离开了，我变成了植物人...</color>");
        // 变回灰色
        _renderer.material.color = Color.grey;
    }
}
