using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravitybox.gFileSystem.EFDAL;
using Gravitybox.gFileSystem.EFDAL.Entity;
using System.IO;
using Gravitybox.gFileSystem.Engine;
using System.Data.SqlClient;
using System.Data;
using Gravitybox.gFileSystem.Service.Common;

namespace Gravitybox.gFileSystem.Manager
{
    /// <summary>
    /// This manager add a concurrency layer on top of the FileEngine
    /// If uses SQL Server to manage encrption keys as well as manage concurrency across multiple machines
    /// </summary>
    public class FileManager : IFileManager, IDisposable
    {
        private string _connectionString = null;
        private byte[] _masterKey = null;
        private byte[] _iv = null;

        public FileManager(byte[] masterKey, string connectionString)
            : this(masterKey, FileEngine.GetDefaultIV(masterKey.Length), connectionString)
        {
        }

        public FileManager(byte[] masterKey, byte[] iv, string connectionString)
        {
            ConfigHelper.ConnectionString = connectionString;
            if (!Directory.Exists(ConfigHelper.StorageFolder))
                throw new Exception("The 'StorageFolder' does not exist.");
            if (!Directory.Exists(ConfigHelper.WorkFolder))
                throw new Exception("The 'WorkFolder' does not exist.");

            _connectionString = connectionString;
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
                using (var q = new WriterLock(Guid.Empty, "GetOrAddTenant", _connectionString))
                {
                    //Add/get a tenant in a transaction
                    var parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter { DbType = DbType.String, IsNullable = false, ParameterName = "@name", Value = name });
                    parameters.Add(new SqlParameter { DbType = DbType.Binary, IsNullable = false, ParameterName = "@key", Value = FileUtilities.GetNewKey(_masterKey.Length).Encrypt(_masterKey, _iv) });
                    var retval = (Guid)SqlHelper.ExecuteWithReturn(_connectionString, "[AddOrUpdateTenant] @name, @key", parameters);
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
        /// Given a tenant name, determines if the item exists
        /// </summary>
        public Guid TenantExists(string name)
        {
            try
            {
                using (var context = new gFileSystemEntities(_connectionString))
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
                using (var context = new gFileSystemEntities(_connectionString))
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
                using (var q = new ReaderLock(tenantID, "", _connectionString))
                {
                    if (string.IsNullOrEmpty(startPattern)) startPattern = null;
                    else startPattern = startPattern.Replace("*", "");
                    var tenant = GetTenant(tenantID);
                    using (var context = new gFileSystemEntities(_connectionString))
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
                using (var q = new ReaderLock(tenantID, "", _connectionString))
                {
                    if (string.IsNullOrEmpty(startPattern)) startPattern = null;
                    else startPattern = startPattern.Replace("*", "");
                    var tenant = GetTenant(tenantID);
                    using (var context = new gFileSystemEntities(_connectionString))
                    {
                        return context.FileStash
                            .Where(x => x.TenantID == tenant.TenantID &&
                            (startPattern == null || x.Path.StartsWith(startPattern)))
                            .GroupBy(x => x.ContainerName)
                            .Select(x => x.Key)
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
                using (var q = new WriterLock(tenantID, "", _connectionString))
                {
                    var count = 0;

                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var newKey = FileUtilities.GetNewKey(_masterKey.Length);
                    using (var fe = new FileEngine(_masterKey, tenantKey, _iv))
                    {
                        fe.WorkingFolder = ConfigHelper.WorkFolder;

                        using (var context = new gFileSystemEntities(_connectionString))
                        {
                            var all = context.FileStash
                                .Where(x => x.TenantID == tenant.TenantID)
                                .ToList();

                            //Loop through all files for this tenant and re-encrypt the data key fore each file
                            //There is nothing to change in the database
                            foreach (var stash in all)
                            {
                                var existingFile = Path.Combine(ConfigHelper.StorageFolder, stash.UniqueKey.ToString());
                                if (File.Exists(existingFile))
                                {
                                    fe.RekeyFile(existingFile, newKey);
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
                using (var q = new WriterLock(tenantID, container + "|" + fileName, _connectionString))
                {
                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, _iv);
                    fe.WorkingFolder = ConfigHelper.WorkFolder;

                    using (var context = new gFileSystemEntities(_connectionString))
                    {
                        //Delete the old file if one exists
                        var stash = context.FileStash.Where(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.ContainerName == container)
                                .FirstOrDefault();
                        if (stash != null)
                        {
                            var existingFile = Path.Combine(ConfigHelper.StorageFolder, stash.UniqueKey.ToString());
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(stash);
                            context.SaveChanges();
                        }

                        //Encrypt/save file
                        if (string.IsNullOrEmpty(dataFile))
                            dataFile = fileName;
                        var cipherFile = fe.SaveFile(dataFile);

                        var fi = new FileInfo(fileName);
                        var newStash = new FileStash
                        {
                            Path = fileName,
                            TenantID = tenant.TenantID,
                            Size = fi.Length,
                            ContainerName = container,
                            CrcPlain = FileUtilities.FileCRC(dataFile),
                        };
                        context.AddItem(newStash);
                        context.SaveChanges();

                        //Move the cipher file to storage
                        var destFile = Path.Combine(ConfigHelper.StorageFolder, newStash.UniqueKey.ToString());
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
                using (var q = new ReaderLock(tenantID, container + "|" + fileName, _connectionString))
                {
                    FileStash stash = null;
                    using (var context = new gFileSystemEntities(_connectionString))
                    {
                        var fi = new FileInfo(fileName);
                        stash = context.FileStash
                            .FirstOrDefault(x =>
                                x.TenantID == tenant.TenantID &&
                                x.Path == fileName &&
                                x.ContainerName == container);

                        //There is no file so return null
                        if (stash == null) return null;
                    }

                    //Create engine
                    var tenantKey = tenant.Key.Decrypt(_masterKey, _iv);
                    var fe = new FileEngine(_masterKey, tenantKey, _iv);
                    fe.WorkingFolder = ConfigHelper.WorkFolder;

                    var cipherFile = Path.Combine(ConfigHelper.StorageFolder, stash.UniqueKey.ToString());
                    var plainText = fe.GetFile(cipherFile);
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
                using (var q = new WriterLock(tenantID, "", _connectionString))
                {
                    var count = 0;
                    using (var context = new gFileSystemEntities(_connectionString))
                    {
                        var all = context.FileStash
                            .Where(x => x.TenantID == tenant.TenantID &&
                                x.ContainerName == container &&
                                x.Path == fileName)
                            .ToList();

                        foreach (var item in all)
                        {
                            var existingFile = Path.Combine(ConfigHelper.StorageFolder, item.UniqueKey.ToString());
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(item);
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
                using (var q = new WriterLock(tenantID, "", _connectionString))
                {
                    var count = 0;
                    using (var context = new gFileSystemEntities(_connectionString))
                    {
                        var all = context.FileStash
                            .Where(x => x.TenantID == tenant.TenantID &&
                                x.ContainerName == container)
                            .ToList();

                        foreach (var item in all)
                        {
                            var existingFile = Path.Combine(ConfigHelper.StorageFolder, item.UniqueKey.ToString());
                            if (File.Exists(existingFile))
                                File.Delete(existingFile);
                            context.DeleteItem(item);
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
                using (var context = new gFileSystemEntities(_connectionString))
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

        void IDisposable.Dispose()
        {
        }
    }
}