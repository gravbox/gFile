using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Gravitybox.gFileSystem.Service
{
    [Serializable()]
    [KnownType(typeof(FileDataInfo))]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class SystemCore : MarshalByRefObject, ISystemCore, IDisposable
    {
        //TODO: handle cache somewhere else
        private static Dictionary<Guid, FilePartCache> _fileUploadPartCache = new Dictionary<Guid, FilePartCache>();
        private static HashSet<string> _fileUploadCache = new HashSet<string>();
        private static Dictionary<Guid, FilePartCache> _fileDownloadCache = new Dictionary<Guid, FilePartCache>();

        /// <summary>
        /// Adds the tenant if not exists and returns its unqiue ID
        /// </summary>
        public Guid GetOrAddTenant(string name)
        {
            //Create the manager object
            using (var fm = new FileManager())
            {
                return fm.GetOrAddTenant(name);
            }
        }

        /// <summary>
        /// Initialize a file for upload
        /// </summary>
        /// <returns>Token that is used to append data in chunk</returns>
        public FileDataInfo SendFileStart(FileInformation block)
        {
            if (block == null)
                throw new Exception("Fie information not set");
            if (block.TenantID == Guid.Empty)
                throw new Exception("Invalid Tenant ID");
            if (string.IsNullOrEmpty(block.Container))
                throw new Exception("Invalid container");
            if (string.IsNullOrEmpty(block.FileName))
                throw new Exception("Invalid file name");
            if (block.Size < 0)
                throw new Exception("Invalid file size");

            byte[] tenantKey = null;
            using (var fm = new FileManager())
            {
                var tenant = fm.GetTenant(block.TenantID);
                tenantKey = tenant.Key.Decrypt(fm.MasterKey, fm.IV);
            }

            lock (_fileUploadCache)
            {
                var cache = new FilePartCache
                {
                    TenantID = block.TenantID,
                    Container = block.Container,
                    FileName = block.FileName,
                    CRC = block.CRC,
                    Size = block.Size,
                    Index = 0,
                    CreatedTime = block.CreatedTime,
                    ModifiedTime = block.ModifiedTime,
                    TenantKey = tenantKey,
                };

                //Add to part cache
                if (_fileUploadCache.Contains(cache.Key))
                    throw new Exception("File concurrency error");
                try
                {
                    using (var fm = new FileManager())
                    {
                        fm.RemoveFile(block.TenantID, block.Container, block.FileName);
                    }

                    _fileUploadCache.Add(cache.Key);
                    _fileUploadPartCache.Add(cache.ID, cache);

                    using (var fm = new FileManager())
                    {
                        var header = new FileHeader
                        {
                            DataKey = cache.OneTimeKey,
                            EncryptedDataKey = cache.OneTimeKey.Encrypt(cache.TenantKey, FileEngine.DefaultIVector),
                            TenantKey = cache.TenantKey,
                        };

                        //Create the data folder
                        var dataPath = Path.Combine(ConfigHelper.WorkFolder, cache.ID.ToString());
                        Directory.CreateDirectory(dataPath);
                        cache.TempDataFile = Path.Combine(dataPath, "out");
                        cache.EncryptStream = Extensions.OpenEncryptStream(cache.TempDataFile, fm.IV, header);
                    }

                    return new FileDataInfo
                    {
                        Token = cache.ID,
                        Size = cache.Size,
                        CreatedTime = block.CreatedTime,
                        ModifiedTime = block.ModifiedTime,
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Send a file part [1..N]
        /// </summary>
        public bool SendFileData(Guid token, byte[] data, int index)
        {
            if (data == null || data.Length == 0)
                throw new Exception("Invalid file data");

            try
            {
                //If not in cache then nothing to do
                if (!_fileUploadPartCache.ContainsKey(token))
                    throw new Exception("File upload error");

                //We know the file part if valid here
                var cache = _fileUploadPartCache[token];

                if (cache.Index != index)
                    throw new Exception("Invalid file index");

                //Write part
                cache.EncryptStream.Write(data, 0, data.Length);

                cache.Index++;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Called to end file upload and finialize the writting to storage
        /// </summary>
        public bool SendFileEnd(Guid token)
        {
            //If not in cache then nothing to do
            if (!_fileUploadPartCache.ContainsKey(token))
                throw new Exception("File upload error");

            try
            {
                //We know the file part if valid here
                var cache = _fileUploadPartCache[token];

                cache.EncryptStream.Close();

                //var outFile = Path.Combine(cache.DataFolder, "out");
                var crc = cache.CRC;
                if (crc == cache.CRC)
                {
                    //Write to file system
                    using (var fm = new FileManager())
                    {
                        var b = fm.SaveEncryptedFile(cache, cache.TempDataFile);
                    }
                }
                else
                {
                    Common.FileUtilities.WipeFile(cache.TempDataFile);
                    Directory.Delete(cache.TempDataFile, true);
                    throw new Exception("File upload failed due to CRC check");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
            finally
            {
                var cache = _fileUploadPartCache[token];
                _fileUploadCache.Remove(cache.Key);
                _fileUploadPartCache.Remove(token);
            }
        }

        /// <summary>
        /// Initialize a file for download
        /// </summary>
        /// <returns>Token that is used to get file chunks</returns>
        public FileDataInfo GetFileStart(Guid tenantId, string container, string fileName)
        {
            try
            {
                var retval = new FileDataInfo();
                using (var fm = new FileManager())
                {
                    var info = fm.GetFileInfo(tenantId, container, fileName);
                    if (info == null) return retval;

                    var token = Guid.NewGuid();
                    _fileDownloadCache.Add(token, new FilePartCache
                    {
                        DecryptStream = fm.GetFile(tenantId, container, fileName),
                        Size = info.Size,
                    });

                    retval.Token = token;
                    retval.Size = info.Size;
                    retval.CreatedTime = info.FileCreatedTime;
                    retval.ModifiedTime = info.FileModifiedTime;

                    return retval;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a file chunk based on the file token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <remarks>On the last call the data returned will be null and the token will become invalid</remarks>
        public byte[] GetFilePart(Guid token, int index)
        {
            if (!_fileDownloadCache.ContainsKey(token))
                throw new Exception("Invalid token");

            var cache = _fileDownloadCache[token];
            if (cache.DecryptStream == null)
                throw new Exception("Invalid token");

            try
            {
                const int blockSize = 1024 * 1024;
                var startIndex = index * blockSize;
                if (!cache.DecryptStream.CanRead)
                {
                    //EOF
                    _fileDownloadCache.Remove(token);
                    return null;
                }
                else
                {
                    var arr = new byte[blockSize];
                    var count = cache.DecryptStream.Read(arr, 0, arr.Length);
                    if (count < arr.Length)
                    {
                        arr = arr.Take(count).ToArray();
                        cache.DecryptStream.Close();
                    }
                    return arr;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a file from storage
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public int RemoveFile(Guid tenantId, string container, string fileName)
        {
            try
            {
                using (var fm = new FileManager())
                {
                    return fm.RemoveFile(tenantId, container, fileName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of existing files in storage for a tenant
        /// </summary>
        public List<string> GetFileList(Guid tenantID, string startPattern = null)
        {
            try
            {
                using (var fm = new FileManager())
                {
                    return fm.GetFileList(tenantID, startPattern);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public List<string> GetContainerList(Guid tenantID, string startPattern = null)
        {
            try
            {
                using (var fm = new FileManager())
                {
                    return fm.GetContainerList(tenantID, startPattern);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Removes all files for a tenant and container
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public int RemoveAll(Guid tenantID, string container)
        {
            try
            {
                using (var fm = new FileManager())
                {
                    return fm.RemoveAll(tenantID, container);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Resets a tenant key and resets all files for the tenant
        /// </summary>
        public int RekeyTenant(Guid tenantID)
        {
            try
            {
                using (var fm = new FileManager())
                {
                    return fm.RekeyTenant(tenantID);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        void IDisposable.Dispose()
        {
        }
    }

}
