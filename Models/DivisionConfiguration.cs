using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class DivisionConfiguration
    {
        public string Name { get; set; }
        public List<string> Preps { get; set; } = [];
    }
}
