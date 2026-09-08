using Microsoft.Extensions.Configuration;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Configuration
    {
        public static string GetRequiredValue(IConfiguration config, string key)
        {
            var value = config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Required configuration '{key}' is missing or empty.");
            }

            return value;
        }

        public static CategoryConfiguration<ArchiveCategory> GetArchiveCategories(IConfigurationRoot config)
        {
            return new()
            {
                ID = Int64.Parse(GetRequiredValue(config, "Category:Archives:ID")),
                Rows = new()
                {
                    AuthorityNumber = GetRequiredValue(config, "Category:Archives:Rows:AuthorityNumber"),
                    RecordSeriesTitle = GetRequiredValue(config, "Category:Archives:Rows:RecordSeriesTitle"),
                    TransferDate = GetRequiredValue(config, "Category:Archives:Rows:TransferDate"),
                    RecordType = GetRequiredValue(config, "Category:Archives:Rows:RecordType"),
                    MicrofilmNumber = GetRequiredValue(config, "Category:Archives:Rows:MicrofilmNumber"),
                }
            };
        }
        public static List<DivisionConfiguration> GetDivisionPrep(IConfigurationRoot config)
        { 
            var divisions = new List<DivisionConfiguration>();

            var divisionSection = config.GetSection("Divisions");
            foreach (var division in divisionSection.GetChildren())
            {
                var divisionConfig = new DivisionConfiguration
                {
                    Name = division.Key,
                    Preps = division.Get<List<string>>() ?? new List<string>()
                };

                divisions.Add(divisionConfig);
            }

            return divisions;
        }
        public static CategoryConfiguration<RecordCategory> GetRecordCategories(IConfigurationRoot config)
        {
            return new()
            {
                ID = Int64.Parse(GetRequiredValue(config, "Category:_Record:ID")),
                Rows = new()
                {
                    SecurityClassification = GetRequiredValue(config, "Category:_Record:Rows:SecurityClassification"),
                }
            };
        }
    }
}
