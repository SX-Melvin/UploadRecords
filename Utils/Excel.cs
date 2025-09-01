using ExcelDataReader;
using System;
using System.Data;
using System.Text;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Excel
    {
        public static ControlFile? ReadControlFile(string filePath)
        {
            ControlFile? result = null;
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
                result = new();

                if (DateTime.TryParse(row["TransferDate"]?.ToString(), out DateTime parsedDate))
                {
                    result.TransferDate = parsedDate;
                }

                result.BatchNumber = row["BatchNumber"].ToString();
                result.MicrofilmNumber = row["MicrofilmNumber"].ToString();
                result.RecordSeriesTitle = row["RecordSeriesTitle"].ToString();
                result.AuthorityNumber = row["AuthorityNumber"].ToString();
                result.RecordType = row["RecordType"].ToString();
                result.FolderRef = row["FolderRef"].ToString();
                result.FolderTitle = row["FolderTitle"].ToString();
                result.FolderSecurityGrading = row["FolderSecurityGrading"].ToString();
            }

            return result;
        }
    }
}
