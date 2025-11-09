using ClosedXML.Excel;
using ExcelDataReader;
using System.Data;
using System.Globalization;
using System.Text;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Excel
    {
        public static string GenerateReport(string savePath, List<BatchFile> batchFiles)
        {
            Logger.Information("Beginning Writing Report File");

            // Create a new workbook
            using var workbook = new XLWorkbook();

            // Add a worksheet
            var worksheet = workbook.Worksheets.Add("Sheet1");

            // Fill some data
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Start Time";
            worksheet.Cell(1, 3).Value = "End Time";
            worksheet.Cell(1, 4).Value = "Time Taken";
            worksheet.Cell(1, 5).Value = "File Name";
            worksheet.Cell(1, 6).Value = "File Size (KB)";
            worksheet.Cell(1, 7).Value = "Attempt";
            worksheet.Cell(1, 8).Value = "Status";
            worksheet.Cell(1, 9).Value = "Remarks";

            int row = 2;
            foreach (var batchFile in batchFiles)
            {
                var ts = TimeSpan.FromSeconds((batchFile.EndDate - batchFile.StartDate).TotalSeconds);

                worksheet.Cell(row, 1).Value = batchFile.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                worksheet.Cell(row, 2).Value = batchFile.StartDate.ToString("HH:mm:ss tt");
                worksheet.Cell(row, 3).Value = batchFile.EndDate.ToString("HH:mm:ss tt");
                worksheet.Cell(row, 4).Value = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
                worksheet.Cell(row, 5).Value = batchFile.Name;
                worksheet.Cell(row, 6).Value = batchFile.SizeInKB;
                worksheet.Cell(row, 7).Value = batchFile.Attempt;
                worksheet.Cell(row, 8).Value = batchFile.Status.ToString();
                worksheet.Cell(row, 9).Value = batchFile.Remarks;
                row++;
            }

            // Style header row
            var header = worksheet.Range("A1:I1");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.Black;
            header.Style.Font.FontColor = XLColor.White;

            // Save to file
            workbook.SaveAs(savePath);

            Logger.Information($"Report file saved on {savePath}");

            return savePath;
        }

        public static List<ControlFile>? ReadControlFile(string filePath)
        {
            Logger.Information($"Reading control file on {filePath}");

            List<ControlFile>? result = null;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var file = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            var table = file.Tables[0];

            foreach (DataRow row in table.Rows)
            {
                result ??= [];

                ControlFile data = new();

                if (DateTime.TryParse(row["TransferDate"]?.ToString(), out DateTime parsedDate))
                {
                    data.TransferDate = parsedDate;
                }

                data.BatchNumber = row["BatchNumber"].ToString();
                data.MicrofilmNumber = row["MicrofilmNumber"].ToString();
                data.RecordSeriesTitle = row["RecordSeriesTitle"].ToString();
                data.AuthorityNumber = row["AuthorityNumber"].ToString();
                data.RecordType = row["RecordType"].ToString();
                data.FolderRef = row["FolderRef"].ToString();
                data.FolderTitle = row["FolderTitle"].ToString();
                data.FolderSecurityGrading = row["FolderSecurityGrading"].ToString();
                data.FolderSensitivityClassification = row["FolderSensitivityClassification"].ToString();

                result.Add(data);
            }

            return result;
        }
    }
}
