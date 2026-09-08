using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Configs
{
    public class OtcsConfig
    {
        public required string APIUrl { get; set; }
        public required string Username { get; set; }
        public required string Secret { get; set; }
    }
}
