using Newtonsoft.Json;
using RestSharp;
using System.Net;
using UploadRecords.Models;
using UploadRecords.Models.API;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class OTCS
    {
        readonly string Username;
        readonly string Secret;
        public string HostUrl;
        RestClientOptions RestOptions;
        RestClient Client;

        public OTCS(string username, string secret, string url) 
        {
            var uri = new Uri(url);
            this.Username = username;
            this.Secret = secret;
            HostUrl = $"{uri.Scheme}://{uri.Host}";
            this.RestOptions = new RestClientOptions(url);
            this.Client = new RestClient(this.RestOptions);
        }

        public async Task<GetTicketResponse> GetTicket()
        {
            GetTicketResponse result = new();

            var request = new RestRequest("v1/auth", Method.Post);

            request.AddParameter("username", Username);
            request.AddParameter("password", Secret);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var response = await Client.ExecuteAsync<GetTicketResponse>(request);

            Logger.Information("v1/auth: " + response.Content);

            var data = JsonConvert.DeserializeObject<GetTicketResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }

        public async Task<CreateFileResponse> CreateFile(string filePath, long parentID, string ticket) 
        {
            CreateFileResponse result = new();

            var request = new RestRequest("v1/nodes", Method.Post);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("type", 144);
            request.AddParameter("parent_id", parentID);
            request.AddParameter("name", Path.GetFileName(filePath));
            request.AddFile("file", filePath);

            var response = await Client.ExecuteAsync(request);
            Logger.Information("v1/nodes: " + response.Content);

            var data = JsonConvert.DeserializeObject<CreateFileResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task UpdateNodePermissionBulk(long nodeId, List<UpdateNodePermissionData> permissions, string ticket) 
        {
            foreach(var perm in permissions)
            {
                var request = new RestRequest($"v2/nodes/{nodeId}/permissions/custom", Method.Post);
                request.AddHeader("otcsticket", ticket);
                request.AddParameter("body", JsonConvert.SerializeObject(new
                {
                    right_id = perm.RightID,
                    permissions = perm.Permissions,
                    apply_to = 0,
                    include_sub_types = new List<long>()
                }));
                var response = await Client.ExecuteAsync(request);
                Logger.Information($"Permissions: {JsonConvert.SerializeObject(permissions)}");
                Logger.Information($"v2/nodes/{nodeId}/permissions/custom: " + response.Content);
            }
        }
        public async Task<CommonResponse> DeleteNodePermission(long nodeId, long rightId, string ticket) 
        {
            CommonResponse result = new();

            var request = new RestRequest($"v2/nodes/{nodeId}/permissions/custom/{rightId}", Method.Delete);

            request.AddHeader("otcsticket", ticket);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v2/nodes/{nodeId}/permissions/custom/{rightId}: " + response.Content);

            var data = JsonConvert.DeserializeObject<CommonResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task<CommonResponse> UpdateNodeOwnerPermission(long nodeId, List<string> permissions, string ticket) 
        {
            CommonResponse result = new();

            var request = new RestRequest($"v2/nodes/{nodeId}/permissions/owner", Method.Put);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("body", JsonConvert.SerializeObject(new
            {
                permissions
            }));

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v2/nodes/{nodeId}/permissions/owner: " + response.Content);

            var data = JsonConvert.DeserializeObject<CommonResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task<CreateFileResponse> DeleteNodePublicPermission(long nodeId, string ticket) 
        {
            CreateFileResponse result = new();

            var request = new RestRequest($"v2/nodes/{nodeId}/permissions/public", Method.Delete);

            request.AddHeader("otcsticket", ticket);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v2/nodes/{nodeId}/permissions/public: " + response.Content);

            var data = JsonConvert.DeserializeObject<CreateFileResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task<CreateFileResponse> DeleteNodeOwnerGroupPermission(long nodeId, string ticket) 
        {
            CreateFileResponse result = new();

            var request = new RestRequest($"v2/nodes/{nodeId}/permissions/group", Method.Delete);

            request.AddHeader("otcsticket", ticket);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v2/nodes/{nodeId}/permissions/group: " + response.Content);

            var data = JsonConvert.DeserializeObject<CreateFileResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task<CreateFolderResponse> CreateFolder(string folderName, long parentID, string ticket, List<DivisionData> divisions)
        {
            CreateFolderResponse result = new();

            var request = new RestRequest("v1/nodes", Method.Post);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("type", 0);
            request.AddParameter("parent_id", parentID);
            request.AddParameter("name", folderName);

            var response = await Client.ExecuteAsync(request);
            Logger.Information("v1/nodes: " + response.Content);

            var data = JsonConvert.DeserializeObject<CreateFolderResponse>(response.Content);

            if (data != null)
            {
                result = data;

                // Folder with that name already exists, lets get the folder directly
                if(result.Error != null)
                {
                    if(result.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    {
                        var getFolder = await GetNodeFromParentByName(folderName, parentID, 0, ticket);

                        if (getFolder != null && getFolder.Results.Count > 0) 
                        {
                            result.Id = getFolder.Results[0].Data.Properties.Id;
                            result.Error = null;
                        }
                    }
                    return result;
                }

                await UpdateFolderPermission(result.Id, folderName, ticket, divisions);
            }

            return result;
        }
        public async Task UpdateFolderPermission(long nodeId, string folderName, string ticket, List<DivisionData> divisions)
        {
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

            Logger.Information($"Updating Division Access Permission And Admin To Folder {folderName}");
            await UpdateNodePermissionBulk(nodeId, permissionDatas, ticket);

            Logger.Information($"Removing Public Access Permission To Folder {folderName}");
            await DeleteNodePublicPermission(nodeId, ticket);

            Logger.Information($"Updating Owner Permission To Folder {folderName}");
            await UpdateNodeOwnerPermission(nodeId, ["see", "see_contents"], ticket);

            Logger.Information($"Delete Owner Group Permission To Folder {folderName}");
            await DeleteNodePermission(nodeId, 2001, ticket);

            Logger.Information($"Removing Owner Group Permission");
            await DeleteNodeOwnerGroupPermission(nodeId, ticket);
        }
        public async Task<GetNodeSubnodesResponse> GetNodeFromParentByName(string nodeName, long parentID, int type, string ticket, int limit = 1)
        {
            GetNodeSubnodesResponse result = new();

            var request = new RestRequest($"v2/nodes/{parentID}/nodes", Method.Get);

            request.AddHeader("otcsticket", ticket);
            request.AddQueryParameter("where_type", type);
            request.AddQueryParameter("where_name", nodeName);
            request.AddQueryParameter("limit", limit);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v2/nodes/{parentID}/nodes: " + response.Content);

            var data = JsonConvert.DeserializeObject<GetNodeSubnodesResponse>(response.Content);

            if (data != null)
            {
                result = data;
            }

            return result;
        }

        public async Task<GetNodeAncestorsResponse> GetNodeAncestors(long nodeID, string ticket) 
        {
            GetNodeAncestorsResponse result = new()
            {
                Ancestors = []
            };

            var request = new RestRequest($"v1/nodes/{nodeID}/ancestors", Method.Get);

            request.AddHeader("otcsticket", ticket);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v1/nodes/{nodeID}/ancestors" + response.Content);

            var data = JsonConvert.DeserializeObject<GetNodeAncestorsResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
        public async Task<ApplyCategoryResponse> ApplyCategoryOnNode(long nodeID, string body, long catID, string ticket)
        {
            ApplyCategoryResponse result = new();

            var request = new RestRequest($"v1/nodes/{nodeID}/categories", Method.Post);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("body", body);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v1/nodes/{nodeID}/categories body: " + body);
            Logger.Information($"v1/nodes/{nodeID}/categories: " + response.Content);

            var data = JsonConvert.DeserializeObject<ApplyCategoryResponse>(response.Content);

            if(data != null)
            {
                result = data;

                // The category already exist, lets update it then
                if(result.Error != null && result.Error.Contains("already exists"))
                {
                    var updateCat = await UpdateCategoryOnNode(catID, nodeID, body, ticket);
                    if (updateCat != null && updateCat.Error == null)
                    {
                        result.Error = null;
                    }
                }
            }

            return result;
        }
        public async Task<ApplyCategoryResponse> UpdateCategoryOnNode(long catID, long nodeID, string body, string ticket)
        {
            ApplyCategoryResponse result = new();

            var request = new RestRequest($"v1/nodes/{nodeID}/categories/{catID}", Method.Put);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("body", body);

            var response = await Client.ExecuteAsync(request);
            Logger.Information($"v1/nodes/{nodeID}/categories/{catID} body: " + body);
            Logger.Information($"v1/nodes/{nodeID}/categories/{catID}: " + response.Content);

            var data = JsonConvert.DeserializeObject<ApplyCategoryResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
    }
}
