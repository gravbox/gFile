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
        public string EncryptedFileName { get; set; }
        public System.IO.Stream EncryptedStream { get; set; }
        public string ToFile(string outFile = null)
        {
            //Write to text file
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            using (var fileStream = File.Create(tempFile))
            {
                this.EncryptedStream.CopyTo(fileStream);
            }
            return tempFile;
        }

    }
}
