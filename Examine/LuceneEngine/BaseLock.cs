using System;
using System.Threading;

namespace Examine.LuceneEngine
{
    public abstract class BaseLock : IDisposable
    {
        protected ReaderWriterLockSlim _Locks;


        public BaseLock(ReaderWriterLockSlim locks)
        {
            _Locks = locks;
        }


        public abstract void Dispose();
    }
}