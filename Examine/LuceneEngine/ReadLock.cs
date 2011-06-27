using System.Threading;

namespace Examine.LuceneEngine
{
    public class ReadLock : BaseLock
    {
        public ReadLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetReadLock(this._Locks);
        }


        public override void Dispose()
        {
            Locks.ReleaseReadLock(this._Locks);
        }
    }
}