using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    internal class FilePartCache
    {
        public DateTime ObjectCreation { get; set; } = DateTime.Now;
        /// <summary>
        /// The unique token used in multi-part file upload/download to identify the file
        /// </summary>
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// The tenant user which this owns this file
        /// </summary>
        public Guid TenantID { get; set; }
        /// <summary>
        /// The tenant container name where the file is stored
        /// </summary>
        public string Container { get; set; }
        /// <summary>
        /// The actual file name used as the key for unique lookup of this file
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// The CRC hash of the plain text file
        /// </summary>
        public string CRC { get; set; }
        /// <summary>
        /// The total plain text file size
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// The current index of the file part being uploaded/downloaded
        /// </summary>
        public int Index { get; set; } = 0;
        /// <summary>
        /// The temp data folder in the working area used to store the file contents while in transit
        /// </summary>
        public string TempDataFile { get; set; }
        /// <summary>
        /// Locking key used for synchronization
        /// </summary>
        public string Key => (this.TenantID + "|" + this.Container + "|" + this.FileName).ToLower();
        /// <summary>
        /// Determine if this operation stores the data in memory or on disk
        /// </summary>
        public bool InMem { get; set; }
        /// <summary>
        /// When using memory for smaller files and not using the working area
        /// This is the data block used to store the file contents
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// This is a one time key used to encrypt files parts as they are uploaded/downloaded.
        /// File parts are stored on disk in the working area encrypted while the file is in transit
        /// </summary>
        public byte[] OneTimeKey = new byte[32];// FileUtilities.GetNewKey();
        /// <summary>
        /// The decrypted stream to use when downloading a file
        /// </summary>
        public System.IO.Stream DecryptStream { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public byte[] TenantKey { get; set; }
        public System.IO.Stream EncryptStream { get; set; }
    }
}
