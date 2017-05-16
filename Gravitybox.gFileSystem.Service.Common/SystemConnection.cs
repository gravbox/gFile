using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    /// <summary>
    /// This is a client side facade that acts as a native file system interface
    /// </summary>
    public class SystemConnection : ServiceConnectionBase, IDisposable
    {
        public SystemConnection(string server = "localhost", int port = 1900)
            : base(server, port)
        {
        }

        /// <summary>
        /// Adds the tenant if not exists and returns its unqiue ID
        /// </summary>
        public virtual Guid GetOrAddTenant(string name)
        {
            Guid retval = Guid.Empty;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.GetOrAddTenant(name);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Saves a file to storage for a tenant in the specified container
        /// </summary>
        public virtual bool SaveFile(Guid tenantId, string container, string fileName)
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
                                retval = service.SendFileEnd(fileInfo.Token);
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
        public virtual OutfileItem GetFile(Guid tenantId, string container, string fileName)
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
                                fileInfo = service.GetFileStart(tenantId, container, fileName);
                            }
                        });

                if (fileInfo.Token == Guid.Empty)
                    return null;

                var index = 0;
                var count = 0;
                var tempfile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString());

                var aes = FileUtilities.CryptoProvider(FileUtilities.GenerateKey(), FileUtilities.GenerateIV());
                var newfs = File.Create(tempfile);
                using (var cryptoStream = new CryptoStream(newfs, aes.CreateEncryptor(), CryptoStreamMode.Write))
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
                            cryptoStream.Write(arr, 0, arr.Length);
                        }
                        index++;
                    } while (count > 0);
                }

                newfs = File.OpenRead(tempfile);
                var outStream = new CryptoStream(newfs, aes.CreateDecryptor(), CryptoStreamMode.Read);
                return new OutfileItem
                {
                    EncryptedFileName = tempfile,
                    EncryptedStream = outStream,
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Removes a file from storage for a tenant in the specified container
        /// </summary>
        public virtual int RemoveFile(Guid tenantId, string container, string fileName)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RemoveFile(tenantId, container, fileName);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Removes all files for a tenant and container
        /// </summary>
        public virtual int RemoveAll(Guid tenantId, string container)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RemoveAll(tenantId, container);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Gets a list of existing files in storage for a tenant
        /// </summary>
        public virtual List<string> GetFileList(Guid tenantID, string startPattern = null)
        {
            List<string> retval = null;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.GetFileList(tenantID, startPattern);
                    }
                });
            return retval;
        }

        public virtual List<string> GetContainerList(Guid tenantID, string startPattern = null)
        {
            List<string> retval = null;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.GetContainerList(tenantID, startPattern);
                    }
                });
            return retval;
        }

        /// <summary>
        /// Resets a tenant key and resets all files for the tenant
        /// </summary>
        public virtual int RekeyTenant(Guid tenantID)
        {
            var retval = 0;
            RetryHelper.DefaultRetryPolicy(3)
                .Execute(() =>
                {
                    using (var factory = GetFactory(_server, _port))
                    {
                        var service = factory.CreateChannel();
                        retval = service.RekeyTenant(tenantID);
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