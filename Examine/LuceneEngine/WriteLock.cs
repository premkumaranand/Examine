using System.Threading;

namespace Examine.LuceneEngine
{
    public class WriteLock : BaseLock
    {
        public WriteLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetWriteLock(this._Locks);
        }


        public override void Dispose()
        {
            Locks.ReleaseWriteLock(this._Locks);
        }
    }
}