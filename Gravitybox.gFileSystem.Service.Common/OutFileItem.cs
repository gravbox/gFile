using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    public class OutfileItem
    {
        /// <summary>
        /// The temp file on disk that holds the encrypted file
        /// When you are done with this file be sure to delete it
        /// </summary>
        public string EncryptedFileName { get; set; }

        /// <summary>
        /// An open stream to the encrypted file so that you can write to anywhere needed
        /// </summary>
        public System.IO.Stream EncryptedStream { get; set; }

        /// <summary>
        /// Convenience method that dumps the decrypted stream to a plaintext file
        /// </summary>
        public string ToFile(string outFile = null)
        {
            //Write to text file
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            using (var fileStream = File.Create(tempFile))
            {
                this.EncryptedStream.CopyTo(fileStream);
            }
            this.EncryptedStream.Close();
            return tempFile;
        }

    }
}
