using RestSharp;
using Newtonsoft.Json;
using UploadRecords.Models.API;

namespace UploadRecords.Utils
{
    public class OTCS
    {
        readonly string username;
        readonly string secret;
        RestClientOptions restOptions;
        RestClient client;

        public OTCS(string username, string secret, string url) 
        {
            this.username = username;
            this.secret = secret;
            restOptions = new RestClientOptions(url);
            client = new RestClient(restOptions);
        }

        public async Task<GetTicketResponse> GetTicket()
        {
            GetTicketResponse result = new();

            var request = new RestRequest("v1/auth", Method.Post);

            request.AddParameter("username", username);
            request.AddParameter("password", secret);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var response = await client.ExecuteAsync<GetTicketResponse>(request);

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

            var response = await client.ExecuteAsync<CreateFileResponse>(request);
            var data = JsonConvert.DeserializeObject<CreateFileResponse>(response.Content);

            if(data != null)
            {
                result = data;
            }

            return result;
        }
    }
}
