using Newtonsoft.Json;
using RestSharp;
using System.Net;
using UploadRecords.Models.API;

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

            var data = JsonConvert.DeserializeObject<GetTicketResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }

        public async Task<CreateFileResponse> CreateFile(string filePath, int parentID, string? ticket = null) 
        {
            CreateFileResponse result = new();

            var getTicket = await GetTicket();
            if (getTicket.Error != null)
            {
                result.Error = getTicket.Error;
                return result;
            }

            ticket ??= getTicket.Ticket;

            var request = new RestRequest("v1/nodes", Method.Post);

            request.AddHeader("otcsticket", ticket);
            request.AddParameter("type", 144);
            request.AddParameter("parent_id", parentID);
            request.AddParameter("name", Path.GetFileName(filePath));
            request.AddFile("file", filePath);

            var response = await Client.ExecuteAsync(request);
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
        
        public async Task<GetNodeAncestorsResponse> GetNodeAncestors(int nodeID, string? ticket = null) 
        {
            GetNodeAncestorsResponse result = new()
            {
                Ancestors = []
            };

            var getTicket = await GetTicket();
            if (getTicket.Error != null)
            {
                result.Error = getTicket.Error;
                return result;
            }

            ticket ??= getTicket.Ticket;

            var request = new RestRequest($"v1/nodes/{nodeID}/ancestors", Method.Get);

            request.AddHeader("otcsticket", ticket);

            var response = await Client.ExecuteAsync(request);
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
