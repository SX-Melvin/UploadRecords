using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models.Db
{
    [Table("Kuaf")]
    public class KUAF
    {
        [Key]
        public long ID { get; set; }
        public string Name { get; set; }
        public long Type { get; set; }
    }
}
