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
        public FileDataInfo SendFileStart(byte[] masterKey, FileInformation block)
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
            using (var fm = new FileManager(masterKey))
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
                    using (var fm = new FileManager(masterKey))
                    {
                        fm.RemoveFile(block.TenantID, block.Container, block.FileName);
                    }

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
                        using (var fm = new FileManager(masterKey))
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

                if (cache.InMem)
                {
                    cache.Data = ConcatArrays(cache.Data, data);
                }
                else
                {
                    //Write part
                    cache.EncryptStream.Write(data, 0, data.Length);
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
                //We know the file part if valid here
                var cache = _fileUploadPartCache[token];

                //Do all the post processing like disk copy and DB update async
                //Task.Factory.StartNew(() =>
                //{
                    SendFileEndPostProcess(_masterKey, cache);
                //});

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
        /// Handles the actual copying of the temp file to real storage
        /// </summary>
        /// <param name="_masterKey"></param>
        /// <param name="cache"></param>
        private void SendFileEndPostProcess(byte[] _masterKey, FilePartCache cache)
        {
            bool retval;
            if (cache.InMem)
            {
                #region InMem processing
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
                #endregion
            }
            else
            {
                #region Disk Processing
                cache.EncryptStream.Close();

                //var outFile = Path.Combine(cache.DataFolder, "out");
                var crc = cache.CRC;
                if (crc == cache.CRC)
                {
                    //Write to file system
                    using (var fm = new FileManager(_masterKey))
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
                #endregion
            }
        }

        /// <summary>
        /// Initialize a file for download
        /// </summary>
        /// <returns>Token that is used to get file chunks</returns>
        public FileDataInfo GetFileStart(byte[] _masterKey, Guid tenantId, string container, string fileName)
        {
            try
            {
                var retval = new FileDataInfo();
                using (var fm = new FileManager(_masterKey))
                {
                    var info = fm.GetFileInfo(tenantId, container, fileName);
                    if (info == null) return retval;

                    var token = Guid.NewGuid();
                    if (info.Size <= ConfigHelper.MaxMemoryFileSize)
                    {
                        var data = fm.GetFileData(tenantId, container, fileName);
                        _fileDownloadCache.Add(token, new FilePartCache
                        {
                            Data = data,
                            InMem = true,
                            Size = info.Size,
                        });
                    }
                    else
                    {
                        _fileDownloadCache.Add(token, new FilePartCache
                        {
                            DecryptStream = fm.GetFile(tenantId, container, fileName),
                            Size = info.Size,
                        });
                    }

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
            if (cache.InMem)
            {
                if (cache.Data == null)
                    throw new Exception("Invalid token");
            }
            else
            {
                //if (!File.Exists(cache.FileName))
                //    throw new Exception("Invalid token");
                if (cache.DecryptStream == null)
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
        /// Resets a tenant key and resets all files for the tenant
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

        /// <summary>
        /// Combine many temp files into one complete file
        /// </summary>
        private void CombineAndWipe(string folder, string outFile, FilePartCache cache, FileManager manager)
        {
            try
            {
                var inputFilePaths = Directory.GetFiles(folder)
                            .OrderBy(x => x)
                            .ToList();

                var tenant = manager.GetTenant(cache.TenantID);
                var tenantKey = tenant.Key.Decrypt(manager.MasterKey, manager.IV);

                var aes = Extensions.CryptoProvider(cache.OneTimeKey, FileEngine.DefaultIVector);
                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var outputStream = File.Create(outFile))
                    {
                        var header = new FileHeader
                        {
                            DataKey = cache.OneTimeKey,
                            EncryptedDataKey = cache.OneTimeKey.Encrypt(tenantKey, FileEngine.DefaultIVector),
                            TenantKey = tenantKey,
                        };

                        //Write blank header. It will be filled in later
                        var padBytes = header.ToArray();
                        outputStream.Write(padBytes, 0, padBytes.Length);

                        using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                        {
                            //Take the encrypted file parts and combine into an encrypted OUT file
                            foreach (var inputFilePath in inputFilePaths)
                            {
                                using (var inputStream = File.OpenRead(inputFilePath))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        //The OUT file is encrypted
                                        inputStream.DecryptStream(ms, FileEngine.DefaultIVector, header);
                                        ms.Seek(0, SeekOrigin.Begin);
                                        ms.CopyTo(cryptoStream);
                                    }
                                }
                                FileUtilities.WipeFile(inputFilePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public static byte[] ConcatArrays(byte[] first, byte[] second)
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

}
