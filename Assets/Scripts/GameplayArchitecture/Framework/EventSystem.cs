using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayArchitecture
{
    public enum Priority
    {
        Logic = 0,
        Appearance = 1,
        Max = 2
    }
    public interface IEventArgs
    {
        public Priority Priority { get; set; }
    }
    public abstract class AbstractEventArgs : IEventArgs
    {
        private Priority _priority = Priority.Logic;
        public Priority Priority { get => _priority; set => _priority = value; }
    }

    public class EventSystem : MonoSingleton<EventSystem>
    {
        //最大优先级数
        private readonly int PRIORITY_LEVEL_NUM = (int)Priority.Max + 1;
        //最长事件缓冲区长度
        private readonly int MAX_EVENTQUEUE_LENGTH = 2000;

        //多级事件触发队列
        private RingBuffer<Action>[] _frontBuffers;
        //多级事件缓冲区
        private RingBuffer<Action>[] _backBuffers;
        //需要一个字典，来存储事件对象和回调的对应关系  
        private Dictionary<Type, Delegate> _listenersDic;//Delegate : Action（不带返回）和 Func的 基类。类似一个C++通用函数指针。

        private void Awake()
        {
            _frontBuffers = new RingBuffer<Action>[PRIORITY_LEVEL_NUM];
            _backBuffers = new RingBuffer<Action>[PRIORITY_LEVEL_NUM];
            for (int i = 0; i < PRIORITY_LEVEL_NUM; i++)
            {
                _frontBuffers[i] = new RingBuffer<Action>(MAX_EVENTQUEUE_LENGTH);
                _backBuffers[i] = new RingBuffer<Action>(MAX_EVENTQUEUE_LENGTH);
            }
            _listenersDic = new Dictionary<Type, Delegate>();
        }

        public void Register<T>(Action<T> listener) where T : IEventArgs
        {
            if (listener == null) return;
            Type type = typeof(T);
            
            if (_listenersDic.ContainsKey(type))
            {
                _listenersDic[type] = Delegate.Combine(_listenersDic[type], listener);
            }
            else
            {
                _listenersDic[type] = listener;
            }
        }

        public void UnRegister<T>(Action<T> listener) where T : IEventArgs
        {
            if (listener == null) return;
            Type type = typeof(T);
            
            if (_listenersDic.ContainsKey(type))
            {
                var result = Delegate.Remove(_listenersDic[type], listener);
                if (result != null)
                {
                    _listenersDic[type] = result;
                }
                else
                {
                    _listenersDic.Remove(type);
                }
            }

        }
        public void Trigger<T>(T e) where T : IEventArgs
        {
            Delegate rawDelegate;
           
            if (!_listenersDic.TryGetValue(e.GetType(), out rawDelegate)) return;
            

            if (rawDelegate is Action<T> callback)
            {
                Action task = () => callback(e);
                Priority priority = e.Priority;
                
                _backBuffers[(int)priority].TryEnqueue(task);
            }
        }

        private void Update()
        {
            if (_frontBuffers == null || _backBuffers == null) return;

            var temp = _frontBuffers;
            _frontBuffers = _backBuffers;
            _backBuffers = temp;

            for (int i = 0; i < PRIORITY_LEVEL_NUM; ++i)
            {
                while (_frontBuffers[i].TryDequeue(out Action task))
                {
                    task.Invoke();
                }
            }
        }
    }

    public class ThreadSafeEventSystem : MonoSingleton<ThreadSafeEventSystem>
    {
        //最大优先级数
        private readonly int PRIORITY_LEVEL_NUM = (int)Priority.Max + 1;
        //最长事件缓冲区长度
        private readonly int MAX_EVENTQUEUE_LENGTH = 2000;
 
        //多级事件触发队列(线程安全)
        private ThreadSafeRingBuffer<Action>[] _threadSafeFrontBuffers;
        //多级事件缓冲区(线程安全)
        private ThreadSafeRingBuffer<Action>[] _ThreadSafeBackBuffers;
        //需要一个字典，来存储事件对象和回调的对应关系(线程安全)
        private Dictionary<Type, Delegate> _ThreadSafeListenersDic;

        private object _listenerDicLock = new object();
        private object _swapLock = new object();

        private void Awake()
        {
            _threadSafeFrontBuffers = new ThreadSafeRingBuffer<Action>[PRIORITY_LEVEL_NUM];
            _ThreadSafeBackBuffers = new ThreadSafeRingBuffer<Action>[PRIORITY_LEVEL_NUM];
            for (int i = 0; i < PRIORITY_LEVEL_NUM; i++)
            {
                _threadSafeFrontBuffers[i] = new ThreadSafeRingBuffer<Action>(MAX_EVENTQUEUE_LENGTH);
                _ThreadSafeBackBuffers[i] = new ThreadSafeRingBuffer<Action>(MAX_EVENTQUEUE_LENGTH);
            }
            _ThreadSafeListenersDic = new Dictionary<Type, Delegate>();
        }

        public void Register<T>(Action<T> listener) where T : IEventArgs
        {
            if (listener == null) return;
            Type type = typeof(T);
            lock(_listenerDicLock)
            {
                if (_ThreadSafeListenersDic.ContainsKey(type))
                {
                    _ThreadSafeListenersDic[type] = Delegate.Combine(_ThreadSafeListenersDic[type], listener);
                }
                else
                {
                    _ThreadSafeListenersDic[type] = listener;
                }
            }
        }

        public void UnRegister<T>(Action<T> listener) where T : IEventArgs
        {
            if (listener == null) return;
            Type type = typeof(T);
            lock(_listenerDicLock)
            {
                if (_ThreadSafeListenersDic.ContainsKey(type))
                {
                    var result = Delegate.Remove(_ThreadSafeListenersDic[type], listener);
                    if (result != null)
                    {
                        _ThreadSafeListenersDic[type] = result;
                    }
                    else
                    {
                        _ThreadSafeListenersDic.Remove(type);
                    }
                }
            }
            
        }
        public void Trigger<T>(T e) where T : IEventArgs
        {
            Delegate rawDelegate;
            lock(_listenerDicLock)
            {
                if (!_ThreadSafeListenersDic.TryGetValue(e.GetType(), out rawDelegate)) return;
            }
           
            if (rawDelegate is Action<T> callback)
            {
                Action task = () => callback(e);
                Priority priority = e.Priority;
                lock(_swapLock)
                {
                    _ThreadSafeBackBuffers[(int)priority].TryEnqueue(task);
                }
            }
        }

        private void Update()
        {
            if (_threadSafeFrontBuffers == null || _ThreadSafeBackBuffers == null) return;

            lock(_swapLock)
            {
                var temp = _threadSafeFrontBuffers;
                _threadSafeFrontBuffers = _ThreadSafeBackBuffers;
                _ThreadSafeBackBuffers = temp;
            }

            for (int i = 0; i < PRIORITY_LEVEL_NUM; ++i)
            {
                while (_threadSafeFrontBuffers[i].TryDequeue(out Action task))
                {
                    task.Invoke();
                }
            }
        }
    }
}

