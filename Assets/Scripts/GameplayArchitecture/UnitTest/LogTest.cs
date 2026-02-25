using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using GamePlayArchitecture;

// 确保你已经开启了 ENABLE_LOGSAVE 宏，否则测试无法进行
public class LogTest : MonoBehaviour
{
    private string _logFilePath;
    private string _guiLog = "等待测试指令...\n";
    private Vector2 _scrollPos;

    private void Start()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, "Log.txt");

        // 显式初始化 (模拟 GameMgr 中的行为)
#if ENABLE_LOGSAVE
        Log.InitLogSave();
        AppendGuiLog($"日志路径: {_logFilePath}");
#else
        AppendGuiLog("<color=red>警告: 未开启 ENABLE_LOGSAVE 宏！\n请在 Project Settings -> Player -> Scripting Define Symbols 中添加。</color>");
#endif
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 450, 700));
        GUILayout.Label("<b>Log System 单元测试面板</b>");

        if (GUILayout.Button("1. 基础功能测试 (D/N/E)"))
        {
            TestBasicFunction();
        }

        if (GUILayout.Button("2. 测试 Log.E 立即落盘 (Flush)"))
        {
            TestErrorFlush();
        }

        if (GUILayout.Button("3. 多线程高并发压力测试 (Thread Safety)"))
        {
            StartCoroutine(TestConcurrency());
        }

        if (GUILayout.Button("4. 打开日志文件目录"))
        {
            Application.OpenURL(Application.persistentDataPath);
        }

        if (GUILayout.Button("清空控制台显示")) _guiLog = "";

        GUILayout.Space(10);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400), GUILayout.Width(430));
        GUILayout.TextArea(_guiLog);
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void AppendGuiLog(string msg)
    {
        _guiLog += $"> {msg}\n";
        Debug.Log($"[Tester] {msg}");
    }

    // --- 测试用例 1: 基础写入 ---
    private void TestBasicFunction()
    {
        AppendGuiLog("开始基础测试...");

        // 写入三条不同类型的日志
        // 生成一个随机ID以便验证
        string id = System.Guid.NewGuid().ToString().Substring(0, 8);

        Log.D($"Basic Debug Test ID:{id}");
        Log.N($"Basic Notice Test ID:{id}");

        // 注意：D 和 N 会进入缓冲区，可能不会立即写入文件（除非缓冲区满）
        // 为了验证文件写入，我们最后调用一条 E，强制触发 Flush
        Log.E($"Basic Error Test ID:{id} (Trigger Flush)");

        // 验证文件
        VerifyFileContent(id);
    }

    // --- 测试用例 2: 错误立即落盘 ---
    private void TestErrorFlush()
    {
        AppendGuiLog("测试 Log.E 立即刷新...");
        string uniqueMsg = "Error_Immediate_Check_" + System.DateTime.Now.Ticks;

        // 写入错误日志
        Log.E(uniqueMsg);

        // 不等待，立即读取文件。如果 Flush 逻辑正确，此刻文件中必须有这条日志
        if (File.Exists(_logFilePath))
        {
            string content = File.ReadAllText(_logFilePath);
            if (content.Contains(uniqueMsg))
            {
                AppendGuiLog("<color=green>PASS: Log.E 成功立即写入文件。</color>");
            }
            else
            {
                AppendGuiLog("<color=red>FAIL: Log.E 未立即写入！(缓冲区可能未刷新)</color>");
            }
        }
    }

    // --- 测试用例 3: 多线程并发 ---
    private IEnumerator TestConcurrency()
    {
        AppendGuiLog("开始多线程压力测试 (10个线程 x 500条日志)...");

        int threadCount = 10;
        int logsPerThread = 500;
        int totalExpected = threadCount * logsPerThread;

        // 记录开始时的文件长度，用于粗略估算增加量
        long startLength = 0;
        if (File.Exists(_logFilePath)) startLength = new FileInfo(_logFilePath).Length;

        List<Thread> threads = new List<Thread>();

        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            Thread t = new Thread(() =>
            {
                for (int j = 0; j < logsPerThread; j++)
                {
                    // 混合写入
                    Log.D($"Thread[{threadId}] Log Index:{j}");
                    if (j % 100 == 0) Log.N($"Thread[{threadId}] Milestone:{j}");
                }
            });
            threads.Add(t);
            t.Start();
        }

        // 等待所有线程结束
        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (var t in threads)
            {
                if (t.IsAlive)
                {
                    allDone = false;
                    break;
                }
            }
            yield return null;
        }

        // 最后手动触发一次 Error 强制刷新缓冲区剩余内容
        Log.E("Thread Test Finished (Force Flush)");

        long endLength = new FileInfo(_logFilePath).Length;
        AppendGuiLog($"多线程写入完成。文件大小增长: {endLength - startLength} bytes");
        AppendGuiLog("<color=green>PASS: 未发生死锁或崩溃。</color>");
        AppendGuiLog("请点击 '打开日志文件目录' 检查日志内容是否乱码。");
    }

    // 辅助验证
    private void VerifyFileContent(string keyword)
    {
        if (!File.Exists(_logFilePath))
        {
            AppendGuiLog("<color=red>FAIL: 日志文件不存在！</color>");
            return;
        }

        // 简单读取验证
        string content = File.ReadAllText(_logFilePath);
        if (content.Contains(keyword))
        {
            AppendGuiLog($"<color=green>PASS: 文件中找到了关键字 '{keyword}'</color>");
        }
        else
        {
            AppendGuiLog($"<color=red>FAIL: 文件中未找到关键字 '{keyword}'</color>");
        }
    }
}