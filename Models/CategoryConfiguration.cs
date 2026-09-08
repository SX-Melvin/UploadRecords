namespace UploadRecords.Models
{
    public class CategoryConfiguration<T>
    {
        public long ID { get; set; }
        public required T Rows { get; set; }
    }
    public class ArchiveCategory
    {
        public required string MicrofilmNumber { get; set; }
        public required string RecordSeriesTitle { get; set; }
        public required string TransferDate { get; set; }
        public required string AuthorityNumber { get; set; }
        public required string RecordType { get; set; }
    }
    public class RecordCategory
    {
        public required string SecurityClassification { get; set; }
    }
}
