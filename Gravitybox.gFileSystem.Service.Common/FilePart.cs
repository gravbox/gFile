using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    [Serializable]
    public class FileInformation
    {
        public Guid TenantID { get; set; }
        public string Container { get; set; }
        public string FileName { get; set; }
        public string CRC { get; set; }
        public long Size { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }
}
