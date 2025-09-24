using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Category
    {
        public static string ConvertArchiveCategoryToJSON(CategoryConfiguration<ArchiveCategory> cat, ControlFile controlFile)
        {
            var body = new Dictionary<string, object>
            {
                ["category_id"] = cat.ID,
                [cat.Rows.MicrofilmNumber] = controlFile.MicrofilmNumber,
                [cat.Rows.AuthorityNumber] = controlFile.AuthorityNumber,
                [cat.Rows.TransferDate] = controlFile.TransferDate,
                [cat.Rows.RecordSeriesTitle] = controlFile.RecordSeriesTitle,
                [cat.Rows.RecordType] = controlFile.RecordType,
            };

            return JsonConvert.SerializeObject(body);
        }
        public static string ConvertRecordCategoryToJSON(CategoryConfiguration<_RecordCategory> cat, ControlFile controlFile)
        {
            var body = new Dictionary<string, object>
            {
                ["category_id"] = cat.ID,
                [cat.Rows.SecurityClassification] = controlFile.FolderSecurityGrading,
            };

            return JsonConvert.SerializeObject(body);
        }
    }
}
