using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using UploadRecords.Models;
using UploadRecords.Utils;
using Xunit;

namespace UploadRecords.Tests;

[Collection("Workspace")]
public sealed class UtilityTests(TestWorkspace workspace)
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RequiredConfigurationRejectsAbsentValues(string? value)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["key"] = value }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => Configuration.GetRequiredValue(config, "key"));
        Assert.Contains("key", error.Message);
    }

    [Fact]
    public void CategoryConfigurationAndPayloadPreserveMappingsAndNullMetadata()
    {
        var values = new Dictionary<string, string?>
        {
            ["Category:Archives:ID"] = "11", ["Category:Archives:Rows:AuthorityNumber"] = "authority",
            ["Category:Archives:Rows:MicrofilmNumber"] = "microfilm", ["Category:Archives:Rows:RecordSeriesTitle"] = "title",
            ["Category:Archives:Rows:TransferDate"] = "transfer", ["Category:Archives:Rows:RecordType"] = "type",
            ["Category:_Record:ID"] = "22", ["Category:_Record:Rows:SecurityClassification"] = "security",
            ["Divisions:Finance:0"] = "finance-group"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var archive = Configuration.GetArchiveCategories(config);
        var record = Configuration.GetRecordCategories(config);
        Assert.Equal(11, archive.ID);
        Assert.Equal("title", archive.Rows.RecordSeriesTitle);
        Assert.Equal(22, record.ID);
        Assert.Equal("finance-group", Assert.Single(Assert.Single(Configuration.GetDivisionPrep(config)).Preps));
        var metadata = new ControlFile { AuthorityNumber = "A123" };
        var body = JObject.Parse(Category.ConvertArchiveCategoryToJSON(archive, metadata));
        Assert.Equal("A123", body["authority"]!.Value<string>());
        Assert.Equal(JTokenType.Null, body["microfilm"]!.Type);
        Assert.Equal("Confidential", JObject.Parse(Category.ConvertRecordCategoryToJSON(record, metadata))["security"]!.Value<string>());
        metadata.FolderSecurityGrading = "Public";
        Assert.Equal("Public", JObject.Parse(Category.ConvertRecordCategoryToJSON(record, metadata))["security"]!.Value<string>());
    }

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public void ChecksumMatchesKnownSha256Vectors(string contents, string expected)
    {
        var path = Path.Combine(workspace.NewDirectory(), "input");
        File.WriteAllText(path, contents);
        Assert.Equal(expected, Checksum.GetFromFile(path));
    }

    [Fact]
    public void ManifestSkipsMalformedLinesAndNormalizesPaths()
    {
        var path = Path.Combine(workspace.NewDirectory(), "manifest.txt");
        Assert.Null(Manifest.ReadManifest(path));
        File.WriteAllText(path, "\ninvalid\nabc  reference/access/file.pdf\n");
        var entry = Assert.Single(Manifest.ReadManifest(path)!);
        Assert.Equal("abc", entry.Checksum);
        Assert.Equal(@"reference\access\file.pdf", entry.Path);
    }

    [Fact]
    public void SpreadsheetReadsDatesAndSkipsRowsWithoutBatchNumber()
    {
        var path = Path.Combine(workspace.NewDirectory(), "metadata.xlsx");
        using (var book = new XLWorkbook())
        {
            var sheet = book.AddWorksheet("metadata");
            string[] headers = ["BatchNumber", "TransferDate", "MicrofilmNumber", "RecordSeriesTitle", "AuthorityNumber", "RecordType", "FolderRef", "FolderTitle", "FolderSecurityGrading", "Note1", "Note2", "FolderSensitivityClassification"];
            for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(2, 1).Value = "B1";
            sheet.Cell(2, 2).Value = new DateTime(2025, 1, 15).ToString(CultureInfo.CurrentCulture);
            sheet.Cell(2, 7).Value = "Vol 1";
            sheet.Cell(3, 2).Value = "ignored";
            sheet.Cell(4, 1).Value = "B2";
            sheet.Cell(4, 2).Value = "invalid-date";
            book.SaveAs(path);
        }
        var records = Excel.ReadControlFile(path)!;
        Assert.Equal(2, records.Count);
        Assert.Equal(new DateTime(2025, 1, 15), records[0].TransferDate);
        Assert.Equal("Vol 1", records[0].FolderRef);
        Assert.Null(records[1].TransferDate);
    }

    [Fact]
    public void ReportContainsStatusAndRemarks()
    {
        var file = workspace.FileRecord();
        file.Remarks = "Checksum mismatch";
        file.Status = Enums.BatchFileStatus.Failed;
        file.EndDate = file.StartDate.AddSeconds(3);
        var path = Path.Combine(workspace.NewDirectory(), "report.xlsx");
        Excel.GenerateReport(path, [file]);
        using var book = new XLWorkbook(path);
        Assert.Equal(file.Name, book.Worksheet(1).Cell(2, 5).GetString());
        Assert.Equal("Failed", book.Worksheet(1).Cell(2, 8).GetString());
        Assert.Equal(file.Remarks, book.Worksheet(1).Cell(2, 9).GetString());
    }

    [Fact]
    public void DatabaseMappingRetainsExistingTableNames()
    {
        using var context = new DatabaseContext("Server=unused;Database=unused;Integrated Security=true");
        Assert.Contains("[Kuaf]", context.KUAFs.ToQueryString());
        Assert.Contains("[DTreeCore]", context.DTreeCores.ToQueryString());
    }

    [Fact]
    public void MissingRegistryCredentialProducesClearFailure()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformNotSupportedException>(() => Registry.GetRegistryValue("missing"));
            return;
        }
        var error = Assert.Throws<InvalidOperationException>(() => Registry.GetRegistryValue("missing", @"UploadRecords.Tests\" + Guid.NewGuid().ToString("N")));
        Assert.Contains("missing", error.Message);
    }
}
