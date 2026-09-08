using UploadRecords.Enums;
using UploadRecords.Models;
using UploadRecords.Services;
using UploadRecords.Utils;
using Xunit;

namespace UploadRecords.Tests;

[Collection("Workspace")]
public sealed class ScannerTests(TestWorkspace workspace)
{
    [Fact]
    public async Task ScanSeparatesValidFilesAndAllValidationFailures()
    {
        var root = workspace.NewDirectory();
        var sub = Path.Combine(root, "batch");
        var access = Path.Combine(sub, "reference", "access");
        Directory.CreateDirectory(access);
        foreach (var name in new[] { "valid.pdf", "image.tif", "image.tiff", "invalid.txt", "missing.pdf", "mismatch.pdf" })
            File.WriteAllText(Path.Combine(access, name), "abc");
        File.WriteAllText(Path.Combine(access, "empty.pdf"), "");
        var hash = Checksum.GetFromFile(Path.Combine(access, "valid.pdf"));
        File.WriteAllText(Path.Combine(sub, "manifest-sha256.txt"), $"{hash}  reference/access/valid.pdf\n{hash}  reference/access/image.tif\n{hash}  reference/access/image.tiff\nwrong  reference/access/mismatch.pdf");
        using var handler = new FakeOtcsHandler();
        var scanner = new Scanner(root, workspace.NewDirectory(), handler.CreateClient(), new() { FolderRef = "batch", Note2 = "Finance" }, [new() { Name = "Finance" }], 7, [99]);
        await scanner.ScanValidFiles();
        Assert.Equal(4, scanner.ValidFiles.Count);
        Assert.Equal(4, scanner.InvalidFiles.Count);
        Assert.All(scanner.InvalidFiles, file => Assert.Equal(BatchFileStatus.Failed, file.Status));
        Assert.Contains(scanner.InvalidFiles, f => f.Remarks!.Contains("no valid extension"));
        Assert.Contains(scanner.InvalidFiles, f => f.Remarks!.Contains("is empty"));
        Assert.Contains(scanner.InvalidFiles, f => f.Remarks!.Contains("not found"));
        Assert.Contains(scanner.InvalidFiles, f => f.Remarks!.Contains("mismatch"));
        var pdf = Assert.Single(scanner.ValidFiles, f => f.Name == "valid.pdf");
        Assert.Equal(hash, pdf.Checksum);
        Assert.True(pdf.PermissionInfo.Division.UpdateBasedOnMetadata);
        Assert.True(scanner.Divisions[0].UsedInNote2);
        Assert.True(Assert.Single(scanner.ValidFiles, f => f.Name == "image.tif").PermissionInfo.Division.NoRepPermission);
        Assert.True(File.Exists(Path.Combine(scanner.LogPath, "batch", "fail.log")));
        await scanner.AddMetadataFileToValidFiles(Path.Combine(sub, "manifest-sha256.txt"));
        Assert.Null(scanner.ValidFiles[^1].SubBatchFolderPath);
        Assert.Equal(7, scanner.ValidFiles[^1].OTCS.ParentID);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Vol 1")]
    [InlineData("malformed")]
    public async Task MissingFolderIsSkippedWithoutNetworkCalls(string? folderRef)
    {
        using var handler = new FakeOtcsHandler();
        var scanner = new Scanner(workspace.NewDirectory(), workspace.NewDirectory(), handler.CreateClient(), new() { FolderRef = folderRef }, [], 1, []);
        await scanner.ScanValidFiles();
        Assert.Empty(scanner.ValidFiles);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task PrefixedFolderIsResolvedButMissingManifestIsSkipped()
    {
        var root = workspace.NewDirectory();
        Directory.CreateDirectory(Path.Combine(root, "0001_Vol 1"));
        using var handler = new FakeOtcsHandler();
        var scanner = new Scanner(root, workspace.NewDirectory(), handler.CreateClient(), new() { FolderRef = "Vol 1" }, [], 1, []);
        await scanner.ScanValidFiles();
        Assert.Equal("0001_Vol 1", scanner.ControlFile.FolderRef);
        Assert.NotEmpty(scanner.ControlFile.FolderPath);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task AuthenticationFailureStopsScanning()
    {
        var root = workspace.NewDirectory();
        var sub = Path.Combine(root, "batch");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "manifest-sha256.txt"), "");
        using var handler = new FakeOtcsHandler { Respond = _ => "{\"error\":\"authentication failed\"}" };
        var scanner = new Scanner(root, workspace.NewDirectory(), handler.CreateClient(), new() { FolderRef = "batch" }, [], 1, []);
        await scanner.ScanValidFiles();
        Assert.Empty(scanner.ValidFiles);
        Assert.Single(handler.Requests);
    }
}
