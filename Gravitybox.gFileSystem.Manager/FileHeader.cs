using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Manager
{
    internal class FileHeader
    {
        public const int FileHeaderSize = 128;

        public short FileVersion { get; set; } = 1;
        public bool IsCompressed { get; set; } = false;
        public byte[] EncryptedDataKey { get; set; }
        public byte[] DataKey { get; set; }
        public byte[] TenantKey { get; set; }

        public bool Load(byte[] arr)
        {
            //File version
            var tarr = new byte[2];
            Buffer.BlockCopy(arr, 0, tarr, 0, tarr.Length);
            this.FileVersion = BitConverter.ToInt16(tarr, 0);

            //Data key
            this.EncryptedDataKey = new byte[48];
            Buffer.BlockCopy(arr, 2, this.EncryptedDataKey, 0, this.EncryptedDataKey.Length);

            //Compressed
            tarr = new byte[1];
            Buffer.BlockCopy(arr, 50, tarr, 0, tarr.Length);
            this.IsCompressed = BitConverter.ToBoolean(tarr, 0);

            return true;
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
