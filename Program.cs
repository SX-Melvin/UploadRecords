using UploadRecords.Services;

// TESTING ONLY
// Checksum.GetFileChecksum("C:\\zWork\\UploadRecords\\Batch 1\\0001 Folder\\data\\access\\example3.pdf");
// TESTING ONLY

var logPath = "C:\\zWork\\UploadRecordsLogs"; // Where we store the success and fail logs
var uploadCount = 1 + 2; // Success + Error Retry
//var uploadRetryInterval = 10000; // TESTING
var uploadRetryInterval = 1000 * 60 * 30; // 30 Mins
var intervalEachRun = 0;

// Start Logic

var scanner = new Scanner("C:\\zWork\\UploadRecords\\Batch 1", logPath);
scanner.ScanValidFiles();

var otcs = new OTCS("admin", "P@ssw0rd", "http://192.168.1.185/otcs/cs.exe/api");
var queue = new Queue(uploadCount, uploadRetryInterval);
queue.RegisterFiles(scanner.ValidFiles, true);

var uploader = new Uploader(intervalEachRun);
await uploader.UploadFiles(otcs, queue);

var invalidFiles = scanner.InvalidFiles;
var validFiles = uploader.ProcessedFiles;

var summarizer = new Summarizer();