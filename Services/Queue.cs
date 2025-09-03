using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Queue
    {
        public List<QueueItem> Queues { get; set; } = [];
        public int MaxRun { get; set; }
        public int IntervalEachRun { get; set; }

        public Queue(int maxRun, int intervalEachRun)
        {
            MaxRun = maxRun;
            IntervalEachRun = intervalEachRun;
        }

        public QueueItem? GetScheduled()
        {
            return Queues.FirstOrDefault(x => DateTime.Now >= x.RunAt);
        }

        public void RegisterFiles(List<ValidFile> files, bool triggerNow = false)
        {
            foreach (var file in files)
            {
                RegisterFile(file, triggerNow);
            }
        }

        public void RegisterFile(ValidFile file, bool triggerNow = false)
        {
            var queue = Queues.FirstOrDefault(x => x.File == file);

            if (queue == null) 
            {
                Queues.Add(new()
                {
                    File = file,
                    RunAt =  DateTime.Now.AddMilliseconds(!triggerNow ? IntervalEachRun : -1),
                    TotalRun = 0
                });
                return;
            }

            if(queue.TotalRun >= MaxRun)
            {
                Audit.Fail(file.LogDirectory, $"{file.Name} upload is failed {MaxRun} times, skipped... - {file.Path}");
                Queues.Remove(queue);
                return;
            }

            queue.TotalRun++;
            queue.RunAt = DateTime.Now.AddMilliseconds(IntervalEachRun);
        }
    }
}
