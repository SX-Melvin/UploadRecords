namespace UploadRecords.Models
{
    public class CategoryConfiguration<T>
    {
        public long ID { get; set; }
        public T Rows { get; set; }
    }
    public class ArchiveCategory
    {
        public string MicrofilmNumber { get; set; }
        public string RecordSeriesTitle { get; set; }
        public string TransferDate { get; set; }
        public string AuthorityNumber { get; set; }
        public string RecordType { get; set; }
    }
    public class _RecordCategory
    {
        public string SecurityClassification { get; set; }
    }
}
