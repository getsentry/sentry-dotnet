using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Internal
{
    // Inefficient, but okay for our use case
    internal class SynchronizedList<T> : IList<T>
    {
        private readonly object _lock = new object();
        private readonly List<T> _innerList;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _innerList.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public SynchronizedList(List<T> innerList) =>
            _innerList = innerList;

        public SynchronizedList() : this(new List<T>()) {}

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                return _innerList.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item)
        {
            lock (_lock)
            {
                _innerList.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _innerList.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _innerList.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _innerList.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _innerList.Remove(item);
            }
        }

        public int IndexOf(T item)
        {
            lock (_lock)
            {
                return _innerList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _innerList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _innerList.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return _innerList[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    _innerList[index] = value;
                }
            }
        }
    }
}
