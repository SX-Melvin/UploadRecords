using UploadRecords.Models;
using UploadRecords.Models.Db;

namespace UploadRecords.Services
{
    public class Csdb
    {
        public DatabaseContext DatabaseContext { get; set; }
        public Csdb(string connStr)
        {
            DatabaseContext = new DatabaseContext(connStr);
        }

        public List<Kuaf> GetKuafsByNames(List<string> names)
        {
            var lowerNames = names.Select(n => n.ToLower()).ToList();
            return DatabaseContext.KUAFs
                .Where(x => lowerNames.Contains(x.Name.ToLower()))
                .ToList();
        }
    }
}
