using UnityEngine;
using GamePlayArchitecture; // 引用命名空间

public class TestPossess : MonoBehaviour
{
    public AUnityController playerController;
    public APawn pawn1; // 红块
    public APawn pawn2; // 蓝球

    public void Start()
    {
        GameObject controllerObj = new GameObject("PlayerController");
        playerController = controllerObj.AddComponent<AUnityController>();
        Log.D("111");
    }

    public void Update()
    {
        var moveInput = playerController._moveAction.ReadValue<Vector2>();
        if (moveInput.x > 0)
        {
            playerController.Possess(pawn1);
        }

        if (moveInput.y > 0)
        {
            playerController.Possess(pawn2);
        }

        if (moveInput.x == 0 && moveInput.y == 0)
        {
            playerController.UnPossess();
        }
    }

}