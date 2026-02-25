using System;
namespace GamePlayArchitecture
{
    public class ThreadSafeRingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _capacity;
        private readonly object _lockObject = new object();

        private int _head;
        private int _tail;
        private int _size;

        public ThreadSafeRingBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be greater than 0");
            _capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public bool TryEnqueue(T e)
        {
            lock (_lockObject)
            {
                if (_size >= _capacity)
                {
                    return false; 
                }
                _buffer[_tail++] = e;
                _tail %= _capacity;
                _size++;
            }
            return true;
        }

        public bool TryDequeue(out T e)
        {
            lock(_lockObject)
            {
                if (_size == 0)
                {
                    e = default;
                    return false;
                }
                e = _buffer[_head];
                if (!typeof(T).IsValueType)
                {
                    _buffer[_head] = default;
                }
                _head = (_head + 1) % _capacity;
                --_size;

                return true;
            }
        }

        public bool TryPeek(out T e)
        {
            lock(_lockObject)
            {
                if (_size == 0)
                {
                    e = default;
                    return false;
                }
                e = _buffer[_head];
                return true;
            }
        }

        public int Count
        {
            get
            {
                lock(_lockObject)
                {
                    return _size;
                }
            }
        }
    }
}
