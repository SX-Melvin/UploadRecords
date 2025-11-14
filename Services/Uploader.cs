using System.Net;
using UploadRecords.Enums;
using UploadRecords.Models;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Uploader
    {
        public int IntervalBetweenFiles { get; set; } = 0;
        public List<BatchFile> ProcessedFiles = [];
        public CategoryConfiguration<ArchiveCategory> ArchiveCategory;
        public CategoryConfiguration<_RecordCategory> RecordCategory;
        public List<DivisionData> Division;
        
        public Uploader(int intervalBetweenFiles, List<DivisionData> division, CategoryConfiguration<ArchiveCategory> archiveCategory, CategoryConfiguration<_RecordCategory> recordCategory)
        {
            IntervalBetweenFiles = intervalBetweenFiles;
            ArchiveCategory = archiveCategory;
            RecordCategory = recordCategory;
            Division = division;
        }

        public async Task UploadFiles(OTCS otcs, Queue queue) 
        {
            Logger.Information($"Beginning Upload");

            string? ticket = null;

            while(queue.Queues.Count > 0)
            {
                // LOGGING PURPOSE
                //var nearest = queue.Queues.OrderBy(x => x.RunAt).FirstOrDefault();
                //if (nearest != null)
                //{
                //    Logger.Information($"The nearest queue is {nearest.RunAt.ToString("MM/dd HH:mm:ss")} - {nearest.File.Path}");
                //}
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
                        Audit.Fail(item.File.LogDirectory, $"{remarks} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                        UpdateProcessedFile(item.File);
                        queue.Queues.Remove(item);
                        continue;
                    }

                    // Renew The Ticket
                    if(ticket == null)
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
                            Audit.Fail(item.File.LogDirectory, $"{remarks} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                            queue.RegisterFile(item.File);
                            continue;
                        }

                        ticket = getTicket.Ticket;
                    }

                    if (ticket != null)
                    {
                        var result = await UploadSingleFile(otcs, queue, item, ticket);

                        if (result == 1)
                        {
                            UpdateProcessedFile(item.File);
                            queue.Queues.Remove(item);
                        }
                        else if (result == 2)
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
                                Audit.Fail(item.File.LogDirectory, $"{remarks} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                                queue.RegisterFile(item.File);
                                continue;
                            }

                            ticket = getTicket.Ticket;
                        }
                    }
                    else
                    {
                        var remarks = $"Fail to upload file {item.File.Name} due to ticket is empty";
                        item.File.Status = BatchFileStatus.Skipped;
                        item.File.Remarks = remarks;
                        item.File.EndDate = DateTime.Now;
                        item.File.Attempt = item.TotalRun;
                        UpdateProcessedFile(item.File);
                        Audit.Fail(item.File.LogDirectory, $"{remarks} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                        queue.RegisterFile(item.File);
                    }

                }

                Thread.Sleep(IntervalBetweenFiles);
            }

            Logger.Information($"Upload Completed");
        }

        public async Task<int> UploadSingleFile(OTCS otcs, Queue queue, QueueItem item, string ticket)
        {
            int result = 0;

            try
            {
                var upload = await otcs.CreateFile(item.File.Path, item.File.OTCS.ParentID, ticket);
                if (upload.Error != null)
                {
                    var remarks = $"Fail to upload file {item.File.Name} due to {upload.Error}";
                    item.File.Status = BatchFileStatus.Skipped;
                    item.File.Remarks = remarks;
                    item.File.EndDate = DateTime.Now;
                    item.File.Attempt = item.TotalRun;
                    UpdateProcessedFile(item.File);
                    Audit.Fail(item.File.LogDirectory, $"{remarks} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                    queue.RegisterFile(item.File);
                    return result;
                }

                Audit.Success(item.File.LogDirectory, $"{item.File.Name} was uploaded with node id {upload.Id} - {Common.ListAncestors(item.File.OTCS.Ancestors)}");
                item.File.EndDate = DateTime.Now;
                item.File.Status = BatchFileStatus.Completed;
                UpdateProcessedFile(item.File);

                // Update File Categories
                await otcs.ApplyCategoryOnNode(upload.Id, Category.ConvertRecordCategoryToJSON(RecordCategory, item.File.ControlFile), RecordCategory.ID, ticket);
                await otcs.ApplyCategoryOnNode(upload.Id, Category.ConvertArchiveCategoryToJSON(ArchiveCategory, item.File.ControlFile), ArchiveCategory.ID, ticket);

                // Adjust divisions access
                if(item.File.PermissionInfo.Division.UpdateBasedOnMetadata)
                {
                    Logger.Information($"Updating Permissions Based On Divisions");
                    var divisions = Division.Where(x => !string.Equals(x.Name, item.File.ControlFile.Note2, StringComparison.OrdinalIgnoreCase));
                    foreach (var prep in divisions)
                    {
                        foreach (var data in prep.PrepDatas)
                        {
                            await otcs.DeleteNodePermission(upload.Id, data.ID, ticket);
                        }
                    }
                }

                Logger.Information($"Updating Admin Permission To Full Control");
                await otcs.UpdateNodePermissionBulk(upload.Id, [new() {
                    Permissions = ["see", "see_contents", "modify", "edit_attributes", "add_items", "reserve", "add_major_version", "delete_versions", "delete", "edit_permissions"],
                    RightID = 1000 // Functional Admin
                }], ticket);

                Logger.Information($"Updating Public Access Permission");
                await otcs.DeleteNodePublicPermission(upload.Id, ticket);

                Logger.Information($"Updating Owner Permission");
                await otcs.UpdateNodeOwnerPermission(upload.Id, ["see", "see_contents"], ticket);

                Logger.Information($"Removing Business Adminstrators Permission");
                await otcs.DeleteNodePermission(upload.Id, 2001, ticket);
                
                Logger.Information($"Removing Owner Group Permission");
                await otcs.DeleteNodeOwnerGroupPermission(upload.Id, ticket);

                Audit.Success(item.File.LogDirectory, $"{item.File.Name} categories was updated - {Common.ListAncestors(item.File.OTCS.Ancestors)}");

                result = 1;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                result = 2;
            }

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
