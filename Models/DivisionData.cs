using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models.Db;

namespace UploadRecords.Models
{
    public class DivisionData: DivisionConfiguration
    {
        public bool UsedInNote2 { get; set; } = false;
        public List<KUAF> PrepDatas { get; set; } = [];
    }
}
