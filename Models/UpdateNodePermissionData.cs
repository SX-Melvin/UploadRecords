using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class UpdateNodePermissionData
    {
        public List<string> Permissions { get; set; }
        public long RightID { get; set; }
    }
}
