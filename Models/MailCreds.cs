using System.Net;
using System.Net.Mail;

namespace UploadRecords.Models
{
    public class MailCreds
    {
        public MailAddress MailAddress { get; set; }
        public string MailSecret { get; set; }
    }
}
