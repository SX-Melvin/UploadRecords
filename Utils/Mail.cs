using UploadRecords.Models;
using UploadRecords.Services;
using System.Net;
using System.Net.Mail;

namespace UploadRecords.Utils
{
    public static class Mail
    {
        public static void SendReportMail(List<string> recipients, Summarizer summarizer)
        {
            Logger.Information("Sending email");

            string subject = "Upload Records Notification";
            string body = $"""
                Dear MND POC and Digitization Vendor POC, <br><br>
                Please note that the upload of batch {summarizer.BatchNameAndNumber} has been completed. <br><br>
                Summary Report <br>
                Total Files Ran: {summarizer.BatchFiles.Count} <br>
                Total Success: {summarizer.BatchFiles.Count(x => x.Status == Enums.BatchFileStatus.Completed)} <br>   
                Total Failed: {summarizer.BatchFiles.Count(x => x.Status == Enums.BatchFileStatus.Failed)} <br>
                Total Skipped: {summarizer.BatchFiles.Count(x => x.Status == Enums.BatchFileStatus.Skipped)} <br><br>
                Please refer to attached file for detailed report. <br><br>
                This is a system generated email for your information. Please do not reply to this email. <br><br>
                Thank you.
            """;

            var smtp = new SmtpClient
            {
                Host = "192.168.1.127",
                Port = 111,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
            };

            using (var message = new MailMessage())
            {
                message.From = new("info@244.com");

                foreach (var email in recipients)
                {
                    message.To.Add(email);
                }

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(summarizer.ReportPath))
                {
                    message.Attachments.Add(new Attachment(summarizer.ReportPath));
                }

                smtp.Send(message);
            }

            Logger.Information("Email sent");
        }
    }
}
