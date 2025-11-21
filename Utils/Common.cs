using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;
using UploadRecords.Models.API;
using UploadRecords.Services;

namespace UploadRecords.Utils
{
    public static class Common
    {
        public static string ListAncestors(List<GetNodeAcestorsAncestor> ancestors)
        {
            if(ancestors == null)
            {
                return "";
            }
            return string.Join(":", ancestors.Select(x => x.Name));
        }
    }
}
