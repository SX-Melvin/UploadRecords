using UploadRecords.Models;
using UploadRecords.Services;

namespace UploadRecords.Utils
{
    public static class Upload
    {
        public static int IntervalBetweenFiles { get; set; } = 2000;
        public static async Task UploadFiles(OTCS otcs, Queue queue) 
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
                    var result = await UploadSingleFile(otcs, queue, item.File, ticket);

                    if (result == 1)
                    {
                        queue.Queues.Remove(item);
                    }
                }

                Thread.Sleep(IntervalBetweenFiles);
            }

            Logger.Information($"Upload Completed");
        }

        public static async Task<int> UploadSingleFile(OTCS otcs, Queue queue, ValidFile item, string? ticket)
        {
            int result = 0;

            if (ticket == null)
            {
                var getTicket = await otcs.GetTicket();
                if (getTicket.Error != null)
                {
                    Audit.Fail(item.LogDirectory, $"Fail to upload file {item.Name} due to {getTicket.Error} - {item.Path}");
                    queue.RegisterFile(item);
                    return result;
                }
            }

            var upload = await otcs.CreateFile(item.Path, item.OTCS.ParentID, ticket);
            if (upload.Error != null)
            {
                Audit.Fail(item.LogDirectory, $"Fail to upload file {item.Name} due to {upload.Error} - {item.Path}");
                queue.RegisterFile(item);
                return result;
            }

            Audit.Success(item.LogDirectory, $"{item.Name} was uploaded with node id {upload.Id} - {item.Path}");
            result = 1;

            return result;
        }
    }
}
