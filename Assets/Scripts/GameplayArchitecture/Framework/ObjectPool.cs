using System.Collections.Generic;

namespace GamePlayArchitecture
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnRecycle();
    }
    public struct PoolObject<T> where T : class, IPoolable, new()
    {
        public T elem;
        public int index;
        public PoolObject(T elem, int index)
        {
            this.elem = elem;
            this.index = index;
        }
    }
    //暂时不打算做多线程的支持
    public class ObjectPool<T> where T : class, IPoolable, new()
    {
        //只有值类型可以申请连续内存，所以用struct包起来而不使用Tuple
        struct Elem
        {
            public T elem;
            public bool IsAvailable;
        }
        //打算设计成可动态扩容的对象池，所以不打算设置为readonly
        private int _capacity;
        private Elem[] _pool;

        //池中剩余可用对象下标
        private Stack<int> _availableIndexStack;
        public ObjectPool(int initialCapacity)
        {
            _capacity = initialCapacity;
            _pool = new Elem[_capacity];//申请一段固定大小的连续内存
            _availableIndexStack = new Stack<int>(_capacity);//申请一个下标栈
            for (int i = 0; i < _capacity; ++i)
            {
                _pool[i].elem = new T();
                _pool[i].IsAvailable = true;
                _availableIndexStack.Push(i);
            }
        }

        public bool Acquire(out PoolObject<T> obj)
        {
            if (_availableIndexStack.TryPop(out int index))
            {
                obj = new PoolObject<T>(_pool[index].elem, index);
                _pool[index].IsAvailable = false; 
                obj.elem.OnSpawn();
                return true;
            }
            else
            {
                if (ReAllocate(_capacity * 2))
                {
                    if (_availableIndexStack.TryPop(out int indexNew))
                    {
                        obj = new PoolObject<T>(_pool[indexNew].elem, indexNew);
                        _pool[indexNew].IsAvailable = false;
                        obj.elem.OnSpawn();
                        return true;
                    }
                }
            }
            obj = default;
            return false;
        }

        public bool Recycle(ref PoolObject<T> obj)
        {
            if (obj.index < 0 || obj.index >= _capacity) return false;
            if (_pool[obj.index].IsAvailable) return false;
            obj.elem.OnRecycle();
            obj.elem = default(T);
            _pool[obj.index].IsAvailable = true;
            _availableIndexStack.Push(obj.index);
            return true;
        }

        private bool ReAllocate(int newCapacity)
        {
            Elem[] newPool = new Elem[newCapacity];
            //深拷贝
            for (int i = 0; i < _capacity; ++i)
            {
                newPool[i] = _pool[i];
            }
            for (int i = _capacity; i < newCapacity; ++i)
            {
                newPool[i].elem = new T();
                newPool[i].IsAvailable = true;
                _availableIndexStack.Push(i);
            }
            _capacity = newCapacity;
            _pool = newPool;
            return true;
        }
    }
}

