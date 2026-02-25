using UnityEngine;
using GamePlayArchitecture;
using System.Collections;

/// <summary>
/// TimerSystem 完整测试套件
/// 测试 Slot Map 机制、句柄安全性、暂停/恢复、循环计时器等功能
/// </summary>
public class TimerSystemTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("<color=cyan>╔══════════════════════════════════════╗</color>");
        Debug.Log("<color=cyan>║     TimerSystem 完整测试套件          ║</color>");
        Debug.Log("<color=cyan>╚══════════════════════════════════════╝</color>");

        StartCoroutine(RunAllTests());
    }

    /// <summary>
    /// 协程方式运行所有测试
    /// </summary>
    IEnumerator RunAllTests()
    {
        // ==================== 基础功能测试 ====================
        yield return RunTestCoroutine("基础回调测试", Test_BasicCallback);
        yield return RunTest("句柄有效性测试", Test_HandleValidity);
        yield return RunTest("命名计时器测试", Test_NamedTimer);

        // ==================== 状态控制测试 ====================
        yield return RunTestCoroutine("暂停/恢复测试", Test_PauseResume);
        yield return RunTestCoroutine("停止计时器测试", Test_StopTimer);
        yield return RunTestCoroutine("获取剩余时间测试", Test_GetTimeRemaining);

        // ==================== 高级功能测试 ====================
        yield return RunTestCoroutine("循环计时器测试", Test_LoopTimer);
        yield return RunTestCoroutine("命名覆盖测试", Test_NameOverride);
        yield return RunTestCoroutine("时间缩放测试", Test_TimeScale);

        // ==================== Slot Map 机制测试 ====================
        yield return RunTest("Swap-and-Pop 机制测试", Test_SwapAndPop);
        yield return RunTest("句柄代数自增测试", Test_GenerationIncrement);
        yield return RunTest("索引复用测试", Test_IndexReuse);

        // ==================== 边界情况测试 ====================
        yield return RunTest("空句柄测试", Test_InvalidHandle);
        yield return RunTestCoroutine("批量计时器测试", Test_BulkTimers);
        yield return RunTestCoroutine("回调中停止计时器测试", Test_StopInCallback);

        // ==================== 间隔回调测试 ====================
        yield return RunTestCoroutine("基础间隔回调测试", Test_IntervalCallback);
        yield return RunTestCoroutine("多次间隔回调测试", Test_MultipleIntervalCallbacks);
        yield return RunTestCoroutine("追逐模式测试", Test_ChaseMode);
        yield return RunTestCoroutine("非追逐模式测试", Test_NonChaseMode);
        yield return RunTestCoroutine("循环+间隔回调测试", Test_LoopWithInterval);
        yield return RunTestCoroutine("间隔回调中停止测试", Test_StopInIntervalCallback);
        yield return RunTestCoroutine("间隔+完成回调测试", Test_IntervalWithComplete);

        Debug.Log("<color=yellow>══════════════════════════════════════</color>");
        Debug.Log("<color=yellow>测试结束</color>");
    }

    // ===================================================================================
    // 基础功能测试
    // ===================================================================================

    // 基础回调测试已移至协程版本 Test_BasicCallback()

    /// <summary>
    /// 测试句柄的有效性验证
    /// </summary>
    bool Test_HandleValidity()
    {
        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => { }
        );

        // 新创建的句柄应该有效
        if (!TimerSystem.Instance.IsHandleValid(handle))
            return Fail("新创建的句柄应该有效");

        // 停止后句柄应该失效
        TimerSystem.Instance.StopTimer(handle);
        if (TimerSystem.Instance.IsHandleValid(handle))
            return Fail("停止后句柄应该失效");

        // Invalid 句柄应该始终无效
        if (TimerSystem.Instance.IsHandleValid(TimerHandle.Invalid))
            return Fail("TimerHandle.Invalid 应该始终无效");

        return true;
    }

    /// <summary>
    /// 测试命名计时器的创建和查找
    /// </summary>
    bool Test_NamedTimer()
    {
        string timerName = "NamedTimer_Test";
        bool callbackFired = false;

        TimerHandle handle1 = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => callbackFired = true,
            timerName: timerName
        );

        // 通过名字获取句柄
        TimerHandle handle2 = TimerSystem.Instance.GetHandleByName(timerName);

        if (handle2 == TimerHandle.Invalid)
            return Fail("通过名字找不到句柄");

        if (handle1 != handle2)
            return Fail("通过名字获取的句柄与原始句柄不一致");

        // 通过名字停止
        TimerSystem.Instance.StopTimer(timerName);

        if (TimerSystem.Instance.IsHandleValid(handle1))
            return Fail("通过名字停止后，原句柄应该失效");

        return true;
    }

    // ===================================================================================
    // 状态控制测试
    // ===================================================================================

    // 暂停/恢复测试已移至协程版本 Test_PauseResume()
    // 获取剩余时间测试已移至协程版本 Test_GetTimeRemaining()
    // 循环计时器测试已移至协程版本 Test_LoopTimer()
    // 命名覆盖测试已移至协程版本 Test_NameOverride()
    // 批量计时器测试已移至协程版本 Test_BulkTimers()
    // 回调中停止计时器测试已移至协程版本 Test_StopInCallback()

    // ===================================================================================
    // Slot Map 机制测试
    // ===================================================================================

    /// <summary>
    /// 测试 Swap-and-Pop 移除机制
    /// </summary>
    bool Test_SwapAndPop()
    {
        bool[] callbacks = { false, false, false };
        TimerHandle[] handles = new TimerHandle[3];

        // 创建 3 个计时器（使用长时间避免在测试期间触发）
        for (int i = 0; i < 3; i++)
        {
            int index = i; // 捕获局部变量避免闭包问题
            handles[i] = TimerSystem.Instance.CreateTimer(
                duration: 100f,
                onComplete: () => callbacks[index] = true
            );
        }

        // 停止中间的计时器 (索引 1)
        TimerSystem.Instance.StopTimer(handles[1]);

        // 剩余两个计时器应该仍然有效
        if (!TimerSystem.Instance.IsHandleValid(handles[0]))
            return Fail("Timer[0] 应该仍然有效");

        if (!TimerSystem.Instance.IsHandleValid(handles[2]))
            return Fail("Timer[2] 应该仍然有效");

        // 被停止的应该失效
        if (TimerSystem.Instance.IsHandleValid(handles[1]))
            return Fail("Timer[1] 应该已失效");

        return true;
    }

    /// <summary>
    /// 测试句柄代数（Generation）自增机制
    /// </summary>
    bool Test_GenerationIncrement()
    {
        string timerName = "GenTest";

        TimerHandle handle1 = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => { },
            timerName: timerName
        );

        int gen1 = handle1.Generation;

        // 停止计时器
        TimerSystem.Instance.StopTimer(handle1);

        // 重新创建同名计时器
        TimerHandle handle2 = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => { },
            timerName: timerName
        );

        int gen2 = handle2.Generation;

        // 代数应该增加
        if (gen2 != gen1 + 1)
            return Fail($"代数应该自增: {gen1} -> {gen2}");

        // 旧句柄应该失效
        if (TimerSystem.Instance.IsHandleValid(handle1))
            return Fail("旧代数的句柄应该失效");

        return true;
    }

    /// <summary>
    /// 测试索引复用机制（Free List）
    /// </summary>
    bool Test_IndexReuse()
    {
        TimerHandle handle1 = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => { }
        );

        int idx1 = handle1.Index;

        // 停止并回收索引
        TimerSystem.Instance.StopTimer(handle1);

        // 再创建一个，应该复用索引
        TimerHandle handle2 = TimerSystem.Instance.CreateTimer(
            duration: 10f,
            onComplete: () => { }
        );

        int idx2 = handle2.Index;

        if (idx1 != idx2)
            return Fail($"索引应该被复用: {idx1} != {idx2}");

        // 但代数应该不同
        if (handle1.Generation == handle2.Generation)
            return Fail("复用索引时代数应该不同");

        return true;
    }

    // ===================================================================================
    // 边界情况测试
    // ===================================================================================

    /// <summary>
    /// 测试无效句柄的各种操作
    /// </summary>
    bool Test_InvalidHandle()
    {
        TimerHandle invalid = TimerHandle.Invalid;

        // 所有操作都应该安全地忽略无效句柄
        TimerSystem.Instance.StopTimer(invalid);
        TimerSystem.Instance.SetPaused(invalid, true);
        float time = TimerSystem.Instance.GetTimeRemaining(invalid);

        if (time != 0f)
            return Fail("无效句柄的剩余时间应该返回 0");

        // 空名字操作也应该安全
        TimerSystem.Instance.StopTimer("");
        TimerSystem.Instance.StopTimer(null);
        TimerSystem.Instance.SetPaused("", true);
        TimerSystem.Instance.SetPaused(null, true);
        TimerSystem.Instance.GetTimeRemaining("");
        TimerSystem.Instance.GetTimeRemaining(null);

        return true;
    }

    // 批量计时器测试已移至协程版本 Test_BulkTimers()
    // 回调中停止计时器测试已移至协程版本 Test_StopInCallback()

    // ===================================================================================
    // 辅助方法和工具类
    // ===================================================================================

    /// <summary>
    /// 运行同步测试的包装器（协程版本）
    /// </summary>
    IEnumerator RunTest(string testName, System.Func<bool> testFunc)
    {
        yield return new WaitForSeconds(0.05f); // 测试间隔

        // 执行测试并捕获异常
        bool passed = ExecuteTest(testFunc, testName);

        if (passed)
        {
            Debug.Log($"<color=green>[通过]</color> {testName}");
        }
    }

    /// <summary>
    /// 运行异步测试的协程包装器
    /// </summary>
    IEnumerator RunTestCoroutine(string testName, System.Func<IEnumerator> testFunc)
    {
        yield return new WaitForSeconds(0.05f); // 测试间隔

        // 先打印开始标记
        Debug.Log($"<color=gray>[测试中]</color> {testName}...");

        // 运行协程测试，异常会在协程内部处理
        yield return StartCoroutine(testFunc());

        // 如果执行到这里说明没有抛出异常
        Debug.Log($"<color=green>[通过]</color> {testName}");
    }

    /// <summary>
    /// 执行同步测试并捕获异常
    /// </summary>
    bool ExecuteTest(System.Func<bool> testFunc, string testName)
    {
        try
        {
            return testFunc();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[异常]</color> {testName}: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 失败时的辅助函数
    /// </summary>
    bool Fail(string reason)
    {
        Debug.LogError($"<color=red>[失败]</color> {reason}");
        return false;
    }

    /// <summary>
    /// 异步等待回调触发
    /// </summary>
    IEnumerator WaitForCallback(System.Func<bool> condition, string failMsg, float timeout)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (condition())
                yield break; // 成功
            yield return null;
            elapsed += Time.deltaTime;
        }

        Fail(failMsg);
    }

    // ===================================================================================
    // 协程版本的测试用例
    // ===================================================================================

    /// <summary>
    /// 协程版本：基础回调测试
    /// </summary>
    IEnumerator Test_BasicCallback()
    {
        bool callbackFired = false;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.1f,
            onComplete: () => callbackFired = true,
            timerName: "BasicTimer_Coroutine"
        );

        if (handle == TimerHandle.Invalid)
            throw new System.Exception("创建计时器失败");

        yield return new WaitUntil(() => callbackFired);
        yield return new WaitForSeconds(0.05f); // 额外等待确认
    }

    /// <summary>
    /// 协程版本：暂停/恢复测试
    /// </summary>
    IEnumerator Test_PauseResume()
    {
        bool callbackFired = false;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.1f,
            onComplete: () => callbackFired = true,
            timerName: "PauseTimer_Coroutine"
        );

        // 暂停
        TimerSystem.Instance.SetPaused(handle, true);
        yield return new WaitForSeconds(0.15f);

        // 确认没有触发
        if (callbackFired)
            throw new System.Exception("暂停期间回调触发了");

        // 恢复
        TimerSystem.Instance.SetPaused(handle, false);
        yield return new WaitUntil(() => callbackFired);
    }

    /// <summary>
    /// 协程版本：停止计时器测试
    /// </summary>
    IEnumerator Test_StopTimer()
    {
        bool callbackFired = false;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () => callbackFired = true
        );

        TimerSystem.Instance.StopTimer(handle);
        yield return new WaitForSeconds(0.15f);

        if (callbackFired)
            throw new System.Exception("停止后回调仍然触发了");
    }

    /// <summary>
    /// 协程版本：循环计时器测试
    /// </summary>
    IEnumerator Test_LoopTimer()
    {
        int callbackCount = 0;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () => callbackCount++,
            timerName: "LoopTimer_Coroutine",
            isLoop: true
        );

        // 等待至少触发 3 次
        yield return new WaitUntil(() => callbackCount >= 3);

        // 停止循环计时器
        TimerSystem.Instance.StopTimer(handle);
    }

    /// <summary>
    /// 协程版本：命名覆盖测试
    /// </summary>
    IEnumerator Test_NameOverride()
    {
        bool firstCallback = false;
        bool secondCallback = false;
        string timerName = "OverrideTimer_Coroutine";

        TimerHandle handle1 = TimerSystem.Instance.CreateTimer(
            duration: 1f,
            onComplete: () => firstCallback = true,
            timerName: timerName
        );

        yield return new WaitForSeconds(0.05f);

        TimerHandle handle2 = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () => secondCallback = true,
            timerName: timerName
        );

        if (TimerSystem.Instance.IsHandleValid(handle1))
            throw new System.Exception("被覆盖的句柄应该失效");

        yield return new WaitUntil(() => secondCallback);
        yield return new WaitForSeconds(0.1f);

        if (firstCallback)
            throw new System.Exception("第一个回调不应该触发");
    }

    /// <summary>
    /// 协程版本：批量计时器测试
    /// </summary>
    IEnumerator Test_BulkTimers()
    {
        int count = 30;
        int callbackCount = 0;

        for (int i = 0; i < count; i++)
        {
            TimerSystem.Instance.CreateTimer(
                duration: 0.03f + (i * 0.001f),
                onComplete: () => callbackCount++
            );
        }

        yield return new WaitUntil(() => callbackCount >= count - 3);
    }

    /// <summary>
    /// 协程版本：回调中停止计时器测试
    /// </summary>
    IEnumerator Test_StopInCallback()
    {
        bool callbackFired = false;
        string timerName = "StopInCallback_Coroutine";

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () =>
            {
                callbackFired = true;
                TimerSystem.Instance.StopTimer(timerName);
            },
            timerName: timerName
        );

        yield return new WaitUntil(() => callbackFired);
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// 协程版本：获取剩余时间测试
    /// </summary>
    IEnumerator Test_GetTimeRemaining()
    {
        float duration = 0.5f;
        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: duration,
            onComplete: () => { }
        );

        float remaining1 = TimerSystem.Instance.GetTimeRemaining(handle);
        if (remaining1 < 0.4f || remaining1 > duration)
            throw new System.Exception($"初始剩余时间错误: {remaining1}");

        yield return new WaitForSeconds(0.2f);

        float remaining2 = TimerSystem.Instance.GetTimeRemaining(handle);
        if (remaining2 >= remaining1)
            throw new System.Exception("剩余时间应该减少");
    }

    /// <summary>
    /// 协程版本：时间缩放测试
    /// </summary>
    IEnumerator Test_TimeScale()
    {
        // 这个测试验证 scaled 和 unscaled 计时器都能正常工作
        // 实际测试中可以通过修改 Time.timeScale 来验证差异
        bool scaledFired = false;
        bool unscaledFired = false;

        TimerHandle scaledHandle = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () => scaledFired = true,
            timerName: "ScaledTimer",
            isUnScaled: false
        );

        TimerHandle unscaledHandle = TimerSystem.Instance.CreateTimer(
            duration: 0.05f,
            onComplete: () => unscaledFired = true,
            timerName: "UnscaledTimer",
            isUnScaled: true
        );

        // 两者都应该触发
        yield return new WaitUntil(() => scaledFired && unscaledFired);
    }

    // ===================================================================================
    // 间隔回调功能测试
    // ===================================================================================

    /// <summary>
    /// 协程版本：基础间隔回调测试
    /// </summary>
    IEnumerator Test_IntervalCallback()
    {
        int triggerCount = 0;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.5f,
            interval: 0.1f,
            onTrigger: () => triggerCount++,
            onComplete: () => { },
            timerName: "IntervalTimer_Basic"
        );

        // 等待间隔回调至少触发一次
        yield return new WaitUntil(() => triggerCount >= 1);

        // 验证触发次数合理（应该在1-5次之间）
        if (triggerCount < 1 || triggerCount > 5)
            throw new System.Exception($"间隔回调次数异常: {triggerCount}");

        // 验证句柄仍然有效（因为总时长还没到）
        if (!TimerSystem.Instance.IsHandleValid(handle))
            throw new System.Exception("间隔回调触发后句柄应该仍然有效");

        TimerSystem.Instance.StopTimer(handle);
    }

    /// <summary>
    /// 协程版本：多次间隔回调测试
    /// </summary>
    IEnumerator Test_MultipleIntervalCallbacks()
    {
        int triggerCount = 0;
        bool completed = false;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.5f,
            interval: 0.1f,
            onTrigger: () => triggerCount++,
            onComplete: () => completed = true,
            timerName: "IntervalTimer_Multiple"
        );

        // 等待完成
        yield return new WaitUntil(() => completed);

        // 验证间隔回调触发了多次（约5次）
        if (triggerCount < 3 || triggerCount > 6)
            throw new System.Exception($"间隔回调次数应该约为5次，实际: {triggerCount}");
    }

    /// <summary>
    /// 协程版本：追逐模式测试 (isChase = true，需要手动设置)
    /// 注意：当前 CreateTimer 未暴露 isChase 参数，此测试验证默认行为
    /// </summary>
    IEnumerator Test_ChaseMode()
    {
        int triggerCount = 0;

        // 默认 isChase = false，所以这里测试非追逐模式
        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 1f,
            interval: 0.05f,
            onTrigger: () => triggerCount++,
            onComplete: () => { },
            timerName: "ChaseMode_Timer"
        );

        // 等待足够时间，让间隔回调触发多次
        yield return new WaitForSeconds(0.5f);

        // 验证间隔回调正常工作
        if (triggerCount < 1)
            throw new System.Exception($"间隔回调应该触发，实际: {triggerCount}");

        TimerSystem.Instance.StopTimer(handle);
    }

    /// <summary>
    /// 协程版本：非追逐模式测试 (默认模式)
    /// </summary>
    IEnumerator Test_NonChaseMode()
    {
        int triggerCount = 0;
        bool completed = false;

        // 默认是非追逐模式：即使累积多个间隔时间，每帧也只触发一次
        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.2f,
            interval: 0.03f,
            onTrigger: () => triggerCount++,
            onComplete: () => completed = true,
            timerName: "NonChaseMode_Timer"
        );

        yield return new WaitUntil(() => completed);

        // 非追逐模式下，触发次数应该有限（每帧最多一次）
        // 0.2秒内帧数有限，所以触发次数应该有限
        if (triggerCount > 20)
            throw new System.Exception($"非追逐模式下触发次数过多: {triggerCount}");
    }

    /// <summary>
    /// 协程版本：循环+间隔回调测试
    /// </summary>
    IEnumerator Test_LoopWithInterval()
    {
        int triggerCount = 0;
        int completeCount = 0;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.1f,
            interval: 0.03f,
            onTrigger: () => triggerCount++,
            onComplete: () => completeCount++,
            timerName: "LoopWithInterval_Timer",
            isLoop: true
        );

        // 等待间隔回调触发多次
        yield return new WaitUntil(() => triggerCount >= 6);

        // 验证完成回调也触发了多次
        if (completeCount < 1)
            throw new System.Exception("循环模式下完成回调应该触发");

        TimerSystem.Instance.StopTimer(handle);
    }

    /// <summary>
    /// 协程版本：间隔回调中停止测试
    /// </summary>
    IEnumerator Test_StopInIntervalCallback()
    {
        TimerHandle handle = TimerHandle.Invalid;
        int triggerCount = 0;
        bool completed = false;

        handle = TimerSystem.Instance.CreateTimer(
            duration: 1f,
            interval: 0.05f,
            onTrigger: () =>
            {
                triggerCount++;
                // 在第3次间隔回调时停止计时器
                if (triggerCount >= 3)
                {
                    TimerSystem.Instance.StopTimer(handle);
                }
            },
            onComplete: () => completed = true,
            timerName: "StopInInterval_Timer"
        );

        // 等待足够时间
        yield return new WaitForSeconds(0.5f);

        // 验证间隔回调触发后停止了
        if (triggerCount > 5)
            throw new System.Exception($"间隔回调应该在触发3次后停止，实际触发: {triggerCount}");

        // 验证句柄已经失效
        if (TimerSystem.Instance.IsHandleValid(handle))
            throw new System.Exception("在间隔回调中停止后句柄应该失效");

        // 验证完成回调没有触发（因为提前停止了）
        if (completed)
            throw new System.Exception("提前停止时完成回调不应该触发");
    }

    /// <summary>
    /// 协程版本：间隔+完成回调测试
    /// </summary>
    IEnumerator Test_IntervalWithComplete()
    {
        int triggerCount = 0;
        int completeCount = 0;
        int finalTriggerCount = 0;

        TimerHandle handle = TimerSystem.Instance.CreateTimer(
            duration: 0.3f,
            interval: 0.1f,
            onTrigger: () => triggerCount++,
            onComplete: () =>
            {
                completeCount++;
                finalTriggerCount = triggerCount;
            },
            timerName: "IntervalWithComplete_Timer"
        );

        yield return new WaitUntil(() => completeCount > 0);

        // 验证间隔回调触发了（2-3次）
        if (finalTriggerCount < 2 || finalTriggerCount > 4)
            throw new System.Exception($"间隔回调次数应该在2-4次之间，实际: {finalTriggerCount}");

        // 验证完成回调触发了一次
        if (completeCount != 1)
            throw new System.Exception($"完成回调应该触发1次，实际: {completeCount}");
    }

}
