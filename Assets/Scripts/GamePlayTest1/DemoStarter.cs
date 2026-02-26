using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamePlayArchitecture;

public class DemoStarter : MonoBehaviour
{
    public AUnityController playerController;
    public MyHeroPawn playerPawn;

    void Start()
    {
        // 延迟 0.1 秒执行，等待所有的 AActor 向 World 报到完毕并执行完 BeginPlay
        Invoke(nameof(StartPossess), 0.1f);
    }

    void StartPossess()
    {
        if (playerController != null && playerPawn != null)
        {
            Debug.Log("【上帝】游戏初始化完毕，灵魂开始注入肉体！");
            // 核心神技：附身！
            playerController.Possess(playerPawn);
        }
    }
}