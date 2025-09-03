using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models
{
    public class QueueItem
    {
        public DateTime RunAt { get; set; }
        public ValidFile File { get; set; }
        public int TotalRun { get; set; } = 0;
    }
}
