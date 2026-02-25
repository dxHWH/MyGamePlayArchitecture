using UnityEngine;
using GamePlayArchitecture; // 引用你的命名空间

public class RingBufferTest : MonoBehaviour
{
    // 在游戏开始时自动运行测试
    void Start()
    {
        Debug.Log("<color=yellow>=== 开始 RingBuffer 测试 ===</color>");

        RunTest("基础入队出队测试", Test_BasicFlow);
        RunTest("容量限制测试", Test_Capacity);
        RunTest("环形回绕测试 (核心)", Test_WrapAround);
        RunTest("Peek 测试", Test_Peek);
        RunTest("清空/复位测试", Test_ClearLogic);

        Debug.Log("<color=yellow>=== 测试结束 ===</color>");
    }

    // --- 测试用例 ---

    bool Test_BasicFlow()
    {
        var buffer = new ThreadSafeRingBuffer<int>(3);

        // 1. 入队 1, 2
        bool ok1 = buffer.TryEnqueue(1);
        bool ok2 = buffer.TryEnqueue(2);

        // 2. 检查数量
        if (buffer.Count != 2) return Fail("Count 应该是 2");

        // 3. 出队
        int val;
        if (!buffer.TryDequeue(out val) || val != 1) return Fail("第一次出队应该是 1");
        if (!buffer.TryDequeue(out val) || val != 2) return Fail("第二次出队应该是 2");

        // 4. 再次出队应该失败
        if (buffer.TryDequeue(out val)) return Fail("队列空了，TryDequeue 应该返回 false");

        return true;
    }

    bool Test_Capacity()
    {
        var buffer = new ThreadSafeRingBuffer<int>(2);
        buffer.TryEnqueue(10);
        buffer.TryEnqueue(20);

        // 此时满了，再加应该失败
        if (buffer.TryEnqueue(30)) return Fail("队列满了，TryEnqueue 应该返回 false");

        return true;
    }

    bool Test_WrapAround()
    {
        // 容量 3: [ , , ]
        var buffer = new ThreadSafeRingBuffer<int>(3);

        // 填满: [1, 2, 3]
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);
        buffer.TryEnqueue(3);

        // 取出一个: [空, 2, 3] (Head 指向 1)
        int temp;
        buffer.TryDequeue(out temp);

        // 再加一个: [4, 2, 3] (Tail 回绕到 0)
        // 如果你的取模逻辑写错了，这里会出错
        if (!buffer.TryEnqueue(4)) return Fail("回绕入队失败");

        // 验证顺序: 2 -> 3 -> 4
        int v1, v2, v3;
        buffer.TryDequeue(out v1);
        buffer.TryDequeue(out v2);
        buffer.TryDequeue(out v3);

        if (v1 != 2 || v2 != 3 || v3 != 4) return Fail($"顺序错误: 期望 2,3,4 实际 {v1},{v2},{v3}");

        return true;
    }

    bool Test_Peek()
    {
        var buffer = new ThreadSafeRingBuffer<int>(5);
        buffer.TryEnqueue(99);

        int val;
        // Peek 不应该移除元素
        if (!buffer.TryPeek(out val) || val != 99) return Fail("Peek 失败或值错误");
        if (buffer.Count != 1) return Fail("Peek 后 Count 数量变了");

        // 之后 Dequeue 应该还能拿出来
        buffer.TryDequeue(out val);
        if (val != 99) return Fail("Peek 后的 Dequeue 值错误");

        return true;
    }

    bool Test_ClearLogic()
    {
        // 验证出队完是否真的空了
        var buffer = new ThreadSafeRingBuffer<int>(2);
        buffer.TryEnqueue(1);
        int val;
        buffer.TryDequeue(out val);

        if (buffer.Count != 0) return Fail("Count 没归零");
        if (buffer.TryDequeue(out val)) return Fail("应该取不出东西了");

        return true;
    }

    // --- 简单的测试辅助工具 ---

    // 运行单个测试的包装器
    void RunTest(string testName, System.Func<bool> testFunc)
    {
        try
        {
            if (testFunc())
            {
                Debug.Log($"<color=green>[通过]</color> {testName}");
            }
            // 失败已经在 Fail 函数里打印了，这里不用处理
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[异常]</color> {testName}: {e.Message}\n{e.StackTrace}");
        }
    }

    // 失败时的辅助函数
    bool Fail(string reason)
    {
        Debug.LogError($"<color=red>[失败]</color> {reason}");
        return false;
    }
}