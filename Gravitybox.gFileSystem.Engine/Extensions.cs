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
                var aes = FileUtilities.CryptoProvider(masterKey, iv);
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
                var aes = FileUtilities.CryptoProvider(masterKey, iv);
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

    }

}