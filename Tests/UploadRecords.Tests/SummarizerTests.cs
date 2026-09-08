using UploadRecords.Services;
using Xunit;

namespace UploadRecords.Tests;

[Collection("Workspace")]
public sealed class SummarizerTests(TestWorkspace workspace)
{
    [Theory]
    [InlineData("{\"error\":\"denied\"}")]
    [InlineData("{}")]
    [InlineData("{\"ticket\":\"\"}")]
    public async Task ReportUploadStopsWhenAuthenticationFails(string response)
    {
        using var handler = new FakeOtcsHandler { Respond = _ => response };
        var file = workspace.FileRecord();
        var summarizer = new Summarizer(new() { OTCS = handler.CreateClient(), BatchNumber = "B1", InvalidFiles = [file], Uploader = TestWorkspace.Uploader() }, new() { From = "test@example.invalid", Host = "unused" }, []);
        await summarizer.SendMail();
        Assert.Single(handler.Requests);
        Assert.Equal(0, summarizer.ReportNodeID);
        Assert.Contains("Fail to upload report", File.ReadAllText(Path.Combine(file.LogDirectory, "fail.log")));
    }

    [Fact]
    public async Task FailedReportUploadDoesNotSendEmail()
    {
        using var handler = new FakeOtcsHandler { Respond = r => r.Path == "/v1/auth" ? FakeOtcsHandler.DefaultResponse(r) : "{\"error\":\"upload rejected\"}" };
        var file = workspace.FileRecord();
        var summarizer = new Summarizer(new() { OTCS = handler.CreateClient(), BatchNumber = "B1", InvalidFiles = [file], Uploader = TestWorkspace.Uploader() }, new() { From = "test@example.invalid", Host = "unused" }, []);
        summarizer.ReportPath = file.Path;
        await summarizer.SendMail();
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(0, summarizer.ReportNodeID);
        Assert.Contains("upload rejected", File.ReadAllText(Path.Combine(file.LogDirectory, "fail.log")));
    }
}
