using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Gravitybox.gFileSystem.Service.Common
{
    [ServiceContract]
    public interface ISystemCore
    {
        [OperationContract]
        Guid GetOrAddTenant(string name);

        [OperationContract]
        Guid SendFileStart(FileInformation block);

        [OperationContract]
        bool SendFileData(Guid token, byte[] data);

        [OperationContract]
        bool SendFileEnd(Guid token);

        [OperationContract]
        Guid GetFileStart(Guid tenantId, string container, string fileName);

        [OperationContract]
        byte[] GetFile(Guid token, int index);

        [OperationContract]
        int RemoveFile(Guid tenantId, string container, string fileName);
    }

}
