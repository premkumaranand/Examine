using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Examine.LuceneEngine
{
    /// <summary>
    /// A threadsafe list that can be accessed by multiple concurrent threads
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThreadSafeList<T> : IList<T>
    {

        //This is the internal dictionary that we are wrapping
        readonly List<T> _list = new List<T>();

        [NonSerialized] readonly ReaderWriterLockSlim _listLock = Locks.GetLockInstance(LockRecursionPolicy.NoRecursion); //setup the lock;

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotSupportedException("Cannot enumerate a threadsafe list.  Instead, enumerate via the count & index of the list");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException("Cannot enumerate a threadsafe list.  Instead, enumerate via the count & index of the list");
        }

        public void AddRange(IEnumerable<T> items)
        {
            using (new WriteLock(this._listLock))
            {
                this._list.AddRange(items);
            }
        }

        public void Add(T item)
        {
            using (new WriteLock(this._listLock))
            {
                this._list.Add(item);
            }
        }

        public virtual void Clear()
        {
            using (new WriteLock(this._listLock))
            {
                this._list.Clear();
            }
        }

        public bool Contains(T item)
        {
            using (new ReadOnlyLock(this._listLock))
            {
                return this._list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (new ReadOnlyLock(this._listLock))
            {
                this._list.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            using (new WriteLock(this._listLock))
            {
                return this._list.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                using (new ReadOnlyLock(this._listLock))
                {
                    return this._list.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            using (new ReadOnlyLock(this._listLock))
            {
                return this._list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Cannot Insert into a threadsafe list");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Cannot RemoveAt from a threadsafe list");
        }

        public T this[int index]
        {
            get
            {
                using (new ReadOnlyLock(this._listLock))
                {
                    return this._list[index];
                }
            }
            set
            {
                using (new WriteLock(this._listLock))
                {
                    this._list[index] = value;
                }
            }
        }
    }
}