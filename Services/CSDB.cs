using UploadRecords.Models;
using UploadRecords.Models.Db;

namespace UploadRecords.Services
{
    public class CSDB
    {
        public DatabaseContext DatabaseContext { get; set; }
        public CSDB(string connStr) 
        { 
            DatabaseContext = new DatabaseContext(connStr);
        }

        public DTreeCore GetNodeFromParentByName(string nodeName, long parentID)
        {
            return DatabaseContext.DTreeCores.Where(x => x.Name.ToLower() == nodeName.ToLower() && x.ParentID == parentID).FirstOrDefault();
        }
        public List<KUAF> GetKuafsByNames(List<string> names)
        {
            var lowerNames = names.Select(n => n.ToLower()).ToList();
            return DatabaseContext.KUAFs
                .Where(x => lowerNames.Contains(x.Name.ToLower()))
                .ToList();
        }
    }
}
