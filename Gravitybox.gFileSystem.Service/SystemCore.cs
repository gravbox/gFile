using Gravitybox.gFileSystem.Manager;
using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace Gravitybox.gFileSystem.Service
{
    [Serializable()]
    [KnownType(typeof(FileInfo))]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class SystemCore : MarshalByRefObject, ISystemCore, IDisposable
    {
        //TODO: handle cache somewhere else
        private static Dictionary<Guid, FilePartCache> _fileUploadPartCache = new Dictionary<Guid, FilePartCache>();
        private static HashSet<string> _fileUploadCache = new HashSet<string>();
        private static Dictionary<Guid, FilePartCache> _fileDownloadCache = new Dictionary<Guid, FilePartCache>();

        public Guid GetOrAddTenant(byte[] _masterKey, string name)
        {
            //Create the manager object
            using (var fm = new FileManager(_masterKey))
            {
                return fm.GetOrAddTenant(name);
            }
        }

        /// <summary>
        /// Initialize a file for upload
        /// </summary>
        /// <returns>Token that is used to append data in chunk</returns>
        public Guid SendFileStart(FileInformation block)
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

            var cache = new FilePartCache
            {
                TenantID = block.TenantID,
                Container = block.Container,
                FileName = block.FileName,
                CRC = block.CRC,
                Size = block.Size,
                Index = 1,
            };

            lock (_fileUploadCache)
            {
                //Add to part cache
                if (_fileUploadCache.Contains(cache.Key))
                    throw new Exception("File concurrency error");
                try
                {
                    _fileUploadCache.Add(cache.Key);
                    _fileUploadPartCache.Add(cache.ID, cache);

                    if (block.Size <= ConfigHelper.MaxMemoryFileSize)
                    {
                        //Mark this to reside in memory and never touch disk
                        cache.InMem = true;
                        cache.Data = new byte[0];
                    }
                    else
                    {
                        //Create the data folder
                        cache.DataFolder = Path.Combine(ConfigHelper.WorkFolder, cache.ID.ToString());
                        Directory.CreateDirectory(cache.DataFolder);
                    }
                    return cache.ID;
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

                if (cache.InMem)
                {
                    cache.Data = CombineArrays(cache.Data, data);
                }
                else
                {
                    //Get temp folder to store parts
                    var tempFile = Path.Combine(cache.DataFolder, cache.Index.ToString("0000000000"));

                    //Write part
                    using (var fs = File.OpenWrite(tempFile))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }

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
        public bool SendFileEnd(byte[] _masterKey, Guid token)
        {
            //If not in cache then nothing to do
            if (!_fileUploadPartCache.ContainsKey(token))
                throw new Exception("File upload error");

            try
            {
                bool retval = false;

                //We know the file part if valid here
                var cache = _fileUploadPartCache[token];

                if (cache.InMem)
                {
                    var crc = Common.FileUtilities.FileCRC(cache.Data);
                    if (crc == cache.CRC)
                    {
                        //If last part then write to file system
                        using (var fm = new FileManager(_masterKey))
                        {
                            retval = fm.SaveFile(cache.TenantID, cache.Container, cache.FileName, cache.Data);
                        }
                    }
                    else
                    {
                        throw new Exception("File upload failed due to CRC check");
                    }
                }
                else
                {
                    //Get temp folder to store parts
                    var partFolder = Path.Combine(ConfigHelper.WorkFolder, cache.ID.ToString());

                    var outFile = Path.Combine(cache.DataFolder, "out");
                    this.CombineAndWipe(cache.DataFolder, outFile);

                    var crc = Common.FileUtilities.FileCRC(outFile);
                    if (crc == cache.CRC)
                    {
                        //Write to file system
                        using (var fm = new FileManager(_masterKey))
                        {
                            retval = fm.SaveFile(cache.TenantID, cache.Container, cache.FileName, outFile);
                        }
                        Common.FileUtilities.WipeFile(outFile);
                        Directory.Delete(cache.DataFolder, true);
                    }
                    else
                    {
                        Common.FileUtilities.WipeFile(outFile);
                        Directory.Delete(cache.DataFolder, true);
                        throw new Exception("File upload failed due to CRC check");
                    }
                }

                return retval;
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
        public Guid GetFileStart(byte[] _masterKey, Guid tenantId, string container, string fileName)
        {
            try
            {
                using (var fm = new FileManager(_masterKey))
                {
                    var info = fm.GetFileInfo(tenantId, container, fileName);
                    if (info == null) return Guid.Empty;

                    var token = Guid.NewGuid();
                    if (info.Size <= ConfigHelper.MaxMemoryFileSize)
                    {
                        var data = fm.GetFileData(tenantId, container, fileName);
                        _fileDownloadCache.Add(token, new FilePartCache { Data = data, InMem = true });
                    }
                    else
                    {
                        var tempfile = fm.GetFile(tenantId, container, fileName);
                        _fileDownloadCache.Add(token, new FilePartCache { FileName = tempfile });
                    }
                    return token;
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
            if (cache.InMem)
            {
                if (cache.Data == null)
                    throw new Exception("Invalid token");
            }
            else
            {
                if (!File.Exists(cache.FileName))
                    throw new Exception("Invalid token");
            }

            try
            {
                const int blockSize = 1024 * 1024;
                var startIndex = index * blockSize;
                if (cache.InMem)
                {
                    #region Memory
                    if (startIndex >= cache.Data.Length)
                    {
                        _fileDownloadCache.Remove(token);
                        return null;
                    }
                    else
                    {
                        using (var fs = new MemoryStream(cache.Data))
                        {
                            var arr = new byte[blockSize];
                            fs.Seek(startIndex, SeekOrigin.Begin);
                            var count = fs.Read(arr, 0, arr.Length);
                            if (count < arr.Length)
                                arr = arr.Take(count).ToArray();
                            return arr;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region File System
                    var fi = new FileInfo(cache.FileName);
                    if (startIndex >= fi.Length)
                    {
                        //EOF - Remove file
                        File.Delete(cache.FileName);
                        _fileDownloadCache.Remove(token);
                        return null;
                    }
                    else
                    {
                        using (var fs = File.Open(cache.FileName, FileMode.Open, FileAccess.Read))
                        {
                            var arr = new byte[blockSize];
                            fs.Seek(startIndex, SeekOrigin.Begin);
                            var count = fs.Read(arr, 0, arr.Length);
                            if (count < arr.Length)
                                arr = arr.Take(count).ToArray();
                            return arr;
                        }
                    }
                    #endregion
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
        public int RemoveFile(byte[] _masterKey, Guid tenantId, string container, string fileName)
        {
            try
            {
                using (var fm = new FileManager(_masterKey))
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
        public List<string> GetFileList(byte[] _masterKey, Guid tenantID, string startPattern = null)
        {
            try
            {
                using (var fm = new FileManager(_masterKey))
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

        /// <summary>
        /// Removes all files for a tenant and container
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public int RemoveAll(byte[] _masterKey, Guid tenantID, string container)
        {
            try
            {
                using (var fm = new FileManager(_masterKey))
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
        /// Resets a tenant key and reset all files for the tenant
        /// </summary>
        public int RekeyTenant(byte[] _masterKey, Guid tenantID)
        {
            try
            {
                using (var fm = new FileManager(_masterKey))
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

        private void CombineAndWipe(string folder, string outFile)
        {
            try
            {
                var inputFilePaths = Directory.GetFiles(folder)
                            .OrderBy(x => x)
                            .ToList();

                using (var outputStream = File.Create(outFile))
                {
                    foreach (var inputFilePath in inputFilePaths)
                    {
                        using (var inputStream = File.OpenRead(inputFilePath))
                        {
                            inputStream.CopyTo(outputStream);
                        }
                        Common.FileUtilities.WipeFile(inputFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public static byte[] CombineArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }


        void IDisposable.Dispose()
        {
        }

    }

    internal class FilePartCache
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid TenantID { get; set; }
        public string Container { get; set; }
        public string FileName { get; set; }
        public string CRC { get; set; }
        public long Size { get; set; }
        public int Index { get; set; } = 1;
        public string DataFolder { get; set; }
        public string Key => (this.TenantID + "|" + this.Container + "|" + this.FileName).ToLower();
        public bool InMem { get; set; }
        public byte[] Data { get; set; }
    }
}