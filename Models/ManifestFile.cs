using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class ManifestFile
    {
        public required string Checksum { get; set; }
        public required string Path { get; set; }
    }
}
