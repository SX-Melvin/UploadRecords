using UploadRecords.Services;
using System.Net.Mail;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Mail
    {
        public static void SendReportMail(MailConfiguration config, List<string> recipients, Summarizer summarizer)
        {
            try
            {
                Logger.Information("Sending email");

                string subject = "Upload Records Notification";
                string body = $"""
                    Dear MND POC and Digitization Vendor POC, <br><br>
                    Please note that the upload of batch {summarizer.BatchNumber} has been completed. <br><br>
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
                    Host = config.Host,
                    Port = config.Port,
                    EnableSsl = false,
                    UseDefaultCredentials = true,
                };

                using (var message = new MailMessage())
                {
                    message.From = new(config.From);

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
            catch(Exception ex) 
            {
                Logger.Error("Error when sending email.", ex);
            }
        }
    }
}
