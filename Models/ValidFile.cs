using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class ValidFile
    {
        public required string Name { get; set; }
        public required string Path { get; set; }
        public required string Checksum { get; set; }
        public required string BatchFolderPath { get; set; }
        public required string SubBatchFolderPath { get; set; }
        public required ValidFileOTCS OTCS { get; set; }
    }

    public class ValidFileOTCS
    {
        public required int ParentID { get; set; }
    }
}
