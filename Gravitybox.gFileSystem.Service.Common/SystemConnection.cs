using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    /// <summary>
    /// This is a client side facade that acts as a native file system interface
    /// </summary>
    public class SystemConnection : IDisposable
    {
        private ChannelFactory<ISystemCore> _factory = null;
        private ISystemCore _service = null;

        public SystemConnection(string server = "localhost", int port = 1900)
        {
            _factory = GetFactory(server, port);
            _service = _factory.CreateChannel();
        }

        public string WorkingFolder { get; set; } = Path.GetTempPath();

        private ChannelFactory<ISystemCore> GetFactory(string serverName)
        {
            return GetFactory(serverName, 1900);
        }

        private ChannelFactory<ISystemCore> GetFactory(string serverName, int port)
        {
            //var myBinding = new CompressedNetTcpBinding() { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
            var myBinding = new NetTcpBinding() { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
            myBinding.ReaderQuotas.MaxStringContentLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxBytesPerRead = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxArrayLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxDepth = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxNameTableCharCount = 10 * 1024 * 1024;
            myBinding.Security.Mode = SecurityMode.None;
            var myEndpoint = new EndpointAddress("net.tcp://" + serverName + ":" + port + "/__gfile");
            return new ChannelFactory<ISystemCore>(myBinding, myEndpoint);
        }

        public Guid GetOrAddTenant(string name)
        {
            return _service.GetOrAddTenant(name);
        }

        public bool SaveFile(Guid tenantId, string container, string fileName)
        {
            try
            {
                //Save the file
                var fi = new FileInfo(fileName);
                const int blockSize = 1024 * 1024;
                var count = Math.Ceiling((fi.Length * 1.0) / blockSize);

                var block = new FileInformation
                {
                    Container = container,
                    FileName = fileName,
                    TenantID = tenantId,
                    CRC = FileUtilities.FileCRC(fileName),
                };

                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    var token = _service.SendFileStart(block);
                    for (var ii = 1; ii <= count; ii++)
                    {
                        var bb = new byte[blockSize];
                        var c = fs.Read(bb, 0, bb.Length);
                        //If last block is smaller then truncate it
                        if (c < blockSize)
                            bb = bb.Take(c).ToArray();
                        var wasSaved = _service.SendFileData(token, bb);
                    }
                    return _service.SendFileEnd(token);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string GetFile(Guid tenantId, string container, string fileName)
        {
            try
            {
                var token = _service.GetFileStart(tenantId, container, fileName);
                var index = 0;
                var count = 0;
                var tempfile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString());
                using (var fs = File.Open(tempfile, FileMode.CreateNew, FileAccess.Write))
                {
                    do
                    {
                        count = 0;
                        var arr = _service.GetFile(token, index);
                        if (arr != null)
                        {
                            count = arr.Length;
                            fs.Write(arr, 0, arr.Length);
                        }
                        index++;
                    } while (count > 0);
                }
                return tempfile;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public int RemoveFile(Guid tenantId, string container, string fileName)
        {
            return _service.RemoveFile(tenantId, container, fileName);
        }

        public List<string> GetFileList(Guid tenantID, string startPattern = null)
        {
            return _service.GetFileList(tenantID, startPattern);
        }

        void IDisposable.Dispose()
        {
            try
            {
                ((IDisposable)_service).Dispose();
                ((IDisposable)_factory).Dispose();
            }
            catch { }
        }
    }
}