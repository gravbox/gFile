using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Engine
{
    internal static class FileUtilities
    {
        /// <summary>
        /// Create a new encryption key with the specified key size
        /// </summary>
        internal static byte[] GetNewKey(int keyByteSize)
        {
            var aes = new System.Security.Cryptography.AesManaged();
            aes.KeySize = keyByteSize * 8;
            aes.GenerateKey();
            return aes.Key;
        }

        internal static void EncryptStream(this System.IO.Stream stream, string targetFile,
          byte[] masterKey, byte[] iv, byte[] tenantKey)
        {
            try
            {
                var aes = CryptoProvider(FileUtilities.GetNewKey(32), iv);

                //Encrypt the data key
                var header = new FileHeader { EncryptedDataKey = aes.Key.Encrypt(tenantKey, iv) };
                var padBytes = header.ToArray();
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    using (var newFile = File.Create(targetFile))
                    {
                        //Write encrypted data key to front of file
                        newFile.Write(padBytes, 0, padBytes.Length);

                        //Write encrypted data
                        using (var cryptoStream = new CryptoStream(newFile, encryptor, CryptoStreamMode.Write))
                        {
                            stream.CopyTo(cryptoStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        internal static void DecryptStream(this System.IO.Stream stream, string targetFile,
            byte[] masterKey, byte[] iv, byte[] tenantKey)
        {
            try
            {
                //Get the data key
                var vv = new byte[FileHeader.FileHeaderSize];
                stream.Read(vv, 0, vv.Length);
                var header = FileHeader.Load(vv);

                var dataKey = header.EncryptedDataKey.Decrypt(tenantKey, iv);
                var aes = CryptoProvider(dataKey, iv);
                using (ICryptoTransform encryptor = aes.CreateDecryptor())
                {
                    using (var newFile = File.OpenWrite(targetFile))
                    {
                        stream.Seek(FileHeader.FileHeaderSize, SeekOrigin.Begin);
                        using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(newFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        internal static AesCryptoServiceProvider CryptoProvider(byte[] key, byte[] iv)
        {
            var aes = new AesCryptoServiceProvider();
            aes.IV = iv;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}
