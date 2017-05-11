using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Engine
{
    internal class FileHeader
    {
        public const int FileHeaderSize = 128;

        public short FileVersion { get; set; } = 1;
        public bool IsCompressed { get; set; } = false;
        public byte[] EncryptedDataKey { get; set; }

        public static FileHeader Load(byte[] arr)
        {
            var retval = new FileHeader();

            //File version
            var tarr = new byte[2];
            Buffer.BlockCopy(arr, 0, tarr, 0, tarr.Length);
            retval.FileVersion = BitConverter.ToInt16(tarr, 0);

            //Data key
            retval.EncryptedDataKey = new byte[48];
            Buffer.BlockCopy(arr, 2, retval.EncryptedDataKey, 0, retval.EncryptedDataKey.Length);

            //Compressed
            tarr = new byte[1];
            Buffer.BlockCopy(arr, 50, tarr, 0, tarr.Length);
            retval.IsCompressed = BitConverter.ToBoolean(tarr, 0);

            return retval;
        }

        /// <summary>
        /// Gets the byte array to prepend to the encrypted file
        /// </summary>
        internal byte[] ToArray()
        {
            var retval = new byte[FileHeader.FileHeaderSize];

            //File version [0-1]
            var verArr = BitConverter.GetBytes(this.FileVersion);
            Buffer.BlockCopy(verArr, 0, retval, 0, verArr.Length);

            //Data key [2-49]
            byte[] keyArr = this.EncryptedDataKey;
            Buffer.BlockCopy(keyArr, 0, retval, 2, keyArr.Length);

            //Compressed [50]
            var compArr = BitConverter.GetBytes(this.IsCompressed);
            Buffer.BlockCopy(compArr, 0, retval, 50, compArr.Length);

            //[51-127] Reserved

            return retval;
        }

    }
}
