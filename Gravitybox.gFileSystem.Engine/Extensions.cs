using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Engine
{
    public static class Extensions
    {
        public static byte[] Encrypt(this byte[] src, byte[] masterKey, byte[] iv)
        {
            try
            {
                var aes = CryptoProvider(masterKey, iv);
                using (ICryptoTransform encrypt = aes.CreateEncryptor())
                {
                    byte[] dest = encrypt.TransformFinalBlock(src, 0, src.Length);
                    return dest;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static byte[] Decrypt(this byte[] src, byte[] masterKey, byte[] iv)
        {
            try
            {
                var aes = CryptoProvider(masterKey, iv);
                using (ICryptoTransform decrypt = aes.CreateDecryptor())
                {
                    byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                    return dest;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        internal static void EncryptStream(this System.IO.Stream stream, string targetFile,
            byte[] masterKey, byte[] iv, byte[] tenantKey)
        {
            try
            {
                var aes = CryptoProvider(FileUtilities.GetNewKey(32), iv);

                //Encrypt the data key
                byte[] padBytes = aes.Key.Encrypt(tenantKey, iv);
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
                var vv = new byte[48];
                stream.Read(vv, 0, vv.Length);
                var dataKey = vv.Decrypt(tenantKey, iv);
                var aes = CryptoProvider(dataKey, iv);
                using (ICryptoTransform encryptor = aes.CreateDecryptor())
                {
                    using (var newFile = File.OpenWrite(targetFile))
                    {
                        stream.Seek(48, SeekOrigin.Begin);
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

        private static AesCryptoServiceProvider CryptoProvider(byte[] key, byte[] iv)
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