using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using UploadRecords.Models;
using UploadRecords.Services;

namespace UploadRecords.Utils
{
    public static class Mail
    {
        public static void SendReportMail(List<string> recipients, MailCreds mailCreds, Summarizer summarizer)
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

            //var smtp = new SmtpClient
            //{
            //    Host = "mail.swiftx.co",
            //    Port = 587,
            //    EnableSsl = false,
            //    DeliveryMethod = SmtpDeliveryMethod.Network,
            //    UseDefaultCredentials = true,
            //    //Credentials = new NetworkCredential(mailCreds.MailAddress.Address, mailCreds.MailSecret)
            //};

            //using (var message = new MailMessage())
            //{
            //    message.From = mailCreds.MailAddress;

            //    foreach (var email in recipients)
            //    {
            //        message.To.Add(email);
            //    }

            //    message.Subject = subject;
            //    message.Body = body;
            //    message.IsBodyHtml = true;

            //    if (!string.IsNullOrEmpty(summarizer.ReportPath))
            //    {
            //        message.Attachments.Add(new Attachment(summarizer.ReportPath));
            //    }

            //    smtp.Send(message);
            //}

            // 1. Build the message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("No Reply", "noreply@mndrmsapp.dev"));

            foreach (var email in recipients)
            {
                message.To.Add(MailboxAddress.Parse(email));
            }

            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body // or TextBody = body for plain text
            };

            if (!string.IsNullOrEmpty(summarizer.ReportPath))
            {
                builder.Attachments.Add(summarizer.ReportPath);
            }

            message.Body = builder.ToMessageBody();

            // 2. Send it
            using (var client = new SmtpClient())
            {
                // accept all SSL certificates (for dev only)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // connect to your hMailServer
                // Port 587 = STARTTLS. If you haven't enabled STARTTLS in hMailServer, use SecureSocketOptions.None.
                client.Connect("192.168.1.79", 587, SecureSocketOptions.None);

                // authenticate
                //client.Authenticate("noreply@mndrmsapp.dev", "P@ssw0rd");

                client.Send(message);
                client.Disconnect(true);
            }

            Logger.Information("Email sent");
        }
    }
}
