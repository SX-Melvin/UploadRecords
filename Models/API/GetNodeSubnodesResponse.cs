namespace UploadRecords.Models.API
{
    public class GetNodeSubnodesResponse
    {
        public class GetNodeSubnodesResultDataProperties
        {
            public string Name {  get; set; }
            public long Id {  get; set; }
            public int Type {  get; set; }
        }

        public class GetNodeSubnodesResultData
        {
            public GetNodeSubnodesResultDataProperties Properties { get; set; }
        }
        public class GetNodeSubnodesResult
        {
            public GetNodeSubnodesResultData Data { get; set; }
        }
        public List<GetNodeSubnodesResult> Results { get; set; }
    }
}
