using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlayArchitecture
{
    // ===================================================================================
    // 1. 计时器句柄 (TimerHandle) - 纯值类型
    // ===================================================================================
    /// <summary>
    /// 唯一标识一个计时器的票据。
    /// 包含索引和代数(Generation)，用于安全访问和防止 ABA 问题。
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        public readonly int Index;      // 数组中的逻辑索引
        public readonly int Generation; // 版本号

        public TimerHandle(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }

        // 哨兵值：表示无效句柄
        public static readonly TimerHandle Invalid = new TimerHandle(-1, -1);

        // 标准判等重写，保证 Dictionary key 的性能
        public bool Equals(TimerHandle other) => Index == other.Index && Generation == other.Generation;
        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, Generation); // 极速哈希
        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
        public override string ToString() => $"TimerHandle(Idx:{Index}, Gen:{Generation})";
    }

    // ===================================================================================
    // 2. 计时器数据 (TimerData) - 纯值类型 (Blittable)
    // ===================================================================================
    /// <summary>
    /// 存储计时器的实际状态。
    /// 注意：这里不存 string，而是存 int Hash，为了保持结构体不含引用类型，避免 GC 扫描。
    /// </summary>
    public struct TimerData
    {
        public float duration;      // 总时长
        public float timeElapsed;   // 已过时长

        public float interval;      //定时触发间隔
        public float intervalElapsed; //间隔累加器

        public Action onTrigger;    // 定时回调
        public Action onComplete;   // 回调 (这是唯一的引用类型字段，但在 List<Struct> 中影响较小)

        // 状态位 (使用 byte 或 bool)
        public bool isLoop;         // 是否循环
        public bool isPaused;       // 是否暂停
        public bool isUnScaled;     // 是否忽略 TimeScale (UI用)
        public bool isChase;      // 当deltaTime>interval的时候是否追逐

        // 标识符 (存 Hash 而不是 string，优化内存和 GC)
        public int timerId;         // 如果为 0，表示匿名计时器
    }

    // ===================================================================================
    // 3. 计时器系统 (TimerSystem) - 核心管理器
    // ===================================================================================
    public class TimerSystem : MonoSingleton<TimerSystem>
    {
        private const int MAX_CHASE_COUNT = 5;
        private void Awake()
        {
            AllocateCapacity(128);
        }
        // --------------------------------------------------------------------------------------

        // ===========================
        // 核心数据结构 (Slot Map)
        // ===========================
        // 1. 紧凑数据数组 (Hot Data): 每一帧都要遍历，必须内存连续
        private TimerData[] _timers = new TimerData[128];
        private int _timerCount = 0;

        // 2. 稀疏映射 (Indirection): Handle.Index -> _timers 真实下标
        private readonly List<int> _handleToIndex = new List<int>(128);

        // 3. 反向映射 (Reverse): _timers 真实下标 -> Handle.Index
        private readonly List<int> _indexToHandle = new List<int>(128);

        // 4. 版本控制 (Generation): 存储每个 Handle.Index 的当前代数
        private readonly List<int> _generations = new List<int>(128);

        // 5. 空闲池 (Free List): 复用 Handle.Index
        private readonly Queue<int> _freeIndices = new Queue<int>(128);

        // ===========================
        // 辅助系统
        // ===========================
        // 别名映射: NameHash -> TimerHandle
        private readonly Dictionary<int, TimerHandle> _idToHandle = new Dictionary<int, TimerHandle>(64);

        // 缓存过期计时器，避免在 Update 循环中修改 List
        private readonly List<TimerHandle> _expiredTimersCache = new List<TimerHandle>(64);

        private readonly List<TimerHandle> _intervalTimersCache = new List<TimerHandle>(64);

        private void EnsureCapacity(int minCapacity)
        {
            if (_timers.Length < minCapacity)
            {
                // 扩容策略：翻倍
                int newSize = Mathf.Max(_timers.Length * 2, minCapacity);
                Array.Resize(ref _timers, newSize);
            }
        }

        /// <summary>
        /// 初始化容量
        /// </summary>
        private void AllocateCapacity(int capacity)
        {
            EnsureCapacity(capacity);
            _handleToIndex.Capacity = capacity;
            _indexToHandle.Capacity = capacity;
            _generations.Capacity = capacity;
        }

        private void Update()
        {
            // 核心驱动：同时传入两种时间增量
            Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        // ===================================================================================
        // 公共 API (增删查)
        // ===================================================================================

        /// <summary>
        /// 创建计时器
        /// </summary>
        /// <param name="duration">时长</param>
        /// <param name="onComplete">回调</param>
        /// <param name="timerName">可选别名 (如果不为空，会覆盖同名旧计时器)</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="isUnScaled">是否使用真实时间 (UI建议为 true)</param>
        public TimerHandle CreateTimer(float duration, Action onComplete,
                                       float interval = -1f, Action onTrigger = null,
                                       string timerName = null,
                                       bool isLoop = false,
                                       bool isUnScaled = false,
                                       bool isChase = true)
        {
            int timerId = 0;

            // 1. 处理别名逻辑 (Hash化)
            if (!string.IsNullOrEmpty(timerName))
            {
                // 使用 Unity 内置的高效 Hash，也可以用 timerName.GetHashCode()
                timerId = timerName.GetHashCode();

                // 如果同名计时器已存在，执行覆盖策略（停止旧的，启动新的）
                if (_idToHandle.TryGetValue(timerId, out TimerHandle oldHandle))
                {
                    if (IsHandleValid(oldHandle)) StopTimer(oldHandle);
                    _idToHandle.Remove(timerId); // 移除旧记录
                }
            }

            // 2. 构建数据
            TimerData data = new TimerData
            {
                duration = duration,
                timeElapsed = 0f,
                interval = interval,
                intervalElapsed = 0f,
                onTrigger = onTrigger,
                onComplete = onComplete,
                isLoop = isLoop,
                isPaused = false,
                isUnScaled = isUnScaled,
                isChase = isChase,
                timerId = timerId // 存 Hash
            };

            // 3. 分配 Handle Index (从池中取或新建)
            int handleIndex;
            if (_freeIndices.Count > 0)
            {
                handleIndex = _freeIndices.Dequeue();
            }
            else
            {
                // 扩容
                handleIndex = _handleToIndex.Count;
                _handleToIndex.Add(-1);
                _indexToHandle.Add(-1);
                _generations.Add(0); // 初始代数为0
            }

            // 4. 生成 Handle
            int generation = _generations[handleIndex];
            TimerHandle handle = new TimerHandle(handleIndex, generation);

            // 5. 放入紧凑数组 (Dense Array)
            EnsureCapacity(_timerCount + 1);
            _timers[_timerCount] = data; // 只有一次写入
            int realIndex = _timerCount;
            _timerCount++;

            // 6. 建立映射关系
            _handleToIndex[handleIndex] = realIndex;

            // 确保反向映射数组够长
            if (realIndex >= _indexToHandle.Count) _indexToHandle.Add(handleIndex);
            else _indexToHandle[realIndex] = handleIndex;

            // 7. 注册 ID 映射
            if (timerId != 0)
            {
                _idToHandle[timerId] = handle;
            }

            return handle;
        }

        /// <summary>
        /// 通过名字停止计时器
        /// </summary>
        public void StopTimer(string timerName)
        {
            if (string.IsNullOrEmpty(timerName)) return;
            int id = timerName.GetHashCode();

            if (_idToHandle.TryGetValue(id, out TimerHandle handle))
            {
                StopTimer(handle);
            }
        }

        /// <summary>
        /// 通过句柄停止计时器 (O(1))
        /// </summary>
        public void StopTimer(TimerHandle handle)
        {
            if (!IsHandleValid(handle)) return;

            int handleIndex = handle.Index;
            int realIndex = _handleToIndex[handleIndex];

            // --- 别名清理逻辑 ---
            // 在数据被移除前，获取它的 ID，从字典里清理掉
            int timerId = _timers[realIndex].timerId;
            if (timerId != 0)
            {
                // 双重校验：确保字典里指的确实是当前这个 Handle (防止极端 ABA 覆盖)
                if (_idToHandle.TryGetValue(timerId, out var storedHandle) && storedHandle == handle)
                {
                    _idToHandle.Remove(timerId);
                }
            }

            // --- Swap-and-Pop (核心移除) ---
            int lastRealIndex = _timerCount - 1;

            // 如果要删除的不是最后一个，需要搬运
            if (realIndex != lastRealIndex)
            {
                // 直接内存拷贝，比 List[i] 赋值稍微快一点点
                _timers[realIndex] = _timers[lastRealIndex];

                // 更新映射 ...
                int lastHandleIndex = _indexToHandle[lastRealIndex];
                _indexToHandle[realIndex] = lastHandleIndex;
                _handleToIndex[lastHandleIndex] = realIndex;
            }

            // 2. 移除数组末尾
            _timerCount--;

            // 3. 销毁 Handle (版本自增)
            _generations[handleIndex]++; // 此时旧 Handle 彻底失效
            _freeIndices.Enqueue(handleIndex); // 回收索引
        }

        /// <summary>
        /// 通过名字暂停/恢复计时器
        /// </summary>
        public void SetPaused(string timerName, bool isPaused)
        {
            // 1. 判空
            if (string.IsNullOrEmpty(timerName)) return;

            // 2. 转 Hash
            int id = timerName.GetHashCode();

            // 3. 查字典找到 Handle
            if (_idToHandle.TryGetValue(id, out TimerHandle handle))
            {
                // 4. 调用核心方法的 Handle 版本
                SetPaused(handle, isPaused);
            }
        }

        /// <summary>
        /// 暂停/恢复
        /// </summary>
        public void SetPaused(TimerHandle handle, bool isPaused)
        {
            if (!IsHandleValid(handle)) return;
            int realIndex = _handleToIndex[handle.Index];

            // 结构体修改需要取出来再赋值回去
            TimerData data = _timers[realIndex];
            data.isPaused = isPaused;
            _timers[realIndex] = data;
        }

        /// <summary>
        /// 通过名字获取剩余时间
        /// </summary>
        public float GetTimeRemaining(string timerName)
        {
            if (string.IsNullOrEmpty(timerName)) return 0f;
            int id = timerName.GetHashCode();

            // 2. 查字典
            if (_idToHandle.TryGetValue(id, out TimerHandle handle))
            {
                // 3. 调用核心方法
                return GetTimeRemaining(handle);
            }
            return 0f;
        }

        /// <summary>
        /// 获取剩余时间
        /// </summary>
        public float GetTimeRemaining(TimerHandle handle)
        {
            if (!IsHandleValid(handle)) return 0f;
            int realIndex = _handleToIndex[handle.Index];
            ref TimerData data = ref _timers[realIndex];
            return Mathf.Max(0f, data.duration - data.timeElapsed);
        }

        /// <summary>
        /// 检查 Handle 是否有效 (版本校验)
        /// </summary>
        public bool IsHandleValid(TimerHandle handle)
        {
            // 1. 索引范围检查
            if (handle.Index < 0 || handle.Index >= _generations.Count) return false;
            // 2. 代数校验 (核心安全机制)
            return _generations[handle.Index] == handle.Generation;
        }

        /// <summary>
        /// 获取句柄，用于高性能缓存
        /// </summary>
        public TimerHandle GetHandleByName(string timerName)
        {
            if (string.IsNullOrEmpty(timerName)) return TimerHandle.Invalid;
            int id = timerName.GetHashCode();
            if (_idToHandle.TryGetValue(id, out TimerHandle handle))
            {
                return handle;
            }
            return TimerHandle.Invalid;
        }

        // ===================================================================================
        // 核心循环 (Tick)
        // ===================================================================================
        private void Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            _expiredTimersCache.Clear();
            _intervalTimersCache.Clear();
            for (int i = 0; i < _timerCount; i++)
            {
                // 【关键】：使用 ref 直接获取数组内存地址的引用
                // 不产生副本，修改 timer 变量就是修改数组内存
                ref TimerData timer = ref _timers[i];

                if (timer.isPaused) continue;

                float dt = timer.isUnScaled ? unscaledDeltaTime : scaledDeltaTime;

                // 直接修改内存
                timer.timeElapsed += dt;

                // ===========================================
                // 间隔回调逻辑
                // ===========================================
                if (timer.interval > 0f) // 只有设置了间隔才执行
                {
                    timer.intervalElapsed += dt;

                    int handleIndex = _indexToHandle[i];
                    int generation = _generations[handleIndex];

                    if (timer.isChase)
                    {
                        int chaseCount = 0;
                        // --- 追赶模式 (While) ---
                        // 如果累积时间够触发多次，就循环加入多次
                        while (timer.intervalElapsed >= timer.interval && chaseCount < MAX_CHASE_COUNT)
                        {
                            // 加入缓存（如果是 3 次，就 Add 3 个相同的 Handle）
                            _intervalTimersCache.Add(new TimerHandle(handleIndex, generation));

                            // 扣除一个间隔
                            timer.intervalElapsed -= timer.interval;
                            chaseCount++;
                        }
                    }
                    else
                    {
                        // --- 普通模式 (If) ---
                        // 即使累积了 10 秒，也只触发一次
                        if (timer.intervalElapsed >= timer.interval)
                        {
                            _intervalTimersCache.Add(new TimerHandle(handleIndex, generation));

                            // 扣除一个间隔 (保持节拍)
                            // 或者 timer.intervalElapsed = 0; (看你是否需要保持严格的节奏相位)
                            // 这里建议减去，这样能保持节奏感，只是丢掉了中间的次数
                            timer.intervalElapsed -= timer.interval;

                        }
                    }
                    // 【防爆设计】: 累积时间依然大大超过间隔（严重掉帧）
                    // 应该把溢出时间限制住，防止下一帧又立即触发
                    if (timer.intervalElapsed >= timer.interval)
                    {
                        timer.intervalElapsed = 0f; // 丢弃多余的时间
                    }
                }

                if (timer.timeElapsed >= timer.duration)
                {
                    int handleIndex = _indexToHandle[i];
                    _expiredTimersCache.Add(new TimerHandle(handleIndex, _generations[handleIndex]));

                    if (timer.isLoop)
                    {
                        timer.timeElapsed -= timer.duration;
                    }
                }

                // 不需要 _timers[i] = timer; 因为我们操作的是 ref
            }

            // ===========================================
            // 执行间隔回调
            // ===========================================
            for (int i = 0; i < _intervalTimersCache.Count; i++)
            {
                TimerHandle handle = _intervalTimersCache[i];

                // 安全检查：如果追赶了 5 次，在第 1 次回调里用户把它停了，
                // 第 2,3,4,5 次循环走到这里时，IsHandleValid 会返回 false，直接 continue。完美！
                if (!IsHandleValid(handle)) continue;

                int realIndex = _handleToIndex[handle.Index];
                ref TimerData data = ref _timers[realIndex];

                try { data.onTrigger?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            }


            // 批量处理过期/回调
            // 放在循环外处理，安全且逻辑清晰
            for (int i = 0; i < _expiredTimersCache.Count; i++)
            {
                TimerHandle handle = _expiredTimersCache[i];

                // 二次校验：防止在同一个 Tick 中，前一个回调把后一个计时器给停了
                if (!IsHandleValid(handle)) continue;

                int realIndex = _handleToIndex[handle.Index];
                var data = _timers[realIndex];

                // 执行回调 (try-catch 防止用户逻辑报错炸掉计时器系统)
                try
                {
                    data.onComplete?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                // 处理移除 (如果是非循环且依然有效)
                if (!data.isLoop)
                {
                    // 再次校验，因为回调里可能已经手动调用了 StopTimer
                    if (IsHandleValid(handle))
                    {
                        StopTimer(handle);
                    }
                }
            }
        }
    }
}
