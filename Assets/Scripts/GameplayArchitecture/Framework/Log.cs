using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace GamePlayArchitecture
{
    public static class Log
    {
#if ENABLE_LOGSAVE
        // 定义一个用于线程同步的锁对象
        private static readonly object _lock = new object();

        private const int BUFFER_SIZE = 2048;
        private static StringBuilder _buffer = new StringBuilder();
        private static string _logPath;
        private static bool _isInitialized = false;
        private static bool _deleteOldWhenRestart = true;
#endif

#if ENABLE_LOGSAVE
        public static void InitLogSave()
        {
            // 双重检查锁定 (Double-Check Locking) 避免不必要的锁开销
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                // 【注意】Application.persistentDataPath 只能在主线程访问
                // 如果第一次调用 Log 是在子线程，这里会报错。
                // 建议：在 GameStart 的 Awake 中显式调用一次 Log.InitLogSave()
                _logPath = Path.Combine(Application.persistentDataPath, "Log.txt");

                try
                {
                    if (_deleteOldWhenRestart && File.Exists(_logPath)) File.Delete(_logPath);
                }
                catch { }

                Application.quitting += Flush;

                _isInitialized = true;

                // 调用内部写入
                AppendInternal("日志系统已加载，路径：" + _logPath);
            }
        }
#endif

        [Conditional("ENABLE_CONSOLELOG"), Conditional("ENABLE_LOGSAVE")]
        public static void N(string s)
        {
#if ENABLE_CONSOLELOG
            // Debug.Log 在 Unity 5.x 以后是线程安全的，所以这里不用加锁
            UnityEngine.Debug.Log($"<color=green>系统信息:{s}</color>");
#endif
#if ENABLE_LOGSAVE
            AppendToFile($"[Notice] {s}");
#endif
        }

        [Conditional("ENABLE_CONSOLELOG"), Conditional("ENABLE_LOGSAVE")]
        public static void D(string s)
        {
#if ENABLE_CONSOLELOG
            UnityEngine.Debug.Log($"<color=#00BFFF>调试信息:{s}</color>");
#endif
#if ENABLE_LOGSAVE
            AppendToFile($"[Debug] {s}");
#endif
        }

        [Conditional("ENABLE_CONSOLELOG"), Conditional("ENABLE_LOGSAVE")]
        public static void E(string s)
        {
#if ENABLE_CONSOLELOG
            UnityEngine.Debug.LogError($"<color=red>错误:{s}</color>");
#endif
#if ENABLE_LOGSAVE
            AppendToFile($"[Error] {s}");
#endif
        }

#if ENABLE_LOGSAVE
        private static void AppendToFile(string msg)
        {
            // 初始化检查放在锁外，性能更好。InitLogSave 内部有锁。
            if (!_isInitialized) InitLogSave();
            AppendInternal(msg);
        }

        private static void AppendInternal(string msg)
        {
            // 获取当前线程ID，方便调试多线程问题 (可选)
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            string time = DateTime.Now.ToString("HH:mm:ss.fff");

            lock (_lock)
            {
                _buffer.AppendLine($"[{time}][T:{threadId}]{msg}");

                if (_buffer.Length >= BUFFER_SIZE)
                {
                    FlushInternal(); // 调用内部无锁 Flush，避免死锁或重入
                }
            }
        }

        private static void Flush()
        {
            lock (_lock)
            {
                FlushInternal();
            }
        }

        // 真正的写入逻辑 (必须在锁内被调用)
        private static void FlushInternal()
        {
            if (_buffer.Length == 0) return;
            try
            {
                File.AppendAllText(_logPath, _buffer.ToString());
                _buffer.Clear();
            }
            catch (Exception ex)
            {
                // 极端情况下(文件被占用)的容错
                // 这里不能再调 Log.E，否则会死递归，只能用 Unity 原生 Log 报个警
#if ENABLE_CONSOLELOG
                UnityEngine.Debug.LogWarning($"日志写入失败: {ex.Message}");
#endif
            }
        }
#endif
    }
}