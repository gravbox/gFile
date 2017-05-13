using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        private byte[] _masterKey = null;

        public SystemConnection(byte[] masterKey, string server = "localhost", int port = 1900)
        {
            if (masterKey == null)
                throw new Exception("Invalid master key");
            if (masterKey.Length != 16 && masterKey.Length != 32)
                throw new Exception("Invalid master key");

            _masterKey = masterKey;
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
            Guid retval = Guid.Empty;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    retval = _service.GetOrAddTenant(_masterKey, name);
                });
            return retval;
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
                    Size = fi.Length,
                };

                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    Guid token = Guid.Empty;
                    RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            token = _service.SendFileStart(block);
                        });

                    for (var ii = 1; ii <= count; ii++)
                    {
                        var bb = new byte[blockSize];
                        var c = fs.Read(bb, 0, bb.Length);
                        //If last block is smaller then truncate it
                        if (c < blockSize)
                            bb = bb.Take(c).ToArray();

                        RetryHelper.DefaultRetryPolicy(3)
                            .Execute(() =>
                            {
                                var wasSaved = _service.SendFileData(token, bb, ii);
                            });

                    }

                    var retval = false;
                    RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            retval = _service.SendFileEnd(_masterKey, token);
                        });
                    return retval;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string GetFile(byte[] _masterKey, Guid tenantId, string container, string fileName)
        {
            try
            {
                Guid token = Guid.Empty;
                RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            token = _service.GetFileStart(_masterKey, tenantId, container, fileName);
                        });

                var index = 0;
                var count = 0;
                var tempfile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString());
                using (var fs = File.Open(tempfile, FileMode.CreateNew, FileAccess.Write))
                {
                    do
                    {
                        count = 0;
                        byte[] arr = null;
                        RetryHelper.DefaultRetryPolicy(3)
                            .Execute(() =>
                            {
                                arr = _service.GetFilePart(token, index);
                            });

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
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    retval = _service.RemoveFile(_masterKey, tenantId, container, fileName);
                });
            return retval;
        }

        public int RemoveAll(Guid tenantId, string container)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    retval = _service.RemoveAll(_masterKey, tenantId, container);
                });
            return retval;
        }

        public List<string> GetFileList(byte[] _masterKey, Guid tenantID, string startPattern = null)
        {
            List<string> retval = null;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    retval = _service.GetFileList(_masterKey, tenantID, startPattern);
                });
            return retval;
        }

        public int RekeyTenant(Guid tenantID)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    retval = _service.RekeyTenant(_masterKey, tenantID);
                });
            return retval;
            
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