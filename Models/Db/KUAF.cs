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
    public class Kuaf
    {
        [Key]
        public long ID { get; set; }
        public required string Name { get; set; }
        public long Type { get; set; }
    }
}
