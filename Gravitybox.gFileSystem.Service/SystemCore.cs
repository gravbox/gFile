using Gravitybox.gFileSystem.Engine;
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
        private static Dictionary<Guid, string> _fileDownloadCache = new Dictionary<Guid, string>();

        //TODO: get the maste key from somewhere else
        private static byte[] MasterKey16 => Encoding.UTF8.GetBytes("8s$w@r1%a-m>2pq9");

        public Guid GetOrAddTenant(string name)
        {
            //Create the manager object
            using (var fm = new FileManager(MasterKey16, GetConnectionString()))
            {
                return  fm.GetOrAddTenant(name);
            }
        }

        public Guid SendFileStart(FileInformation block)
        {
            if (block == null)
                throw new Exception("Block not set");
            if (block.TenantID == Guid.Empty)
                throw new Exception("Invalid TenantId");
            if (string.IsNullOrEmpty(block.Container))
                throw new Exception("Invalid container");
            if (string.IsNullOrEmpty(block.FileName))
                throw new Exception("Invalid filename");

            var cache = new FilePartCache
            {
                TenantID = block.TenantID,
                Container = block.Container,
                FileName = block.FileName,
                CRC = block.CRC,
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

                    //Create the data folder
                    cache.DataFolder = Path.Combine(ConfigHelper.WorkFolder, cache.ID.ToString());
                    Directory.CreateDirectory(cache.DataFolder);

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
        public bool SendFileData(Guid token, byte[] data)
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

                //Get temp folder to store parts
                var tempFile = Path.Combine(cache.DataFolder, cache.Index.ToString("0000000000"));

                //Write part
                using (var fs = File.OpenWrite(tempFile))
                {
                    fs.Write(data, 0, data.Length);
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

        public bool SendFileEnd(Guid token)
        {
            //If not in cache then nothing to do
            if (!_fileUploadPartCache.ContainsKey(token))
                throw new Exception("File upload error");

            try
            {
                bool retval = false;

                //We know the file part if valid here
                var cache = _fileUploadPartCache[token];

                //Get temp folder to store parts
                var partFolder = Path.Combine(ConfigHelper.WorkFolder, cache.ID.ToString());

                var outFile = Path.Combine(cache.DataFolder, "out");
                this.CombineAndWipe(cache.DataFolder, outFile);

                var crc = Common.FileUtilities.FileCRC(outFile);
                if (crc == cache.CRC)
                {
                    //If last part then write to file system
                    using (var fm = new FileManager(MasterKey16, GetConnectionString()))
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

        public Guid GetFileStart(Guid tenantId, string container, string fileName)
        {
            try
            {
                using (var fm = new FileManager(MasterKey16, GetConnectionString()))
                {
                    var tempfile = fm.GetFile(tenantId, container, fileName);
                    var token = Guid.NewGuid();
                    _fileDownloadCache.Add(token, tempfile);
                    return token;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public byte[] GetFile(Guid token, int index)
        {
            if (!_fileDownloadCache.ContainsKey(token))
                throw new Exception("Invalid token");

            var tempfile = _fileDownloadCache[token];
            if (!File.Exists(tempfile))
                throw new Exception("Invalid token");

            try
            {
                const int blockSize = 1024 * 1024;
                var startIndex = index * blockSize;
                var fi = new FileInfo(tempfile);
                if (startIndex >= fi.Length)
                {
                    //EOF - Remove file
                    File.Delete(tempfile);
                    return null;
                }
                else
                {
                    using (var fs = File.Open(tempfile, FileMode.Open, FileAccess.Read))
                    {
                        var arr = new byte[blockSize];
                        fs.Seek(startIndex, SeekOrigin.Begin);
                        var count = fs.Read(arr, 0, arr.Length);
                        if (count < arr.Length)
                            arr = arr.Take(count).ToArray();
                        return arr;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public int RemoveFile(Guid tenantId, string container, string fileName)
        {
            try
            {
                using (var fm = new FileManager(MasterKey16, GetConnectionString()))
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

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["gFileSystemEntities"].ConnectionString;
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
        public int Index { get; set; } = 1;
        public string DataFolder { get; set; }
        public string Key => (this.TenantID + "|" + this.Container + "|" + this.FileName).ToLower();
    }
}