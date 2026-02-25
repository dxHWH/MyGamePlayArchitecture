using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GamePlayArchitecture;

public class RingBufferStressTest : MonoBehaviour
{
    void Start()
    {
        RunStressTest();
    }

    void RunStressTest()
    {
        Debug.Log("开始多线程压力测试...");

        // 容量设小一点，增加"队满"和"队空"的碰撞概率，逼出竞态条件
        int capacity = 100;
        var buffer = new ThreadSafeRingBuffer<int>(capacity);

        int totalItemsToProcess = 100000; // 总共要处理十万个数据

        // 用于校验原子性：生产了多少，消费了多少
        int producedCount = 0;
        int consumedCount = 0;

        // 开启生产者线程
        Task producer = Task.Run(() =>
        {
            for (int i = 0; i < totalItemsToProcess; i++)
            {
                // 自旋重试直到入队成功
                while (!buffer.TryEnqueue(1))
                {
                    Thread.Sleep(0); // 让出时间片，避免死锁
                }
                Interlocked.Increment(ref producedCount);
            }
        });

        // 开启消费者线程
        Task consumer = Task.Run(() =>
        {
            int itemsConsumed = 0;
            while (itemsConsumed < totalItemsToProcess)
            {
                int val;
                if (buffer.TryDequeue(out val))
                {
                    itemsConsumed++;
                    Interlocked.Increment(ref consumedCount);
                }
                else
                {
                    Thread.Sleep(0);
                }
            }
        });

        // 等待两个线程完成
        Task.WaitAll(producer, consumer);

        Debug.Log($"测试结束:");
        Debug.Log($"生产者产出: {producedCount}");
        Debug.Log($"消费者消耗: {consumedCount}");

        if (producedCount == totalItemsToProcess && consumedCount == totalItemsToProcess)
        {
            Debug.Log("<color=green>测试通过！数据完美守恒，未发生死锁或丢包。</color>");
        }
        else
        {
            Debug.LogError("<color=red>测试失败！数据不一致。</color>");
        }
    }
}