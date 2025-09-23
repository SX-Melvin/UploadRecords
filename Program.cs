using Microsoft.Extensions.Configuration;
using System.Linq;
using UploadRecords.Models;
using UploadRecords.Services;
using UploadRecords.Utils;

// build configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // needed for console apps
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

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
MailCreds mailCreds = new()
{
    MailAddress = new(Registry.GetRegistryValue("emailaddress"), "Upload Records"),
    MailSecret = Registry.GetRegistryValue("emailkey")
}; // From who are we sending the email report

Logger.Information("DB Connection String " + dbConnectionStr);

Logger.Information("Logs Path: " + logPath);
Logger.Information("Batch Path: " + batchFolder);

Logger.Information("OTCS Username: " + otcsUsername);
Logger.Information("OTCS Secret: " + new string('*', otcsSecret?.Length ?? 0));
Logger.Information("OTCS ApiUrl: " + otcsApiUrl);
Logger.Information("NodeID: " + config["OTCS:NodeID"]);

Logger.Information("Recipients: " + string.Join(",", recipients));
Logger.Information("Email Sender: " + Registry.GetRegistryValue("emailaddress"));
Logger.Information("Email Secret: " + new string('*', Registry.GetRegistryValue("emailkey")?.Length ?? 0));

// Start Logic

var otcs = new OTCS(otcsUsername, otcsSecret, otcsApiUrl);
var csdb = new CSDB(dbConnectionStr);

var scanner = new Scanner(batchFolder, logPath, csdb, otcs);
await scanner.ScanValidFiles();

var queue = new Queue(uploadCount, uploadRetryInterval, scanner.ValidFiles);

var uploader = new Uploader(intervalEachRun);
await uploader.UploadFiles(otcs, queue);

var summarizer = new Summarizer(scanner, [.. scanner.InvalidFiles, .. uploader.ProcessedFiles], recipients);
summarizer.GenerateReport();
summarizer.SendMail();

// End Logic