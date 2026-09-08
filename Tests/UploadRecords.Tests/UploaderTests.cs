using UploadRecords.Enums;
using UploadRecords.Models;
using Xunit;
using Queue = UploadRecords.Services.Queue;

namespace UploadRecords.Tests;

[Collection("Workspace")]
public sealed class UploaderTests(TestWorkspace workspace)
{
    [Fact]
    public async Task SuccessfulUploadAppliesPermissionsAndCompletesQueue()
    {
        using var handler = new FakeOtcsHandler();
        var file = workspace.FileRecord();
        file.PermissionInfo.Division.UpdateBasedOnMetadata = true;
        file.PermissionInfo.Division.NoRepPermission = true;
        var uploader = TestWorkspace.Uploader([
            new() { Name = "Finance", PrepDatas = [new() { Name = "Finance", ID = 10 }] },
            new() { Name = "Other", PrepDatas = [new() { Name = "Other", ID = 20 }] }
        ]);
        var queue = new Queue(3, 0, [file]);
        await uploader.UploadFiles(handler.CreateClient(), queue);
        Assert.Empty(queue.Queues);
        Assert.Equal(BatchFileStatus.Completed, Assert.Single(uploader.ProcessedFiles).Status);
        Assert.Contains(handler.Requests, r => r.Path == "/v2/nodes/42/permissions/custom/20" && r.Method == "DELETE");
        Assert.Contains(handler.Requests, r => r.Path == "/v2/nodes/42/permissions/custom/10" && r.Method == "DELETE");
        Assert.Equal(2, handler.Requests.Count(r => r.Path == "/v1/nodes/42/categories"));
        Assert.True(File.Exists(Path.Combine(file.LogDirectory, "success.log")));
    }

    [Theory]
    [InlineData("{\"error\":\"bad credentials\"}")]
    [InlineData("{}")]
    public async Task MissingOrFailedTicketRetriesOnlyUpToLimit(string response)
    {
        using var handler = new FakeOtcsHandler { Respond = _ => response };
        var file = workspace.FileRecord();
        var uploader = TestWorkspace.Uploader();
        var queue = new Queue(2, 0, [file]);
        await uploader.UploadFiles(handler.CreateClient(), queue);
        Assert.Empty(queue.Queues);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(2, file.Attempt);
        Assert.Equal(BatchFileStatus.Skipped, Assert.Single(uploader.ProcessedFiles).Status);
    }

    [Fact]
    public async Task FailedUploadIsRetriedWithoutReauthenticating()
    {
        using var handler = new FakeOtcsHandler { Respond = r => r.Path == "/v1/auth" ? FakeOtcsHandler.DefaultResponse(r) : "{\"error\":\"upload failed\"}" };
        var file = workspace.FileRecord();
        var uploader = TestWorkspace.Uploader();
        var queue = new Queue(2, 0, [file]);
        await uploader.UploadFiles(handler.CreateClient(), queue);
        Assert.Single(handler.Requests, r => r.Path == "/v1/auth");
        Assert.Equal(2, handler.Requests.Count(r => r.Path == "/v1/nodes"));
        Assert.Equal(BatchFileStatus.Skipped, file.Status);
        Assert.Empty(queue.Queues);
    }

    [Fact]
    public async Task ExhaustedQueueDoesNotContactServer()
    {
        using var handler = new FakeOtcsHandler();
        var file = workspace.FileRecord();
        var queue = new Queue(0, 0, [file]);
        var uploader = TestWorkspace.Uploader();
        await uploader.UploadFiles(handler.CreateClient(), queue);
        Assert.Empty(handler.Requests);
        Assert.Empty(queue.Queues);
        Assert.Equal(BatchFileStatus.Skipped, file.Status);
    }

    [Fact]
    public void ProcessedFilesAreUpdatedByPathWithoutDuplicates()
    {
        var file = workspace.FileRecord();
        var uploader = TestWorkspace.Uploader();
        uploader.UpdateProcessedFile(file);
        var replacement = workspace.FileRecord();
        replacement.Path = file.Path;
        replacement.Status = BatchFileStatus.Completed;
        replacement.Attempt = 3;
        replacement.Remarks = "complete";
        replacement.EndDate = DateTime.Now;
        uploader.UpdateProcessedFile(replacement);
        Assert.Same(file, Assert.Single(uploader.ProcessedFiles));
        Assert.Equal(replacement.Status, file.Status);
        Assert.Equal(3, file.Attempt);
        Assert.Equal("complete", file.Remarks);
        Assert.Equal(replacement.EndDate, file.EndDate);
    }

    [Fact]
    public void QueueSchedulesRetriesAndPreservesOneEntryPerFile()
    {
        var file = workspace.FileRecord();
        var queue = new Queue(3, 60000, [file]);
        Assert.NotNull(queue.GetScheduled());
        queue.RegisterFile(file);
        Assert.Null(queue.GetScheduled());
        Assert.Equal(1, Assert.Single(queue.Queues).TotalRun);
    }
}
