using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async static Task<long> CreateFolderIfNotExist(CSDB csdb, OTCS otcs, List<string> nodeNames)
        {
            string ticket = null;
            long result = 2000;
            foreach (var nodeName in nodeNames.Skip(1)) // skip first item (Enterprise)
            { 
                var node = csdb.GetNodeFromParentByName(nodeName, result);

                if(node != null)
                {
                    result = node.DataID;
                    continue;
                }

                // Node Not Exist, Lets Create It
                ticket ??= (await otcs.GetTicket()).Ticket;
                var folder = await otcs.CreateFolder(nodeName, result, ticket);
                if (folder != null)
                {
                    result = folder.Id;
                }
            }

            return result;
        }
    }
}
