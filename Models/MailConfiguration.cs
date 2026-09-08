using System.Net;
using System.Net.Mail;

namespace UploadRecords.Models
{
    public class MailConfiguration
    {
        public required string From { get; set; }
        public required string Host { get; set; }
        public int Port { get; set; }
    }
}
