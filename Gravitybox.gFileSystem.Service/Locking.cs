using Gravitybox.gFileSystem.EFDAL;
using Gravitybox.gFileSystem.EFDAL.Entity;
using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    internal static class LockingManager
    {
        private static Dictionary<Guid, DatastoreLock> _lockCache = new Dictionary<Guid, DatastoreLock>();
        private static object _locker = new object();

        internal static DatastoreLock GetLocker(Guid id)
        {
            lock (_locker)
            {
                if (!_lockCache.ContainsKey(id))
                    _lockCache.Add(id, new DatastoreLock(id));
                return _lockCache[id];
            }
        }
    }

    #region IDataLock

    internal interface IDataLock
    {
        int LockTime { get; }

        int WaitingLocksOnEntry { get; }

        int ReadLockCount { get; }
    }

    #endregion

    #region AcquireReaderLock

    internal class ReaderLock : IDataLock, IDisposable
    {
        private DatastoreLock m_Lock = null;
        private bool m_Disposed = false;
        private static long _counter = 0;
        private long _lockIndex = 0;
        private DateTime _initTime = DateTime.Now;
        private const int TimeOut = 60000;
        private bool _inError = false;

        /// <summary />
        public ReaderLock(Guid id, string traceInfo)
        {
            this.LockTime = -1;
            m_Lock = LockingManager.GetLocker(id);

            this.ReadLockCount = m_Lock.CurrentReadCount;
            this.WaitingLocksOnEntry = m_Lock.WaitingWriteCount;
            if (this.WaitingLocksOnEntry > 10)
                Logger.LogWarning("Waiting Writer Locks: Count=" + this.WaitingLocksOnEntry + ", RepositoryId=" + id);

            if (!m_Lock.TryEnterReadLock(TimeOut))
            {
                _inError = true;

                throw new Exception("Could not get reader lock: " +
                    ((m_Lock.ObjectId == Guid.Empty) ? string.Empty : "ID=" + m_Lock.ObjectId) +
                    ", CurrentReadCount=" + m_Lock.CurrentReadCount +
                    ", WaitingReadCount=" + m_Lock.WaitingReadCount +
                    ", WaitingWriteCount=" + m_Lock.WaitingWriteCount +
                    ", HoldingThread=" + m_Lock.HoldingThreadId +
                    ", TraceInfo=" + m_Lock.TraceInfo +
                    ", LockFailTime=" + (int)DateTime.Now.Subtract(_initTime).TotalMilliseconds +
                    ", WriteHeldTime=" + m_Lock.WriteHeldTime);
            }

            this.LockTime = (int)DateTime.Now.Subtract(_initTime).TotalMilliseconds;
            Interlocked.Increment(ref _counter);
            _lockIndex = _counter;
            m_Lock.HeldReads.AddOrUpdate(_lockIndex, DateTime.Now, (key, value) => DateTime.Now);
            m_Lock.TraceInfo = traceInfo;
            m_Lock.HoldingThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public int LockTime { get; private set; }

        /// <summary>
        /// Returns the number of write locks that were in queue when creating this lock
        /// </summary>
        public int WaitingLocksOnEntry { get; private set; }

        /// <summary>
        /// Returns the number of read locks held on entry
        /// </summary>
        public int ReadLockCount { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary />
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing && m_Lock != null)
                {
                    var traceInfo = m_Lock.TraceInfo;
                    var totalTime = DateTime.Now.Subtract(_initTime);
                    if (!_inError)
                    {
                        DateTime dt;
                        if (!m_Lock.HeldReads.TryRemove(_lockIndex, out dt))
                            Logger.LogWarning("HeldReads was not released. ObjectId=" + m_Lock.ObjectId + ", Index=" + _lockIndex + ", TraceInfo=" + m_Lock.TraceInfo + ", Elapsed=" + totalTime.TotalMilliseconds);
                        m_Lock.TraceInfo = null;
                        m_Lock.HoldingThreadId = null;
                    }

                    m_Lock.ExitReadLock();

                    if (totalTime.TotalSeconds > 60)
                        Logger.LogWarning("ReaderLock Long: Elapsed=" + totalTime.TotalSeconds);
                }
            }
            m_Disposed = true;
        }
    }

    #endregion

    #region AcquireWriterLock

    /// <summary />
    internal class WriterLock : IDataLock, IDisposable
    {
        private DatastoreLock m_Lock = null;
        private bool m_Disposed = false;
        private DateTime _initTime = DateTime.Now;
        private const int TimeOut = 60000;
        private bool _inError = false;

        public WriterLock(Guid id)
            : this(id, string.Empty)
        {
        }

        public WriterLock(Guid id, string traceInfo)
        {
            if (id == Guid.Empty) return;

            m_Lock = LockingManager.GetLocker(id);

            this.ReadLockCount = m_Lock.CurrentReadCount;
            this.WaitingLocksOnEntry = m_Lock.WaitingWriteCount;
            if (this.WaitingLocksOnEntry > 10)
                Logger.LogWarning("Waiting Writer Locks: Count=" + this.WaitingLocksOnEntry + ", RepositoryId=" + id);

            if (!m_Lock.TryEnterWriteLock(TimeOut))
            {
                _inError = true;

                var lapses = string.Join("-", m_Lock.HeldReads.Values.ToList().Select(x => (int)DateTime.Now.Subtract(x).TotalSeconds).ToList());
                throw new Exception("Could not get writer lock: " +
                    ((m_Lock.ObjectId == Guid.Empty) ? string.Empty : "ID=" + m_Lock.ObjectId) +
                    ", CurrentReadCount=" + m_Lock.CurrentReadCount +
                    ", WaitingReadCount=" + m_Lock.WaitingReadCount +
                    ", WaitingWriteCount=" + m_Lock.WaitingWriteCount +
                    ", HoldingThread=" + m_Lock.HoldingThreadId +
                    ", TraceInfo=" + m_Lock.TraceInfo +
                    ", WriteHeldTime=" + m_Lock.WriteHeldTime +
                    ", LockFailTime=" + (int)DateTime.Now.Subtract(_initTime).TotalMilliseconds +
                    ", Lapses=" + lapses);
            }

            this.LockTime = (int)DateTime.Now.Subtract(_initTime).TotalMilliseconds;
            m_Lock.TraceInfo = traceInfo;
            m_Lock.WriteLockHeldTime = DateTime.Now;
            m_Lock.HoldingThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public int LockTime { get; private set; }

        /// <summary>
        /// Returns the number of write locks that were in queue when creating this lock
        /// </summary>
        public int WaitingLocksOnEntry { get; private set; }

        /// <summary>
        /// Returns the number of read locks held on entry
        /// </summary>
        public int ReadLockCount { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary />
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed && disposing && m_Lock != null)
            {
                var traceInfo = m_Lock.TraceInfo;
                if (!_inError)
                {
                    m_Lock.WriteLockHeldTime = null;
                    m_Lock.TraceInfo = null;
                    m_Lock.HoldingThreadId = null;
                }

                m_Lock.ExitWriteLock();

                var totalTime = (int)DateTime.Now.Subtract(_initTime).TotalSeconds;
                if (totalTime > 60)
                    Logger.LogWarning("WriterLock Long: Elapsed=" + totalTime);
            }
            m_Disposed = true;
        }
    }

    #endregion

    #region DatastoreLock

    internal class DatastoreLock : System.Threading.ReaderWriterLockSlim
    {
        public DatastoreLock(Guid objectId)
            : base(LockRecursionPolicy.SupportsRecursion)
        {
            this.ObjectId = objectId;
        }

        public bool AnyLocks()
        {
            return (this.CurrentReadCount == 0) && !this.IsWriteLockHeld;
        }

        public int WriteHeldTime
        {
            get
            {
                var retval = -1;
                if (this.WriteLockHeldTime.HasValue)
                    retval = (int)DateTime.Now.Subtract(this.WriteLockHeldTime.Value).TotalMilliseconds;
                return retval;
            }
        }

        public DateTime? WriteLockHeldTime { get; internal set; }

        public string TraceInfo { get; internal set; }

        public Guid LockID { get; private set; } = Guid.NewGuid();

        public Guid ObjectId { get; private set; }

        public int? HoldingThreadId { get; internal set; }

        public System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> HeldReads { get; private set; } = new System.Collections.Concurrent.ConcurrentDictionary<long, DateTime>();
    }

    #endregion

}