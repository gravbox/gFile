using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravitybox.gFileSystem.EFDAL;
using Gravitybox.gFileSystem.EFDAL.Entity;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using Gravitybox.gFileSystem.Service.Common;
using System.Security.Cryptography;

namespace Gravitybox.gFileSystem.Service
{
    /// <summary>
    /// This manager add a concurrency layer on top of the FileEngine.
    /// It uses SQL Server to manage encryption keys.
    /// </summary>
    internal class FileManager : IDisposable
    {
        private readonly Guid GetOrAddTenantLockID = Guid.NewGuid();
        private string[] _skipZipExtensions = new string[] {
            ".zip", ".iso", ".tar", ".mp3", ".mp4", ".z", ".7z",
            ".s7z", ".apk", ".arc", ".cab", ".jar", ".lzh",
            ".pak",".png",".jpg",".jpeg",".wav",".rar"
        };

        public FileManager()
        {
            if (!Directory.Exists(ConfigHelper.StorageFolder))
                throw new Exception("The 'StorageFolder' does not exist.");
            if (!Directory.Exists(ConfigHelper.WorkFolder))
                throw new Exception("The 'WorkFolder' does not exist.");

           this.MasterKey = ConfigHelper.MasterKey;
            this.IV = FileEngine.DefaultIVector;
        }

        public byte[] MasterKey { get; private set; }
        public byte[] IV { get; private set; }

