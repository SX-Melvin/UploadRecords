using Newtonsoft.Json;
using RestSharp;
using System.Net;
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
    }
}
