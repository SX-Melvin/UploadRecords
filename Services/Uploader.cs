using UploadRecords.Enums;
using UploadRecords.Models;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Uploader
    {
        public int IntervalBetweenFiles { get; set; } = 0;
        public List<BatchFile> ProcessedFiles = [];
        
        public Uploader(int intervalBetweenFiles)
        {
            IntervalBetweenFiles = intervalBetweenFiles;
        }

        public async Task UploadFiles(OTCS otcs, Queue queue) 
        {
            Logger.Information($"Beginning Upload");

            string? ticket = null;

            while(queue.Queues.Count > 0)
            {
                // LOGGING PURPOSE
                var nearest = queue.Queues.OrderBy(x => x.RunAt).FirstOrDefault();
                if (nearest != null)
                {
                    Logger.Information($"The nearest queue is {nearest.RunAt.ToString("MM/dd HH:mm:ss")} - {nearest.File.Path}");
                }
                // LOGGING PURPOSE

                var item = queue.GetScheduled();

                if (item != null)
                {
                    if (item.TotalRun >= queue.MaxRun)
                    {
                        var remarks = $"{item.File.Name} upload is failed {queue.MaxRun} times";
                        item.File.Status = BatchFileStatus.Skipped;
                        item.File.Remarks = remarks;
                        item.File.EndDate = DateTime.Now;
                        item.File.Attempt = item.TotalRun;
                        Audit.Fail(item.File.LogDirectory, $"{remarks} - {item.File.Path}");
                        UpdateProcessedFile(item.File);
                        queue.Queues.Remove(item);
                        continue;
                    }

                    var result = await UploadSingleFile(otcs, queue, item, ticket);

                    if (result == 1)
                    {
                        UpdateProcessedFile(item.File);
                        queue.Queues.Remove(item);
                    }
                }

                Thread.Sleep(IntervalBetweenFiles);
            }

            Logger.Information($"Upload Completed");
        }

        public async Task<int> UploadSingleFile(OTCS otcs, Queue queue, QueueItem item, string? ticket)
        {
            int result = 0;

            if (ticket == null)
            {
                var getTicket = await otcs.GetTicket();
                if (getTicket.Error != null)
                {
                    var remarks = $"Fail to upload file {item.File.Name} due to {getTicket.Error}";
                    item.File.Status = BatchFileStatus.Skipped;
                    item.File.Remarks = remarks;
                    item.File.EndDate = DateTime.Now;
                    item.File.Attempt = item.TotalRun;
                    UpdateProcessedFile(item.File);
                    Audit.Fail(item.File.LogDirectory, $"{remarks} - {item.File.Path}");
                    queue.RegisterFile(item.File);
                    return result;
                }
            }

            var upload = await otcs.CreateFile(item.File.Path, item.File.OTCS.ParentID, ticket);
            if (upload.Error != null)
            {
                var remarks = $"Fail to upload file {item.File.Name} due to {upload.Error}";
                item.File.Status = BatchFileStatus.Skipped;
                item.File.Remarks = remarks;
                item.File.EndDate = DateTime.Now;
                item.File.Attempt = item.TotalRun;
                UpdateProcessedFile(item.File);
                Audit.Fail(item.File.LogDirectory, $"{remarks} - {item.File.Path}");
                queue.RegisterFile(item.File);
                return result;
            }

            Audit.Success(item.File.LogDirectory, $"{item.File.Name} was uploaded with node id {upload.Id} - {item.File.Path}");
            result = 1;

            return result;
        }

        public void UpdateProcessedFile(BatchFile file)
        {
            var processedFile = ProcessedFiles.FirstOrDefault(x => x.Path == file.Path);

            if(processedFile == null)
            {
                ProcessedFiles.Add(file);
                return;
            }

            processedFile.Status = file.Status;
            processedFile.Remarks = file.Remarks;
            processedFile.EndDate = file.EndDate;
            processedFile.Attempt = file.Attempt;
        }
    }
}
