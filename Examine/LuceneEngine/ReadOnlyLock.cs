using System.Threading;

namespace Examine.LuceneEngine
{
    public class ReadOnlyLock : BaseLock
    {
        public ReadOnlyLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetReadOnlyLock(this._Locks);
        }


        public override void Dispose()
        {
            Locks.ReleaseReadOnlyLock(this._Locks);
        }
    }
}