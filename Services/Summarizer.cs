using System.Net.Mail;
using UploadRecords.Models;
using UploadRecords.Utils;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace UploadRecords.Services
{
    public class Summarizer
    {
        public long ReportNodeID { get; set; }
        public List<BatchFile> BatchFiles { get; set; }
        public List<string> EmailAddresses { get; set; }
        public MailAddress? Sender { get; set; }
        public string ReportPath { get; set; } = string.Empty;
        public string ReportFileName { get; set; }
        public string BatchNumber { get; set; }
        public MailConfiguration MailConfiguration { get; set; }
        public SummarizerConfiguration Config { get; set; }
        public Summarizer(SummarizerConfiguration config, MailConfiguration mailConfig, List<string> emailAddresses)
        {
            Config = config;
            List<BatchFile> files = [.. Config.InvalidFiles, .. Config.Uploader.ProcessedFiles];

            BatchFiles = files.OrderBy(x => x.StartDate).ToList(); // Sort Start Time ASC
            EmailAddresses = emailAddresses;
            ReportFileName = $"{DateTime.Now.ToString("ddMMyyyyHHmm")}_Batch_{Config.BatchNumber}.xlsx";
            BatchNumber = Config.BatchNumber;
            MailConfiguration = mailConfig;
        }

        public void GenerateReport()
        {
            ReportPath = Excel.GenerateReport(Path.Combine(Path.GetTempPath(), ReportFileName), BatchFiles);
        }

        public async Task SendMail()
        {
            var ticket = await Config.OTCS.GetTicket();
            if(ticket.Error != null || string.IsNullOrEmpty(ticket.Ticket))
            {
                var remarks = $"Fail to upload report file due to {ticket.Error ?? "ticket is empty"}";
                Audit.Fail(BatchFiles[0].LogDirectory, $"{remarks} - {Common.ListAncestors(BatchFiles[0].OTCS.Ancestors)}");
                return;
            }
            
            var file = await Config.OTCS.CreateFile(ReportPath, Config.ReportNodeLocationID, ticket.Ticket);

            if(file.Error != null)
            {
                var remarks = $"Fail to upload report file due to {file.Error}";
                Audit.Fail(BatchFiles[0].LogDirectory, $"{remarks} - {Common.ListAncestors(BatchFiles[0].OTCS.Ancestors)}");
                return;
            }

            ReportNodeID = file.Id;
            Mail.SendReportMail(MailConfiguration, EmailAddresses, this);
        }
    }
}
