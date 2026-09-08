namespace UploadRecords.Models.API
{
    public class GetNodeSubnodesResponse
    {
        public class GetNodeSubnodesResultDataProperties
        {
            public required string Name {  get; set; }
            public long Id {  get; set; }
            public int Type {  get; set; }
        }

        public class GetNodeSubnodesResultData
        {
            public required GetNodeSubnodesResultDataProperties Properties { get; set; }
        }
        public class GetNodeSubnodesResult
        {
            public required GetNodeSubnodesResultData Data { get; set; }
        }
        public List<GetNodeSubnodesResult> Results { get; set; } = [];
    }
}
