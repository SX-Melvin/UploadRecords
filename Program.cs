using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using UploadRecords.Models;
using UploadRecords.Services;
using UploadRecords.Utils;

// build configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // needed for console apps
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var emailFrom = config["Email:From"]; // Email Sender
var emailHost = config["Email:Host"]; // Email Host
var emailPort = Int32.Parse(config["Email:Port"]); // Email Port
var dbConnectionStr = config["Database:ConnectionString"]; // Db Connection
var logPath = config["Audit:Path"]; // Where we store the success and fail logs
var batchFolder = config["Batch:FolderPath"]; // Where the batch folder located
var uploadCount = 1 + 2; // Success + Error Retry
var uploadRetryInterval = 1000 * 60 * 30; // 30 Mins
var intervalEachRun = 0; // How long we wait to upload the next file
var otcsUsername = Registry.GetRegistryValue("otuser"); // OTCS account user
var otcsSecret = Registry.GetRegistryValue("otkey"); // OTCS account pwd
var otcsApiUrl = Registry.GetRegistryValue("otcsapiurl"); // OTCS API url
List<string> recipients = config.GetSection("Batch:Recipients").Get<List<string>>(); // To who are we sending the email report
string controlFileName = "metadata.xlsx"; // Control file name
long batchUploadNodeID = long.Parse(config["Batch:UploadFolderNodeID"]); // Where to upload the files / folder
List<long> functionalAdminIDs = config.GetSection("FunctionalAdminID").Get<List<long>>(); // Functional admin IDs

Logger.Information("DB Connection String " + dbConnectionStr);

Logger.Information("Logs Path: " + logPath);
Logger.Information("Batch Path: " + batchFolder);

Logger.Information("OTCS Username: " + otcsUsername);
Logger.Information("OTCS Secret: " + new string('*', otcsSecret?.Length ?? 0));
Logger.Information("OTCS ApiUrl: " + otcsApiUrl);
Logger.Information("NodeID: " + config["OTCS:NodeID"]);

Logger.Information("Email Host: " + emailHost);
Logger.Information("Email From: " + emailFrom);
Logger.Information("Email Port: " + emailPort);
Logger.Information("Recipients: " + string.Join(", ", recipients));

// Start Logic

var archiveCat = Configuration.GetArchiveCategories(config);
var recordCat = Configuration.GetRecordCategories(config);
var divisions = Configuration.GetDivisionPrep(config);

var otcs = new OTCS(otcsUsername, otcsSecret, otcsApiUrl);
var csdb = new CSDB(dbConnectionStr);
var mailConfig = new MailConfiguration()
{
    Host = emailHost,
    From = emailFrom,
    Port = emailPort
};
var division = Division.GetDivisionDatas(divisions, csdb);
List<BatchFile> allBatchFiles = [];
List<BatchFile> allInvalidBatchFiles = [];
string batchNumber = "";

try
{
    string controlFilePath = Path.Combine(batchFolder, controlFileName);
    var metadatas = Excel.ReadControlFile(controlFilePath);

    // Control file not found
    if (metadatas == null)
    {
        Logger.Error($"Control file not found on {controlFilePath}, skipped...");
        return;
    }

    for (int i = 0; i < metadatas.Count; i++)
    {
        var metadata = metadatas[i];

        var scanner = new Scanner(batchFolder, logPath, csdb, otcs, metadata, division, batchUploadNodeID);
        await scanner.ScanValidFiles();
        
        // Add metadata.xlsx on first loop
        if (i == 0)
        {
            await scanner.AddMetadataFileToValidFiles(controlFilePath);
            batchNumber = scanner.ControlFile.BatchNumber ?? "";
        }

        allBatchFiles.AddRange(scanner.ValidFiles);
        allInvalidBatchFiles.AddRange(scanner.InvalidFiles);
    }

    if(metadatas.Count > 0)
    {
        var queue = new Queue(uploadCount, uploadRetryInterval, allBatchFiles);

        var uploader = new Uploader(intervalEachRun, division, archiveCat, recordCat, functionalAdminIDs);
        await uploader.UploadFiles(otcs, queue);

        var summarizer = new Summarizer(new()
        {
            OTCS = otcs,
            ReportNodeLocationID = long.Parse(config["OTCS:ReportNodeLocationID"]),
            InvalidFiles = allInvalidBatchFiles,
            BatchNumber = batchNumber,
            Uploader = uploader
        }, mailConfig, recipients);
        summarizer.GenerateReport();
        await summarizer.SendMail();
    }
}
catch (Exception ex)
{
    Logger.Error($"An error occured {ex.Message}");
}
// End Logic