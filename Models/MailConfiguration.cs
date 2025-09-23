using System.Net;
using System.Net.Mail;

namespace UploadRecords.Models
{
    public class MailConfiguration
    {
        public string From { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
