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

namespace Gravitybox.gFileSystem.Service
{
    /// <summary>
    /// This manager add a concurrency layer on top of the FileEngine.
    /// It uses SQL Server to manage encryption keys.
    /// </summary>
    public class FileManager : IDisposable
    {
        private byte[] _masterKey = null;
        private byte[] _iv = null;
        private readonly Guid GetOrAddTenantLockID = Guid.NewGuid();
        private string[] _skipZipExtensions = new string[] {
            ".zip", ".iso", ".tar", ".mp3", ".mp4", ".z", ".7z",
            ".s7z", ".apk", ".arc", ".cab", ".jar", ".lzh",
            ".pak",".png",".jpg",".jpeg",".wav",".rar"
        };

        public FileManager(byte[] masterKey)
            : this(masterKey, FileEngine.DefaultIVector)
        {
        }

        public FileManager(byte[] masterKey, byte[] iv)
        {
            if (!Directory.Exists(ConfigHelper.StorageFolder))
                throw new Exception("The 'StorageFolder' does not exist.");
            if (!Directory.Exists(ConfigHelper.WorkFolder))
                throw new Exception("The 'WorkFolder' does not exist.");

            _masterKey = masterKey;
            _iv = iv;
        }

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
                    parameters.Add(new SqlParameter { DbType = DbType.Binary, IsNullable = false, ParameterName = "@key", Value = FileUtilities.GetNewKey().Encrypt(_masterKey, _iv) });
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
            if (tenant == null)
                return 0;

