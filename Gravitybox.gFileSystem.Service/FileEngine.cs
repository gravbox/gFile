using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    /// <summary>
    /// This class is NOT thread safe. You can add and remove the same file 
    /// on different threads with unpredictable results
    /// You can build you own concurrency manager or use the
    /// The default SQL FileManager project that does this already
    /// </summary>
    public class FileEngine : IDisposable
    {
        #region Locals
        private byte[] _masterKey = null;
        private byte[] _iv = null;
        private byte[] _tenantKey = null;
        public static readonly byte[] DefaultIVector = Encoding.UTF8.GetBytes("@2CdcëD45F6%d7H");
        #endregion

        public FileEngine(byte[] masterKey, byte[] tenantKey, Guid tenantId)
            : this(masterKey, tenantKey, tenantId, DefaultIVector)
        {
        }

        public FileEngine(byte[] masterKey, byte[] tenantKey, Guid tenantId, byte[] iv)
        {
            if (masterKey == null || masterKey.Length != 32)
                throw new Exception("The master key must be a 256 bit (32 byte) value.");
            if (tenantKey == null || tenantKey.Length != 32)
                throw new Exception("The tenant key must be a 256 bit (32 byte) value.");
            if (iv == null || iv.Length != 16)
                throw new Exception("The IV must be a 128 bit (16 byte) value.");
            if (tenantId == Guid.Empty)
                throw new Exception("The TenantId must be set.");

            if (masterKey.Length != tenantKey.Length)
                throw new Exception("The master key and tenant key must be the same size.");

            this.TenantID = tenantId;

            _masterKey = masterKey;
            _iv = iv;
            _tenantKey = tenantKey;
            this.WorkingFolder = Path.GetTempPath();
        }

        /// <summary>
        /// The folder where files will be copied temporarily when encrypting and decrypting
        /// </summary>
        public string WorkingFolder { get; set; }

        public Guid TenantID { get; private set; }

        /// <summary>
        /// Given a file, this will encrypt it and put it in storage
        /// </summary>
        public string SaveFile(string fileName)
        {
            try
            {
                var newFile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString() + ".crypt");
                var header = new FileHeader { DataKey = FileUtilities.GetNewKey() };
                header.EncryptedDataKey = header.DataKey.Encrypt(_tenantKey, _iv);
                header.TenantKey = _tenantKey;

                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    fs.EncryptStream(newFile, _iv, header);
                }
                return newFile;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Given a file, this will encrypt it and put it in storage
        /// </summary>
        public string SaveFile(byte[] data)
        {
            try
            {
                var newFile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString() + ".crypt");
                var header = new FileHeader { DataKey = FileUtilities.GetNewKey() };
                header.EncryptedDataKey = header.DataKey.Encrypt(_tenantKey, _iv);
                header.TenantKey = _tenantKey;

                using (var fs = new MemoryStream(data))
                {
                    fs.EncryptStream(newFile, _iv, header);
                }
                return newFile;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Given a file, this will decrypt it from storage and 
        /// return a temporary file in the working storage area
        /// For security, you need to call WipeFile when done.
        /// </summary>
        public string GetFile(string cryptFileName)
        {
            try
            {
                if (!File.Exists(cryptFileName))
                    return null;

                var header = new FileHeader { TenantKey = _tenantKey };
                var newFile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString() + ".decrypt");
                using (var ts = File.OpenWrite(newFile))
                {
                    using (var fs = File.Open(cryptFileName, FileMode.Open, FileAccess.Read))
                    {
                        fs.DecryptStream(ts, _iv, header);
                    }
                }
                return newFile;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public byte[] GetFileData(string cryptFileName)
        {
            try
            {
                if (!File.Exists(cryptFileName))
                    return null;

                var header = new FileHeader { TenantKey = _tenantKey };
                using (var ts = new MemoryStream())
                {
                    using (var fs = File.Open(cryptFileName, FileMode.Open, FileAccess.Read))
                    {
                        fs.DecryptStream(ts, _iv, header);
                    }
                    return ts.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// This will rekey a tenant file with a new tenant key
        /// </summary>
        /// <param name="cryptFileName"></param>
        /// <param name="newTenantKey"></param>
        /// <returns></returns>
        public bool RekeyFile(string cryptFileName, byte[] newTenantKey)
        {
            try
            {
                var header = new FileHeader();
                using (var fs = File.Open(cryptFileName, FileMode.Open, FileAccess.Read))
                {
                    //Get the data key
                    var vv = new byte[FileHeader.FileHeaderSize];
                    fs.Read(vv, 0, vv.Length);
                    header.Load(vv);
                    var dataKey = header.EncryptedDataKey.Decrypt(_tenantKey, _iv);
                    if (dataKey == null)
                    {
                        Logger.LogWarning("File could not be decrypted: " + cryptFileName);
                        return false;
                    }

                    //Create new file pad with data key encrypted with new tenant key
                    header.TenantKey = newTenantKey;
                    header.DataKey = dataKey;
                    header.EncryptedDataKey = dataKey.Encrypt(newTenantKey, _iv);
                }
                Extensions.WriteFileHeader(cryptFileName, header);

                return true;
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
