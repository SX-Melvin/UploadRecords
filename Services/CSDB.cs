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
    }
}
