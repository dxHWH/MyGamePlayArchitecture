using UnityEngine;
using GamePlayArchitecture;

public class GameObjectWrapper : IPoolable
{
    public GameObject Instance { get; private set; }

    public void Init(GameObject go)
    {
        Instance = go;
    }

    public void OnSpawn()
    {
        if (Instance != null)
        {
            Instance.SetActive(true);

            // 【核心】：出池时，恢复 World 的 Tick 驱动
            AActor actor = Instance.GetComponent<AActor>();
            if (actor != null)
            {
                actor.SetActorTickEnabled(true);
            }
        }
    }

    public void OnRecycle()
    {
        if (Instance != null)
        {
            // 【核心】：入池时，掐断 World 的 Tick 驱动，防止在后台空转！
            AActor actor = Instance.GetComponent<AActor>();
            if (actor != null)
            {
                actor.SetActorTickEnabled(false);
            }

            Instance.SetActive(false);

            // 可选：重置物理速度，防止下次出池时乱飞
            Rigidbody rb = Instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}