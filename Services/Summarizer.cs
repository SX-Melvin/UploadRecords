using System.Net.Mail;
using UploadRecords.Models;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Summarizer
    {
        public List<BatchFile> BatchFiles { get; set; }
        public List<string> EmailAddresses { get; set; }
        public MailAddress Sender { get; set; }
        public Scanner Scanner { get; set; }
        public string ReportPath { get; set; }
        public string ReportFileName { get; set; }
        public string BatchNameAndNumber { get; set; }
        public Summarizer(Scanner scanner, List<BatchFile> files, List<string> emailAddresses)
        {
            BatchFiles = files.OrderBy(x => x.StartDate).ToList(); // Sort Start Time ASC
            Scanner = scanner;
            EmailAddresses = emailAddresses;
            ReportFileName = $"{DateTime.Now.ToString("ddMMyyyy")}_{Path.GetFileName(Scanner.FolderPath)}_{Scanner.ControlFile.BatchNumber}.xlsx";
            BatchNameAndNumber = $"{Path.GetFileName(Scanner.FolderPath)}_{Scanner.ControlFile.BatchNumber}";
        }

        public void GenerateReport()
        {
            ReportPath = Excel.GenerateReport(Path.Combine(Path.GetTempPath(), ReportFileName), BatchFiles);
        }

        public void SendMail()
        {
            Mail.SendReportMail(EmailAddresses, this);
        }
    }
}
