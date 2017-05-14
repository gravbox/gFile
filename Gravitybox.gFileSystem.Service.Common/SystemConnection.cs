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
        protected byte[] _masterKey = null;
        protected string _server = null;
        protected int _port = 0;

        public delegate void FileUploadEventHandler(object sender, FileProgressEventArgs e);

        public event FileUploadEventHandler FileUpload;
        public event FileUploadEventHandler FileDownload;

        protected virtual void OnFileUpload(FileProgressEventArgs e)
        {
            if (this.FileUpload != null)
                this.FileUpload(this, e);
        }

        protected virtual void OnFileDownload(FileProgressEventArgs e)
        {
            if (this.FileDownload != null)
                this.FileDownload(this, e);
        }

        public SystemConnection(byte[] masterKey, string server = "localhost", int port = 1900)
        {
            if (masterKey == null || masterKey.Length != 32)
                throw new Exception("Invalid master key");

            _server = server;
            _port = port;
            _masterKey = masterKey;
        }

        /// <summary>
        /// The folder where temp operations are performed
        /// </summary>
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

        /// <summary>
        /// Adds the tenant if not exists and returns its unqiue ID
        /// </summary>
        public Guid GetOrAddTenant(string name)
        {
            Guid retval = Guid.Empty;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.GetOrAddTenant(_masterKey, name);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Saves a file to storage for a tenant in the specified container
        /// </summary>
        public bool SaveFile(Guid tenantId, string container, string fileName)
        {
            try
            {
                //Save the file
                var fi = new FileInfo(fileName);
                const int blockSize = 1024 * 1024;
                var count = (int)Math.Ceiling((fi.Length * 1.0) / blockSize);

                var block = new FileInformation
                {
                    Container = container,
                    FileName = fileName,
                    TenantID = tenantId,
                    CRC = FileUtilities.FileCRC(fileName),
                    Size = fi.Length,
                    CreatedTime = fi.CreationTime.ToUniversalTime(),
                    ModifiedTime = fi.LastWriteTime.ToUniversalTime(),
                };

                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    FileDataInfo fileInfo = null;
                    RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            using (var factory = GetFactory(_server, _port))
                            {
                                var service = factory.CreateChannel();
                                fileInfo = service.SendFileStart(block);
                            }
                        });

                    for (var ii = 0; ii < count; ii++)
                    {
                        var bb = new byte[blockSize];
                        var c = fs.Read(bb, 0, bb.Length);
                        //If last block is smaller then truncate it
                        if (c < blockSize)
                            bb = bb.Take(c).ToArray();

                        RetryHelper.DefaultRetryPolicy(3)
                            .Execute(() =>
                            {
                                using (var factory = GetFactory(_server, _port))
                                {
                                    var service = factory.CreateChannel();
                                    var wasSaved = service.SendFileData(fileInfo.Token, bb, ii);
                                }
                            });

                        this.OnFileUpload(new FileProgressEventArgs
                        {
                            ChunkIndex = ii,
                            Container = container,
                            FileName = fileName,
                            TotalChunks = count,
                        });

                    }

                    var retval = false;
                    RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            using (var factory = GetFactory(_server, _port))
                            {
                                var service = factory.CreateChannel();
                                retval = service.SendFileEnd(_masterKey, fileInfo.Token);
                            }
                        });
                    return retval;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a file from storage for a tenant in the specified container
        /// </summary>
        /// <param name="_masterKey"></param>
        /// <param name="tenantId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFile(byte[] _masterKey, Guid tenantId, string container, string fileName)
        {
            try
            {
                FileDataInfo fileInfo = null;
                RetryHelper.DefaultRetryPolicy(3)
                        .Execute(() =>
                        {
                            using (var factory = GetFactory(_server, _port))
                            {
                                var service = factory.CreateChannel();
                                fileInfo = service.GetFileStart(_masterKey, tenantId, container, fileName);
                            }
                        });

                if (fileInfo.Token == Guid.Empty)
                    return null;

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
                                using (var factory = GetFactory(_server, _port))
                                {
                                    var service = factory.CreateChannel();
                                    arr = service.GetFilePart(fileInfo.Token, index);

                                    if (arr != null)
                                    {
                                        this.OnFileDownload(new FileProgressEventArgs
                                        {
                                            ChunkIndex = index,
                                            Container = container,
                                            FileName = fileName,
                                            TotalChunks = count,
                                        });
                                    }
                                }
                            });

                        if (arr != null)
                        {
                            count = arr.Length;
                            fs.Write(arr, 0, arr.Length);
                        }
                        index++;
                    } while (count > 0);
                }

                var fi = new FileInfo(tempfile);
                fi.CreationTime = fileInfo.CreatedTime;
                fi.LastWriteTime = fileInfo.ModifiedTime;

                return tempfile;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Removes a file from storage for a tenant in the specified container
        /// </summary>
        public int RemoveFile(Guid tenantId, string container, string fileName)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RemoveFile(_masterKey, tenantId, container, fileName);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Removes all files for a tenant and container
        /// </summary>
        public int RemoveAll(Guid tenantId, string container)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RemoveAll(_masterKey, tenantId, container);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Gets a list of existing files in storage for a tenant
        /// </summary>
        public List<string> GetFileList(byte[] _masterKey, Guid tenantID, string startPattern = null)
        {
            List<string> retval = null;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.GetFileList(_masterKey, tenantID, startPattern);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Resets a tenant key and resets all files for the tenant
        /// </summary>
        public int RekeyTenant(Guid tenantID)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RekeyTenant(_masterKey, tenantID);
                    }
                });
            return retval;
            
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