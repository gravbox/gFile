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
        Guid GetOrAddTenant(byte[] _masterKey, string name);

        [OperationContract]
        FileDataInfo SendFileStart(FileInformation block);

        [OperationContract]
        bool SendFileData(Guid token, byte[] data, int index);

        [OperationContract]
        bool SendFileEnd(byte[] _masterKey, Guid token);

        [OperationContract]
        FileDataInfo GetFileStart(byte[] _masterKey, Guid tenantId, string container, string fileName);

        [OperationContract]
        byte[] GetFilePart(Guid token, int index);

        [OperationContract]
        int RemoveFile(byte[] _masterKey, Guid tenantId, string container, string fileName);

        [OperationContract]
        List<string> GetFileList(byte[] _masterKey, Guid tenantID, string startPattern = null);

        [OperationContract]
        int RemoveAll(byte[] _masterKey, Guid tenantID, string container);

        [OperationContract]
        int RekeyTenant(byte[] _masterKey, Guid tenantID);
    }

}