            try
            {
                using (var q = new WriterLock(tenantID, ""))
                {
                    var count = 0;

                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var newKey = FileUtilities.GetNewKey();
                    using (var fe = new FileEngine(_masterKey, tenantKey, tenantID, _iv))
                    {
                        fe.WorkingFolder = ConfigHelper.WorkFolder;

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
                                    if (fe.RekeyFile(existingFile, newKey))
                                        count++;
                                }
                            }

                            //Save the new tenant key
                            tenant = context.Tenant.FirstOrDefault(x => x.UniqueKey == tenantID);
                            tenant.Key = newKey.Encrypt(_masterKey, _iv);
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

        public bool SaveFile(Guid tenantID, string container, string fileName, byte[] data)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            if (tenant == null)
                return false;

            try
            {
                using (var q = new WriterLock(tenantID, container + "|" + fileName))
                {
                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, tenantID, _iv);

                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        //Delete the old file if one exists
                        var stash = context.FileStash
                            .Include(x => x.Container)
                            .Where(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.Container.Name == container)
                                .FirstOrDefault();
                        if (stash != null)
                        {
                            var existingFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(stash);
                            context.SaveChanges();
                        }

                        var crc = FileUtilities.FileCRC(data);

                        //Determine if should compress
                        var isCompressed = false;
                        var fi = new FileInfo(fileName);
                        var origSize = data.Length;
                        if (!_skipZipExtensions.Contains(fi.Extension.ToLower()) && data.Length > 5000)
                        {
                            data = FileUtilities.ZipArray(data);
                            isCompressed = true;
                        }

                        //Do the actual encryption
                        var cipherFile = fe.SaveFile(data);
                        var fiCipher = new FileInfo(cipherFile);

                        var containerItem = context.Container
                            .Where(x => x.TenantId == tenant.TenantID && x.Name == container)
                            .FirstOrDefault();

                        if (containerItem == null)
                        {
                            containerItem = new Container { TenantId = tenant.TenantID, Name = container };
                            context.AddItem(containerItem);
                            context.SaveChanges();
                            Directory.CreateDirectory(Path.Combine(ConfigHelper.StorageFolder, tenantID.ToString(), containerItem.UniqueKey.ToString()));
                        }

                        stash = new FileStash
                        {
                            Path = fileName,
                            TenantID = tenant.TenantID,
                            Size = origSize,
                            StorageSize = fiCipher.Length,
                            ContainerId = containerItem.ContainerId,
                            CrcPlain = crc,
                            IsCompressed = isCompressed,
                        };
                        context.AddItem(stash);
                        context.SaveChanges();

                        //Move the cipher file to storage
                        var destFile = GetFilePath(tenant.UniqueKey, containerItem.UniqueKey, stash);
                        File.Move(cipherFile, destFile);
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

        /// <summary>
        /// Creates a file in storage for a specific tenant in the specified container
        /// The filename is used as the lookup key. It is unique.
        /// If a file for the filename key exists, it is overwritten.
        /// </summary>
        /// <param name="tenantID">The tenant account under which to store this file</param>
        /// <param name="container">The container name under which to store this file</param>
        /// <param name="fileName">The file name to store</param>
        /// <param name="dataFile">If the data is in a different file then specify it here</param>
        /// <returns></returns>
        public bool SaveFile(Guid tenantID, string container, string fileName, string dataFile = null)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            if (tenant == null)
                return false;

            try
            {
                using (var q = new WriterLock(tenantID, container + "|" + fileName))
                {
                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, tenantID, _iv);
                    fe.WorkingFolder = ConfigHelper.WorkFolder;

                    using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                    {
                        //Delete the old file if one exists
                        var stash = context.FileStash
                            .Include(x => x.Container)
                            .Where(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.Container.Name == container)
                                .FirstOrDefault();
                        if (stash != null)
                        {
                            var existingFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(stash);
                            context.SaveChanges();
                        }

                        //Encrypt/save file
                        if (string.IsNullOrEmpty(dataFile))
                            dataFile = fileName;

                        var crc = FileUtilities.FileCRC(dataFile);

                        //Determine if should compress
                        var isCompressed = false;
                        var fi = new FileInfo(fileName);
                        if (!_skipZipExtensions.Contains(fi.Extension.ToLower()) && fi.Length > 5000)
                        {
                            var zipFile = dataFile + ".z";
                            if (FileUtilities.ZipFile(dataFile, zipFile))
                            {
                                FileUtilities.WipeFile(dataFile);
                                dataFile = zipFile;
                                isCompressed = true;
                            }
                        }

                        //Do the actual encryption
                        var cipherFile = fe.SaveFile(dataFile);
                        var fiCipher = new FileInfo(cipherFile);

                        var containerItem = context.Container
                            .Where(x => x.TenantId == tenant.TenantID && x.Name == container)
                            .FirstOrDefault();

                        if (containerItem == null)
                        {
                            containerItem = new Container { TenantId = tenant.TenantID, Name = container };
                            context.AddItem(containerItem);
                            context.SaveChanges();
                            Directory.CreateDirectory(Path.Combine(ConfigHelper.StorageFolder, tenantID.ToString(), containerItem.UniqueKey.ToString()));
                        }

                        stash = new FileStash
                        {
                            Path = fileName,
                            TenantID = tenant.TenantID,
                            Size = fi.Length,
                            StorageSize = fiCipher.Length,
                            ContainerId = containerItem.ContainerId,
                            CrcPlain = crc,
                            IsCompressed = isCompressed,
                        };
                        context.AddItem(stash);
                        context.SaveChanges();

                        //Move the cipher file to storage
                        var destFile = GetFilePath(tenant.UniqueKey, containerItem.UniqueKey, stash);
                        File.Move(cipherFile, destFile);
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
            if (tenant == null)
                return null;

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
        public string GetFile(Guid tenantID, string container, string fileName)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            if (tenant == null)
                return null;

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
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, tenantID, _iv);
                    fe.WorkingFolder = ConfigHelper.WorkFolder;

                    var cipherFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                    var plainText = fe.GetFile(cipherFile);

                    if (stash.IsCompressed)
                    {
                        var unzipFile = plainText + ".uz";
                        if (FileUtilities.UnzipFile(plainText, unzipFile))
                        {
                            FileUtilities.WipeFile(plainText);
                            File.Move(unzipFile, plainText);
                        }
                    }

                    var crc = FileUtilities.FileCRC(plainText);

                    //Verify that the file is the same as when it was saved
                    if (crc != stash.CrcPlain)
                        throw new Exception("The file is corrupted!");

                    return plainText;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public byte[] GetFileData(Guid tenantID, string container, string fileName)
        {
            if (string.IsNullOrEmpty(container))
                throw new Exception("The container must be set");

            var tenant = GetTenant(tenantID);
            if (tenant == null)
                return null;

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
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, tenantID, _iv);
                    fe.WorkingFolder = ConfigHelper.WorkFolder;

                    var cipherFile = GetFilePath(tenant.UniqueKey, stash.Container.UniqueKey, stash);
                    var data = fe.GetFileData(cipherFile);

                    if (stash.IsCompressed)
                    {
                        data = FileUtilities.UnzipArray(data);
                    }

                    var crc = FileUtilities.FileCRC(data);

                    //Verify that the file is the same as when it was saved
                    if (crc != stash.CrcPlain)
                        throw new Exception("The file is corrupted!");

                    return data;
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
            if (tenant == null)
                return 0;

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
            if (tenant == null)
                return 0;

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

        private Tenant GetTenant(Guid id)
        {
            try
            {
                using (var context = new gFileSystemEntities(ConfigHelper.ConnectionString))
                {
                    return context.Tenant.Where(x => x.UniqueKey == id).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
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