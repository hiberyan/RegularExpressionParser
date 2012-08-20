using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegularExpression
{

    public class Deque<T> : IEnumerable<T>
    {
        private T[] _array;
        private const int _defaultCapacity = 4;

        private int _head;
        private int _tail;
        private int _count;
     
        public Deque():this(_defaultCapacity)
        {
        }

        public Deque(int capacity)
        {
            this._array = new T[capacity];

            _head = 0;
            _tail = 0;
            _count = 0;

        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public  void PushFront(T item)
        {
            if (this._count == this._array.Length)
            {
                var capacity = this._array.Length * 2;
                SetCapacity(capacity);
            }
            if (this._head > 0 && this._head != (this._tail + 1))
            {
                _array[_head - 1] = item;
                _head -= 1;
            }
            else // Inset at the begining
            {
                _head = _array.Length - 1;
                _array[_array.Length - 1] = item;
            }
            _count += 1;

        }

        public void PushBack(T item)
        {
            if (this._count == this._array.Length)
            {
                var capacity = this._array.Length * 2;
                SetCapacity(capacity);
            }
            _array[_tail] = item;
             _tail = (_tail + 1) % _array.Length;
            _count += 1;
        }

        public T PopFront()
        {
            if (_count == 0)
                throw new InvalidOperationException();
            var item = _array[_head];
            _head = (_head + 1) % _array.Length;
            _count -= 1;
            return item;
        }

        public T PopBack()
        {
            if (_count == 0)
                throw new InvalidOperationException();
            _tail = _tail - 1;
            if (_tail < 0)
                _tail = _array.Length - 1;
            _count -= 1;
            return _array[_tail];
          
        }

        public T PeekFront()
        {
            if (_count == 0)
                throw new InvalidOperationException();
            return _array[_head];
        }

        public T PeekBack()
        {
            if (_count == 0)
                throw new InvalidOperationException();
            if (_tail > 0)
                return _array[_tail - 1];
            else
                return _array[_array.Length - 1];
        }

        public IEnumerator<T> GetEnumerator()
        {
            var tail = _tail > _head ? _tail : (_tail + _array.Length);
            for (int i = _head; i < tail; i++)
            {
                yield return _array[i %_array.Length];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void SetCapacity(int capacity)
        {
            var newArray = new T[capacity];
            var tail = _tail > _head ? _tail : (_tail + _array.Length);
            int index = 0;
            for (int i = _head; i < tail; i++)
            {
                newArray[index] = _array[i % _array.Length];
                index++; 
            }
            _array = newArray;
            _tail = tail - _head;
            _head = 0;
        }
    }
}
