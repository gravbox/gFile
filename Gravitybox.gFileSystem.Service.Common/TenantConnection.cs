using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    public class TenantConnection : ServiceConnectionBase, IDisposable
    {
        protected string _tenantName = null;
        protected string _container = null;
        protected Guid _tenantID = Guid.Empty;

        public TenantConnection(string tenantName, string container, string server = "localhost", int port = 1900)
            : base(server, port)
        {
            if (string.IsNullOrEmpty(tenantName))
                throw new Exception("The tenant name must be set.");
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container name must be set.");

            _tenantName = tenantName;
            _container = container;
        }

        private Guid GetTenantID()
        {
            if (_tenantID == Guid.Empty)
            {
                using (var service = new SystemConnection())
                {
                    _tenantID = service.GetOrAddTenant(_tenantName);
                }
            }
            return _tenantID;
        }

        public virtual bool SaveFile(string fileName)
        {
            using (var service = new SystemConnection())
            {
                return service.SaveFile(this.GetTenantID(), _container, fileName);
            }
        }

        public virtual OutfileItem GetFile(string fileName)
        {
            using (var service = new SystemConnection())
            {
                return service.GetFile(this.GetTenantID(), _container, fileName);
            }
        }

        public virtual int RemoveFile(string fileName)
        {
            using (var service = new SystemConnection())
            {
                return service.RemoveFile(this.GetTenantID(), _container, fileName);
            }
        }

        public virtual int RemoveAll()
        {
            using (var service = new SystemConnection())
            {
                return service.RemoveAll(this.GetTenantID(), _container);
            }
        }

        public virtual List<string> GetFileList(string startPattern = null)
        {
            using (var service = new SystemConnection())
            {
                return service.GetFileList(this.GetTenantID(), startPattern);
            }
        }

        public virtual List<string> GetContainerList(string startPattern = null)
        {
            using (var service = new SystemConnection())
            {
                return service.GetContainerList(this.GetTenantID(), startPattern);
            }
        }

        void IDisposable.Dispose()
        {
            try
            {
            }
            catch { }
        }

    }
}
