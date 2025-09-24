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
        RestClientOptions RestOptions;
        RestClient Client;

        public OTCS(string username, string secret, string url) 
        {
            this.Username = username;
            this.Secret = secret;
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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

            var data = JsonConvert.DeserializeObject<CreateFileResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }

        public async Task<CreateFolderResponse> CreateFolder(string folderName, long parentID, string ticket)
        {
            CreateFolderResponse result = new();

            var request = new RestRequest("v1/nodes", Method.Post);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("type", 0);
            request.AddParameter("parent_id", parentID);
            request.AddParameter("name", folderName);

            var response = await Client.ExecuteAsync(request);
            Logger.Information("v1/nodes: " + response.Content);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

            var data = JsonConvert.DeserializeObject<CreateFolderResponse>(response.Content);

            if (data != null)
            {
                result = data;

                // Folder with that name already exists, lets get the folder directly
                if(result.Error != null && result.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    var getFolder = await GetNodeFromParentByName(folderName, parentID, 0, ticket);

                    if (getFolder != null && getFolder.Results.Count > 0) 
                    {
                        result.Id = getFolder.Results[0].Data.Properties.Id;
                        result.Error = null;
                    }
                }
            }

            return result;
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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException($"Unauthorized: {response.Content}", null, HttpStatusCode.Unauthorized);
            }

            var data = JsonConvert.DeserializeObject<ApplyCategoryResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
    }
}
