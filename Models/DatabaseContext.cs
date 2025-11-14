using Microsoft.EntityFrameworkCore;
using UploadRecords.Models.Db;

namespace UploadRecords.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<DTreeCore> DTreeCores { get; set; }
        public DbSet<KUAF> KUAFs { get; set; }
        public string ConnectionString { get; set; }

        public DatabaseContext(string connectionStr) 
        {
            ConnectionString = connectionStr;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(ConnectionString);
        }
    }
}
