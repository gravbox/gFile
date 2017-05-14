using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    public class FileProgressEventArgs : System.EventArgs
    {
        public string FileName { get; set; }
        public string Container { get; set; }
        public int TotalChunks { get; set; }
        public int ChunkIndex { get; set; }
    }
}
