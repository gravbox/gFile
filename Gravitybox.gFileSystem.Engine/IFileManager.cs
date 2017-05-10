using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Engine
{
    public interface IFileManager
    {
        Guid GetOrAddTenant(string name);

        Guid TenantExists(string name);

        Guid TenantExists(Guid id);

        List<string> GetFileList(Guid tenantID, string startPattern = null);

        List<string> GetContainerList(Guid tenantID, string startPattern = null);

        int RekeyTenant(Guid tenantID);

        bool SaveFile(Guid tenantID, string container, string fileName);

        string GetFile(Guid tenantID, string container, string fileName);

        int RemoveFile(Guid tenantID, string container, string fileName);

        int RemoveAll(Guid tenantID, string container);
    }
}
