using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    [Serializable]
    public class FileDataInfo
    {
        public Guid Token { get; set; }
        public long Size { get; set; }
    }

}
