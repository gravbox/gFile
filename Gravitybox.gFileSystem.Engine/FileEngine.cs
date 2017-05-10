using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Engine
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
        public static readonly byte[] DefaultIV16 = Encoding.UTF8.GetBytes("@2CdcëD45F6%d7H");
        public static readonly byte[] DefaultIV32 = Encoding.UTF8.GetBytes("@2CdcëD45F6%d7Hz[(25Vo(q:'30(d8");
        #endregion

        public FileEngine(byte[] masterKey, byte[] tenantKey)
            : this(masterKey, tenantKey, GetDefaultIV(masterKey.Length))
        {
        }

        public FileEngine(byte[] masterKey, byte[] tenantKey, byte[] iv)
        {
            if (masterKey == null || (masterKey.Length != 16 && masterKey.Length != 32))
                throw new Exception("The master key must be a 128/256 bit value.");
            if (tenantKey == null || (tenantKey.Length != 16 && tenantKey.Length != 32))
                throw new Exception("The tenant key must be a 128/256 bit value.");
            if (iv == null || (iv.Length != 16 && iv.Length != 32))
                throw new Exception("The IV must be a 128/256 bit value.");

            if (masterKey.Length != tenantKey.Length || tenantKey.Length != iv.Length)
                throw new Exception("The master key, tenant key, and IV must be the same size.");

            this.KeySize = tenantKey.Length * 8;

            _masterKey = masterKey;
            _iv = iv;
            _tenantKey = tenantKey;
            this.WorkingFolder = Path.GetTempPath();
        }

        public static byte[] GetDefaultIV(int length)
        {
            if (length == 16)
                return DefaultIV16;
            else
                return DefaultIV32;
        }

        /// <summary>
        /// Determines the key size based on the used keys
        /// The master and tenant keys must be the same size
        /// </summary>
        public int KeySize { get; private set; } = 32;

        /// <summary>
        /// The folder where files will be copied temporarily when encrypting and decrypting
        /// </summary>
        public string WorkingFolder { get; set; }

        /// <summary>
        /// Given a file, this will encrypt it and put it in storage
        /// </summary>
        public string SaveFile(string fileName)
        {
            try
            {
                var newFile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString() + ".crypt");
                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    fs.EncryptStream(newFile, _masterKey, _iv, _tenantKey);
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

                var newFile = Path.Combine(this.WorkingFolder, Guid.NewGuid().ToString() + ".decrypt");
                using (var fs = File.Open(cryptFileName, FileMode.Open, FileAccess.Read))
                {
                    fs.DecryptStream(newFile, _masterKey, _iv, _tenantKey);
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
        /// This will rekey a tenant file with a new tenant key
        /// </summary>
        /// <param name="cryptFileName"></param>
        /// <param name="newTenantKey"></param>
        /// <returns></returns>
        public bool RekeyFile(string cryptFileName, byte[] newTenantKey)
        {
            try
            {
                byte[] padBytes = null;
                using (var fs = File.Open(cryptFileName, FileMode.Open, FileAccess.Read))
                {
                    //Get the data key
                    var dataKey = new byte[32];
                    var vv = new byte[48];
                    fs.Read(vv, 0, vv.Length);
                    dataKey = vv.Decrypt(_tenantKey, _iv);
                    //Create new file pad with data key encrypted with new tenant key
                    padBytes = dataKey.Encrypt(newTenantKey, _iv);
                }

                using (var file = File.OpenWrite(cryptFileName))
                {
                    //Write encrypted data key to front of file
                    file.Write(padBytes, 0, padBytes.Length);
                }

                return false;
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
