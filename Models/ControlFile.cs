using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class ControlFile
    {
        public string? BatchNumber { get; set; }
        public string? MicrofilmNumber { get; set; }
        public string? RecordSeriesTitle { get; set; }
        public DateTime? TransferDate { get; set; }
        public string? AuthorityNumber { get; set; }
        public string? RecordType { get; set; }
        public string? FolderRef { get; set; }
        public string? FolderTitle { get; set; }
        public string? FolderSecurityGrading { get; set; }
        public List<string> FolderPath { get; set; } = [];
        public string? FolderSensitivityClassification { get; set; }
        public string? Note1 { get; set; }
        public string? Note2 { get; set; }
    }
}
