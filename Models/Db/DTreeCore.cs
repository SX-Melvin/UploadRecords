using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models.Db
{
    [Table("DTreeCore")]
    public class DTreeCore
    {
        [Key]
        public long DataID { get; set; }
        public string Name { get; set; }
        public long ParentID { get; set; }
    }
}
