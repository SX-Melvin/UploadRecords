using UploadRecords.Services;
using UploadRecords.Utils;

// TESTING ONLY
// Checksum.GetFileChecksum("C:\\zWork\\UploadRecords\\Batch 1\\0001 Folder\\data\\access\\example3.pdf");
// TESTING ONLY

var logPath = "C:\\zWork\\UploadRecordsLogs"; // Where we store the success and fail logs
var uploadCount = 1 + 2; // Success + Error Retry
//var uploadRetryInterval = 10000; // TESTING
var uploadRetryInterval = 1000 * 60 * 30; // 30 Mins

// Start Logic

var newRun = new Scanner("C:\\zWork\\UploadRecords\\Batch 1", logPath);
newRun.ScanValidFiles();

var otcs = new OTCS("admin", "P@ssw0rd", "http://192.168.1.185/otcs/cs.exe/api");
var queue = new Queue(uploadCount, uploadRetryInterval);
queue.RegisterFiles(newRun.ValidFiles, true);

await Upload.UploadFiles(otcs, queue);