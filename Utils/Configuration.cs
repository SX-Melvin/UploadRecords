using Microsoft.Extensions.Configuration;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Configuration
    {
        public static CategoryConfiguration<ArchiveCategory> GetArchiveCategories(IConfigurationRoot config)
        {
            return new()
            {
                ID = Int64.Parse(config["Category:Archives:ID"]),
                Rows = new()
                {
                    AuthorityNumber = config["Category:Archives:Rows:AuthorityNumber"],
                    RecordSeriesTitle = config["Category:Archives:Rows:RecordSeriesTitle"],
                    TransferDate = config["Category:Archives:Rows:TransferDate"],
                    RecordType = config["Category:Archives:Rows:RecordType"],
                    MicrofilmNumber = config["Category:Archives:Rows:MicrofilmNumber"],
                }
            };
        }
        public static CategoryConfiguration<_RecordCategory> GetRecordCategories(IConfigurationRoot config)
        {
            return new()
            {
                ID = Int64.Parse(config["Category:_Record:ID"]),
                Rows = new()
                {
                    SecurityClassification = config["Category:_Record:Rows:SecurityClassification"],
                }
            };
        }
    }
}
