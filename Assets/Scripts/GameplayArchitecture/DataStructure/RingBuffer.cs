using System;

public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _capacity;

    private int _head;
    private int _tail;
    private int _size;

    public RingBuffer(int capacity)
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
        if (_size >= _capacity)
        {
            return false;
        }
        _buffer[_tail++] = e;
        _tail %= _capacity;
        _size++;
        return true;
    }

    public bool TryDequeue(out T e)
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

    public bool TryPeek(out T e)
    {
        if (_size == 0)
        {
            e = default;
            return false;
        }
        e = _buffer[_head];
        return true;
        
    }

    public int Count
    {
        get
        {
            return _size;
        }
    }
}
