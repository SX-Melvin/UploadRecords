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
        public async static Task<long> CreateFolderIfNotExist(CSDB csdb, OTCS otcs, List<string> nodeNames, List<DivisionData> divisions)
        {
            string ticket = null;
            long result = 2000;
            foreach (var nodeName in nodeNames.Skip(1)) // skip first item (Enterprise)
            { 
                var node = csdb.GetNodeFromParentByName(nodeName, result);
                List<UpdateNodePermissionData> permissionDatas = [
                    new() {
                        Permissions = ["see", "see_contents", "modify", "edit_attributes", "add_items", "reserve", "add_major_version", "delete_versions", "delete", "edit_permissions"],
                        RightID = 1000 // Functional Admin
                    }    
                ];

                foreach (var division in divisions)
                {
                    foreach (var prep in division.PrepDatas ?? [])
                    {
                        permissionDatas.Add(new()
                        {
                            Permissions = ["see", "see_contents"],
                            RightID = prep.ID,
                        });
                    }
                }

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

                    Logger.Information($"Updating Division Access Permission And Admin To Folder {nodeName}");
                    await otcs.UpdateNodePermissionBulk(folder.Id, permissionDatas, ticket);

                    Logger.Information($"Removing Public Access Permission To Folder {nodeName}");
                    await otcs.DeleteNodePublicPermission(folder.Id, ticket);

                    Logger.Information($"Updating Owner Permission To Folder {nodeName}");
                    await otcs.UpdateNodeOwnerPermission(folder.Id, ["see", "see_contents"], ticket);

                    Logger.Information($"Delete Owner Group Permission To Folder {nodeName}");
                    await otcs.DeleteNodePermission(folder.Id, 2001, ticket);

                    Logger.Information($"Removing Owner Group Permission");
                    await otcs.DeleteNodeOwnerGroupPermission(folder.Id, ticket);
                }
            }

            return result;
        }
    }
}
