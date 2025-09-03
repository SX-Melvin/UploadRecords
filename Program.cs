using UploadRecords.Models;
using UploadRecords.Services;

var logPath = "C:\\zWork\\UploadRecordsLogs"; // Where we store the success and fail logs
var batchFolder = "C:\\zWork\\UploadRecords\\Batch 1"; // Where the batch folder located
var uploadCount = 1 + 2; // Success + Error Retry
var uploadRetryInterval = 1000 * 60 * 30; // 30 Mins
var intervalEachRun = 0; // How long we wait to upload the next file
var otcsUsername = "admin";
var otcsSecret = "P@ssw0rd";
var otcsApiUrl = "http://192.168.1.185/otcs/cs.exe/api";
List<string> recipients = ["melvinjovano2@gmail.com", "melvin.swiftx@outlook.com"];
MailCreds mailCreds = new()
{
    MailAddress = new("melvin@swiftx.co", "Upload Records"),
    MailSecret = "fLV9Yp8RoefH"
};

// Start Logic

var scanner = new Scanner(batchFolder, logPath);
scanner.ScanValidFiles();

var otcs = new OTCS(otcsUsername, otcsSecret, otcsApiUrl);
var queue = new Queue(uploadCount, uploadRetryInterval, scanner.ValidFiles);

var uploader = new Uploader(intervalEachRun);
await uploader.UploadFiles(otcs, queue);

var summarizer = new Summarizer(scanner, [.. scanner.InvalidFiles, .. uploader.ProcessedFiles], recipients, mailCreds);
summarizer.GenerateReport();
summarizer.SendMail();

// End Logic