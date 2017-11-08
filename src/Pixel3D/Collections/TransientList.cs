using System;
using System.Collections.Generic;

namespace Pixel3D.Collections
{
    // We want to be able to return temporary lists to methods that are recursive. We don't want the lists on the GC. So we pack into a single buffer.
    // We expect the underlying buffer to get reset at safe points (typically: UpdateContext resets)

    struct TransientList<T> : IEnumerable<T>
    {
        public TransientList(List<T> storage)
        {
            this.storage = storage;
            this.start = storage.Count;
            this.count = 0;
        }

        public List<T> storage;
        public int start;
        public int count;

        public int Count { get { return count; } }

        public void Add(T item)
        {
            if(start + count != storage.Count)
                throw new InvalidOperationException("Transient list has been closed!");

            storage.Add(item);
            count++;
        }


        #region Enumeration

        struct Enumerator : IEnumerator<T>
        {
            public List<T> storage;
            public int i;
            public int remaining;

            public T Current { get { return storage[i]; } }

            public bool MoveNext()
            {
                if(remaining > 0)
                {
                    i++;
                    remaining--;
                    return true;
                }
                return false;
            }

            object System.Collections.IEnumerator.Current { get { return Current; } }
            public void Dispose() { }
            public void Reset() { throw new NotImplementedException(); }
        }

        public IEnumerator<T> GetEnumerator() { return new Enumerator { i = start-1, remaining = count, storage = storage }; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion

    }
}
