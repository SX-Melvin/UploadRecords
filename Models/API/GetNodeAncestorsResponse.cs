using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UploadRecords.Models.API
{
    public class GetNodeAncestorsResponse : CommonResponse
    {
        public List<GetNodeAcestorsAncestor> Ancestors { get; set; } = null;
    }

    public class GetNodeAcestorsAncestor 
    { 
        public long Id { get; set; }
        public int Type { get; set; }
        public string Name { get; set; }

        [JsonProperty("volume_id")]
        public int VolumeId { get; set; }

        [JsonProperty("parent_id")]
        public long ParentID { get; set; }   

        [JsonProperty("type_name")]
        public string TypeName { get; set; }   
    }
}