        /// <summary>
        /// Creates a new tenant or retieves an existing one.
        /// The tenant ID is returned
        /// </summary>
        public Guid GetOrAddTenant(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("The name must be specified.");

            try
            {
                using (var q = new WriterLock(GetOrAddTenantLockID, "GetOrAddTenant"))
                {
                    //Add/get a tenant in a transaction
                    var parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter { DbType = DbType.String, IsNullable = false, ParameterName = "@name", Value = name });
                    parameters.Add(new SqlParameter { DbType = DbType.Binary, IsNullable = false, ParameterName = "@key", Value = FileUtilities.GenerateKey().Encrypt(MasterKey, IV) });
                    var tenantID = (Guid)SqlHelper.ExecuteWithReturn(ConfigHelper.ConnectionString, "[AddOrUpdateTenant] @name, @key", parameters);

                    //Create the tenant storage folder
                    var tFolder = Path.Combine(ConfigHelper.StorageFolder, tenantID.ToString());
                    if (!Directory.Exists(tFolder))
                        Directory.CreateDirectory(tFolder);

                    return tenantID;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Given a tenant name, determines if the item exists
        /// </summary>
        public Guid TenantExists(string name)
        {
            try
            {
                using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                {
                    return context.Tenant.Where(x => x.Name == name).Select(x => x.UniqueKey).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Given a tenant ID, determines if the item exists
        /// </summary>
        public Guid TenantExists(Guid id)
        {
            try
            {
                using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                {
                    return context.Tenant.Where(x => x.UniqueKey == id).Select(x => x.UniqueKey).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Get a list of all files for the specified tenant.
        /// The start pattern can be used to narrow the search based on the path
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="startPattern"></param>
        /// <returns></returns>
        public List<string> GetFileList(Guid tenantID, string startPattern = null)
        {
            try
            {
                using (var q = new ReaderLock(tenantID, ""))
                {
                    if (string.IsNullOrEmpty(startPattern)) startPattern = null;
                    else startPattern = startPattern.Replace("*", "");
                    var tenant = GetTenant(tenantID);
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        return context.FileStash
                            .Where(x => x.TenantID == tenant.TenantID &&
                            (startPattern == null || x.Path.StartsWith(startPattern)))
                            .Select(x => x.Path)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Get a list of all containers for the specified tenant.
        /// The start pattern can be used to narrow the search based on start of the name
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="startPattern"></param>
        /// <returns></returns>
        public List<string> GetContainerList(Guid tenantID, string startPattern = null)
        {
            try
            {
                using (var q = new ReaderLock(tenantID, ""))
                {
                    if (string.IsNullOrEmpty(startPattern)) startPattern = null;
                    else startPattern = startPattern.Replace("*", string.Empty);
                    var tenant = GetTenant(tenantID);
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        return context.Container
                            .Where(x => x.TenantId == tenant.TenantID &&
                            (startPattern == null || x.Name.StartsWith(startPattern)))
                            .Select(x => x.Name)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Given a valid tenant, this will change the tenant key and ensure all files
        /// associated to that tenant are been re-keyed. This routine can be used if the
        /// tenant key has been compromised.
        /// </summary>
        public int RekeyTenant(Guid tenantID)
        {
            var tenant = GetTenant(tenantID);
            try
            {
                using (var q = new WriterLock(tenantID, ""))
                {
                    var count = 0;

                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(MasterKey, IV);
                    var newKey = FileUtilities.GenerateKey();
                    using (var engine = new FileEngine(MasterKey, tenantKey, tenantID, IV))
                    {
                        engine.WorkingFolder = ConfigHelper.WorkFolder;

                        using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                        {
                            var all = context.FileStash
                                .Include(x => x.Container)
                                .Where(x => x.TenantID == tenant.TenantID)
                                .ToList();

                            //Loop through all files for this tenant and re-encrypt the data key for each file
                            //There is nothing to change in the database
                            foreach (var stash in all)
                            {
                                var existingFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                                if (File.Exists(existingFile))
                                {
                                    if (engine.RekeyFile(existingFile, newKey))
                                        count++;
                                }
                            }

                            //Save the new tenant key
                            tenant = context.Tenant.FirstOrDefault(x => x.UniqueKey == tenantID);
                            tenant.Key = newKey.Encrypt(MasterKey, IV);
                            context.SaveChanges();
                        }
                    }
                    return count;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public bool SaveEncryptedFile(FilePartCache cache, string outFile)
        {
            if (string.IsNullOrEmpty(cache.Container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(cache.TenantID);
            try
            {
                using (var q = new WriterLock(cache.TenantID, cache.Container + "|" + cache.FileName))
                {
                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(MasterKey, IV);
                    using (var engine = new FileEngine(MasterKey, tenantKey, cache.TenantID, IV))
                    {
                        using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                        {
                            this.RemoveFile(tenant.UniqueKey, cache.Container, cache.FileName);
                            var containerItem = GetContainer(tenant, cache.Container);

                            var fiCipher = new FileInfo(outFile);
                            var stash = new FileStash
                            {
                                Path = cache.FileName,
                                TenantID = tenant.TenantID,
                                Size = cache.Size,
                                StorageSize = fiCipher.Length,
                                ContainerId = containerItem.ContainerId,
                                CrcPlain = cache.CRC,
                                IsCompressed = false,
                                FileCreatedTime = cache.CreatedTime,
                                FileModifiedTime = cache.ModifiedTime,
                                UniqueKey = cache.ID,
                            };
                            context.AddItem(stash);
                            context.SaveChanges();

                            //Move the cipher file to storage
                            var destFile = GetFilePath(tenant.UniqueKey, containerItem.UniqueKey, stash);
                            File.Move(outFile, destFile);
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public FileStash GetFileInfo(Guid tenantID, string container, string fileName)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            try
            {
                using (var q = new ReaderLock(tenantID, container + "|" + fileName))
                {
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        var fi = new FileInfo(fileName);
                        return context.FileStash
                            .FirstOrDefault(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.Container.Name == container);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a file from storage for a tenant in the specified container
        /// using the filenme as the lookup key
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public System.IO.Stream GetFile(Guid tenantID, string container, string fileName)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            try
            {
                using (var q = new ReaderLock(tenantID, container + "|" + fileName))
                {
                    FileStash stash = null;
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        var fi = new FileInfo(fileName);
                        stash = context.FileStash
                            .Include(x => x.Container)
                            .FirstOrDefault(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.Container.Name == container);

                        //There is no file so return null
                        if (stash == null) return null;
                    }

                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(MasterKey, IV);
                    string cipherFile = null;
                    using (var engine = new FileEngine(MasterKey, tenantKey, tenantID, IV))
                    {
                        engine.WorkingFolder = ConfigHelper.WorkFolder;
                        cipherFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                        return engine.GetFileStream(cipherFile);
                    }

                    //if (stash.IsCompressed)
                    //{
                    //    var unzipFile = plainText + ".uz";
                    //    if (FileUtilities.UnzipFile(plainText, unzipFile))
                    //    {
                    //        FileUtilities.WipeFile(plainText);
                    //        File.Move(unzipFile, plainText);
                    //    }
                    //}

                    //var crc = FileUtilities.FileCRC(plainText);

                    ////Verify that the file is the same as when it was saved
                    //if (crc != stash.CrcPlain)
                    //    throw new Exception("The file is corrupted!");

                    //return plainText;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a file from storeage for a tenant in the specified container
        /// using the filenme as the lookup key
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public int RemoveFile(Guid tenantID, string container, string fileName)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            try
            {
                using (var q = new WriterLock(tenantID, ""))
                {
                    var count = 0;
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        var all = context.FileStash
                            .Include(x => x.Container)
                            .Where(x => x.TenantID == tenant.TenantID &&
                                x.Container.Name == container &&
                                x.Path == fileName)
                            .ToList();

                        foreach (var stash in all)
                        {
                            var existingFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(stash);
                            count++;
                        }
                        context.SaveChanges();
                    }
                    return count;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all files in for the specified tenant in the specified container
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public int RemoveAll(Guid tenantID, string container)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            try
            {
                using (var q = new WriterLock(tenantID, ""))
                {
                    var count = 0;
                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        var all = context.FileStash
                            .Include(x => x.Container)
                            .Where(x => x.TenantID == tenant.TenantID &&
                                x.Container.Name == container)
                            .ToList();

                        foreach (var stash in all)
                        {
                            var existingFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(stash);
                            count++;
                        }
                        context.SaveChanges();
                    }
                    return count;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        internal Tenant GetTenant(Guid tenantId)
        {
            using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
            {
                return context.Tenant.Single(x => x.UniqueKey == tenantId);
            }
        }

        private Container GetContainer(Tenant tenant, string name)
        {
            try
            {
                using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                {
                    var retval = context.Container
                        .Where(x => x.TenantId == tenant.TenantID && x.Name == name)
                        .FirstOrDefault();

                    var containerItem = context.Container
                        .Where(x => x.TenantId == tenant.TenantID && x.Name == name)
                        .FirstOrDefault();

                    if (containerItem == null)
                    {
                        containerItem = new Container { TenantId = tenant.TenantID, Name = name };
                        context.AddItem(containerItem);
                        context.SaveChanges();
                        Directory.CreateDirectory(Path.Combine(ConfigHelper.StorageFolder, tenant.UniqueKey.ToString(), containerItem.UniqueKey.ToString()));
                    }

                    return containerItem;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GetFilePath(Guid tenantID, Guid containerID, FileStash stash)
        {
            try
            {
                return Path.Combine(ConfigHelper.StorageFolder,
                       tenantID.ToString(),
                       containerID.ToString(),
                       stash.UniqueKey.ToString());
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        void IDisposable.Dispose()
        {
        }
    }
}