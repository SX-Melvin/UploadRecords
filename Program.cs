using UploadRecords.Models;
using UploadRecords.Services;
using UploadRecords.Utils;
using Microsoft.Extensions.Configuration;

// build configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // needed for console apps
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var logPath = config["Audit:Path"]; // Where we store the success and fail logs
var batchFolder = config["Batch:FolderPath"]; // Where the batch folder located
var uploadCount = 1 + 2; // Success + Error Retry
var uploadRetryInterval = 1000 * 60 * 30; // 30 Mins
var intervalEachRun = 0; // How long we wait to upload the next file
var otcsUsername = Registry.GetRegistryValue("otuser"); // OTCS account user
var otcsSecret = Registry.GetRegistryValue("otkey"); // OTCS account pwd
var otcsApiUrl = Registry.GetRegistryValue("otcsapiurl"); // OTCS API url
List<string> recipients = ["melvinjovano2@gmail.com", "melvin.swiftx@outlook.com"]; // To who are we sending the email report
MailCreds mailCreds = new()
{
    MailAddress = new(Registry.GetRegistryValue("emailaddress"), "Upload Records"),
    MailSecret = Registry.GetRegistryValue("emailkey")
}; // From who are we sending the email report

// Start Logic

var otcs = new OTCS(otcsUsername, otcsSecret, otcsApiUrl);

var scanner = new Scanner(batchFolder, logPath, int.Parse(config["OTCS:NodeID"]), otcs);
await scanner.ScanValidFiles();

var queue = new Queue(uploadCount, uploadRetryInterval, scanner.ValidFiles);

var uploader = new Uploader(intervalEachRun);
await uploader.UploadFiles(otcs, queue);

var summarizer = new Summarizer(scanner, [.. scanner.InvalidFiles, .. uploader.ProcessedFiles], recipients, mailCreds);
summarizer.GenerateReport();
summarizer.SendMail();

// End Logic