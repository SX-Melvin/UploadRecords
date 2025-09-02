using UploadRecords.Services;
using UploadRecords.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

// TESTING ONLY
// Checksum.GetFileChecksum("C:\\zWork\\UploadRecords\\Batch 1\\0001 Folder\\data\\access\\example3.pdf");
// TESTING ONLY

var logPath = "C:\\zWork\\UploadRecordsLogs";

var newRun = new Scanner("C:\\zWork\\UploadRecords\\Batch 1", logPath);
newRun.Run();

var otcs = new OTCS("admin", "P@ssw0rd", "http://192.168.1.185/otcs/cs.exe/api");
string? ticket = null;

foreach (var item in newRun.validFiles)
{
    var logsPath = Path.Combine(logPath, Path.GetFileName(item.SubBatchFolderPath));

    if (ticket == null) 
    {
        var getTicket = await otcs.GetTicket();
        if(getTicket.Error != null)
        {
            Audit.Fail(logsPath, $"Fail to upload file {item.Name} due to {getTicket.Error} - {item.Path}");
        }
    }

    var upload = await otcs.CreateFile(item.Path, item.OTCS.ParentID, ticket);
    if (upload.Error != null)
    {
        Audit.Fail(logsPath, $"Fail to upload file {item.Name} due to {upload.Error} - {item.Path}");
        continue;
    }

    Audit.Success(logsPath, $"{item.Name} was uploaded with node id {upload.Id} - {item.Path}");
}