using Gravitybox.gFileSystem.EFDAL;
using Gravitybox.gFileSystem.EFDAL.Entity;
using Gravitybox.gFileSystem.Engine;
using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Manager
{
    /// <summary>
    /// This is a database thread locking class where locks are held in the database
    /// This ensures that applications on multiple machines do not interfere with storage
    /// </summary>
    internal abstract class LockBase : IDisposable
    {
        protected const int TimeoutException = 60000;
        protected string _connectionString = null;
        protected long LockId = 0;
        protected long Hash = 0;
        protected DateTime _startedDate = DateTime.Now;

        internal LockBase(Guid key, string item, string connectionString, bool isWrite)
        {
            _connectionString = connectionString;

            //If empty then lock whole tenant
            long hash = 0;
            if (!string.IsNullOrEmpty(item))
                hash = FileUtilities.Hash(Encoding.UTF8.GetBytes(item));

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter { DbType = DbType.Guid, IsNullable = false, ParameterName = "@key", Value = key });
            parameters.Add(new SqlParameter { DbType = DbType.Boolean, IsNullable = false, ParameterName = "@iswrite", Value = isWrite });
            parameters.Add(new SqlParameter { DbType = DbType.Int64, IsNullable = false, ParameterName = "@hash", Value = hash });

            //Try to get a lock and if not loop and try again
            long lockId = (long)SqlHelper.ExecuteWithReturn(_connectionString, "[GetLock] @key, @iswrite, @hash", parameters);
            if (lockId == 0)
            {
                do
                {
                    System.Threading.Thread.Sleep(100);
                    lockId = (long)SqlHelper.ExecuteWithReturn(_connectionString, "[GetLock] @key, @iswrite, @hash", parameters);
                } while (lockId == 0 && DateTime.Now.Subtract(_startedDate).TotalMilliseconds >= TimeoutException);
            }

            if (lockId == 0)
                throw new Exception("Timeout error");

            this.LockId = lockId;

        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            using (var context = new gFileSystemEntities(_connectionString))
            {
                context.ThreadLock.Where(x => x.ID == this.LockId).Delete();
                context.SaveChanges();
            }
        }
    }

    internal class ReaderLock : LockBase
    {
        public ReaderLock(Guid key, string item, string connectionString)
            : base(key, item, connectionString, false)
        {
        }
    }

    internal class WriterLock : LockBase
    {
        public WriterLock(Guid key, string item, string connectionString)
            : base(key, item, connectionString, true)
        {
        }
    }

}