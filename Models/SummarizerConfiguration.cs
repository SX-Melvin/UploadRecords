using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Services;

namespace UploadRecords.Models
{
    public class SummarizerConfiguration
    {
        public long ReportNodeLocationID { get; set; }
        public OTCS OTCS { get; set; }
        public string BatchNumber { get; set; }
        public List<BatchFile> InvalidFiles { get; set; }
        public Uploader Uploader { get; set; }
    }
}
