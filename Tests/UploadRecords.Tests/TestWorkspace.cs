using System.Net;
using System.Text;
using Newtonsoft.Json;
using UploadRecords.Models;
using UploadRecords.Services;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace UploadRecords.Tests;

[CollectionDefinition("Workspace")]
public sealed class WorkspaceCollection : ICollectionFixture<TestWorkspace>;

public sealed class TestWorkspace : IDisposable
{
    private readonly string originalDirectory = Directory.GetCurrentDirectory();
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "UploadRecords.Tests", Guid.NewGuid().ToString("N"));

    public TestWorkspace()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "appsettings.json"), JsonConvert.SerializeObject(new { Logs = new { Path = Path.Combine(Root, "logs", "tests.log") } }));
        Directory.SetCurrentDirectory(Root);
    }

    public string NewDirectory()
    {
        var path = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public BatchFile FileRecord(string name = "record.pdf")
    {
        var dir = NewDirectory();
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, "abc");
        return new BatchFile
        {
            Name = name, Path = path, LogDirectory = dir, BatchFolderPath = dir,
            SubBatchFolderPath = dir, ControlFile = new() { Note2 = "Finance" },
            OTCS = new() { ParentID = 10, Ancestors = [] },
            PermissionInfo = new() { Division = new() }, StartDate = DateTime.Now
        };
    }

    public static CategoryConfiguration<ArchiveCategory> Archive => new()
    {
        ID = 1, Rows = new() { MicrofilmNumber = "microfilm", AuthorityNumber = "authority", RecordSeriesTitle = "title", TransferDate = "transfer", RecordType = "type" }
    };
    public static CategoryConfiguration<RecordCategory> Record => new() { ID = 2, Rows = new() { SecurityClassification = "security" } };
    public static Uploader Uploader(List<DivisionData>? divisions = null) => new(0, divisions ?? [], Archive, Record, [99]);

    public void Dispose()
    {
        Serilog.Log.CloseAndFlush();
        Directory.SetCurrentDirectory(originalDirectory);
        Directory.Delete(Root, recursive: true);
    }
}

public sealed record RecordedRequest(string Method, string Path, string? Ticket, string Body);

public sealed class FakeOtcsHandler : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];
    public Func<RecordedRequest, string> Respond { get; set; } = DefaultResponse;
    public Otcs CreateClient() => new("test-user", "test-value", "https://otcs.invalid/", this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var recorded = new RecordedRequest(request.Method.Method, request.RequestUri!.PathAndQuery,
            request.Headers.TryGetValues("otcsticket", out var tickets) ? tickets.Single() : null,
            request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        Requests.Add(recorded);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Respond(recorded), Encoding.UTF8, "application/json") };
    }

    public static string DefaultResponse(RecordedRequest request)
    {
        if (request.Path == "/v1/auth") return "{\"ticket\":\"test-ticket\"}";
        if (request.Path.EndsWith("/ancestors", StringComparison.Ordinal)) return "{\"ancestors\":[{\"id\":42,\"name\":\"batch\",\"parent_id\":7}]}";
        return "{\"id\":42}";
    }
}
