using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Configs;
using UploadRecords.Enums;
using UploadRecords.Models;
using UploadRecords.Models.API;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Scanner
    {
        public string FolderPath;
        public string LogPath;
        public string ControlFileName = "metadata.xlsx";
        public string ManifestFileName = "manifest-sha256.txt";
        public ControlFile ControlFile;
        public List<string> FoldersContainsFile = ["master", "access"];
        public List<string> ValidFileExtensions = [".tiff", ".pdf"];
        public List<BatchFile> InvalidFiles = [];
        public List<BatchFile> ValidFiles = [];
        public OTCS OTCS;
        public CSDB CSDB;

        public Scanner(string batchPath, string logPath, CSDB csdb, OTCS otcs)
        {
            FolderPath = batchPath;
            LogPath = logPath;
            OTCS = otcs;
            CSDB = csdb;
        }

        public async Task ScanValidFiles()
        {
            try
            {
                Logger.Information($"Beginning Scanning");

                // Loop each batch folder directories
                foreach (var subBatchFolder in Directory.GetDirectories(FolderPath)) // subBatchFolder = CF-0001
                {
                    Logger.Information($"Processing {subBatchFolder}");

                    var batchFolderName = Path.GetFileName(subBatchFolder);
                    string manifestFilePath = Path.Combine(subBatchFolder, ManifestFileName);
                    var manifest = CSV.ReadManifest(manifestFilePath);
                    var logsPath = Path.Combine(LogPath, Path.GetFileName(subBatchFolder));

                    // Manifest not found
                    if (manifest == null)
                    {
                        Logger.Error($"Manifest file not found on {subBatchFolder}, skipped...");
                        continue;
                    }

                    foreach (var fileRefFolder in Directory.GetDirectories(subBatchFolder))
                    {
                        var fileRefFolderName = Path.GetFileName(fileRefFolder);
                        string controlFilePath = Path.Combine(fileRefFolder, "master", ControlFileName);
                        var metadata = Excel.ReadControlFile(controlFilePath);

                        // Control file not found
                        if (metadata == null)
                        {
                            Logger.Error("Control file not found, skipped...");
                            return;
                        }

                        ControlFile = metadata;

                        // Create all the necesarry folders
                        var getTicket = await OTCS.GetTicket();
                        if (getTicket.Error != null)
                        {
                            Logger.Error("Process aborted due to: " + getTicket.Error);
                            return;
                        }

                        var nodeId = await Common.CreateFolderIfNotExist(CSDB, OTCS, ControlFile.FolderPath);
                        var getAncestors = await OTCS.GetNodeAncestors(nodeId, getTicket.Ticket!);
                        var ancestors = getAncestors.Ancestors;

                        var createFolder = await OTCS.CreateFolder(Path.GetFileName(FolderPath), nodeId, getTicket.Ticket!);

                        // Failed to create batch folder
                        if (createFolder.Error != null)
                        {
                            Logger.Error("Process aborted due to: " + createFolder.Error);
                            return;
                        }

                        var createBatchFolder = await OTCS.CreateFolder(batchFolderName, createFolder.Id, getTicket.Ticket!);

                        // Failed to create batch folder
                        if (createBatchFolder.Error != null)
                        {
                            Logger.Error("Process aborted due to: " + createBatchFolder.Error);
                            continue;
                        }

                        var createFileRefFolder = await OTCS.CreateFolder(fileRefFolderName, createBatchFolder.Id, getTicket.Ticket!);

                        // Failed to create batch folder
                        if (createFileRefFolder.Error != null)
                        {
                            Logger.Error("Process aborted due to: " + createFileRefFolder.Error);
                            continue;
                        }

                        ancestors.AddRange([
                            new()
                            {
                                Id = createFileRefFolder.Id,
                                Name = fileRefFolderName,
                                ParentID = createBatchFolder.Id,
                                Type = 0
                            },
                            new()
                            {
                                Id = createBatchFolder.Id,
                                Name = batchFolderName,
                                ParentID = createFolder.Id,
                                Type = 0
                            }
                        ]);

                        foreach (var folderFile in FoldersContainsFile)
                        {
                            var filesPath = Path.Combine(fileRefFolder, folderFile);

                            if(Path.Exists(filesPath))
                            {
                                var createFileFolder = await OTCS.CreateFolder(folderFile, createFileRefFolder.Id, getTicket.Ticket!);

                                // Failed to create batch folder
                                if (createFileFolder.Error != null)
                                {
                                    Logger.Error("Process aborted due to: " + createFileFolder.Error);
                                    continue;
                                }

                                ancestors.Add(new()
                                {
                                    Id = createFileFolder.Id,
                                    Name = folderFile,
                                    ParentID = createFileRefFolder.Id,
                                    Type = 0
                                });

                                foreach (var file in Directory.GetFiles(filesPath))
                                {
                                    var fileInfo = new FileInfo(file);

                                    var batchFile = new BatchFile()
                                    {
                                        ControlFile = ControlFile,
                                        Path = file,
                                        LogDirectory = logsPath,
                                        Name = Path.GetFileName(file),
                                        StartDate = DateTime.Now,
                                        Attempt = 1,
                                        SizeInKB = (double)fileInfo.Length / 1024,
                                        OTCS = new() { ParentID = createFileFolder.Id, Ancestors = ancestors },
                                        BatchFolderPath = filesPath,
                                        SubBatchFolderPath = subBatchFolder
                                    };

                                    Logger.Information($"Processing {file}");
                                    var fileName = Path.GetFileName(file);

                                    // Check file extensions
                                    var ext = Path.GetExtension(file).ToLowerInvariant();
                                    if (!ValidFileExtensions.Contains(ext))
                                    {
                                        var remarks = $"File {fileName} has no valid extension";
                                        batchFile.Status = BatchFileStatus.Failed;
                                        batchFile.Remarks = remarks;
                                        batchFile.EndDate = DateTime.Now;
                                        InvalidFiles.Add(batchFile);
                                        Audit.Fail(logsPath, $"{remarks} - {Common.ListAncestors(batchFile.OTCS.Ancestors)}");
                                        continue;
                                    }

                                    if (fileInfo.Length == 0)
                                    {
                                        var remarks = $"File {fileName} is empty";
                                        batchFile.Status = BatchFileStatus.Failed;
                                        batchFile.Remarks = remarks;
                                        batchFile.EndDate = DateTime.Now;
                                        InvalidFiles.Add(batchFile);
                                        Audit.Fail(logsPath, $"{remarks} - {Common.ListAncestors(batchFile.OTCS.Ancestors)}");
                                        continue;
                                    }

                                    var checksum = Checksum.GetFromFile(file);
                                    var parts = file.Split(Path.DirectorySeparatorChar);
                                    var filePathChecksum = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(Math.Max(0, parts.Length - 3)));

                                    var findChecksumByPath = manifest.FirstOrDefault(x => x.Path == filePathChecksum);

                                    if (findChecksumByPath == null)
                                    {
                                        var remarks = $"File {fileName} checksum's not found in manifest file";
                                        batchFile.Status = BatchFileStatus.Failed;
                                        batchFile.Remarks = remarks;
                                        batchFile.EndDate = DateTime.Now;
                                        InvalidFiles.Add(batchFile);
                                        Audit.Fail(logsPath, $"{remarks} - {Common.ListAncestors(batchFile.OTCS.Ancestors)}");
                                        continue;
                                    }

                                    if (findChecksumByPath.Checksum != checksum)
                                    {
                                        var remarks = $"File {fileName} checksum's is mismatch";
                                        batchFile.Status = BatchFileStatus.Failed;
                                        batchFile.Remarks = remarks;
                                        batchFile.EndDate = DateTime.Now;
                                        InvalidFiles.Add(batchFile);
                                        Audit.Fail(logsPath, $"{remarks} - {Common.ListAncestors(batchFile.OTCS.Ancestors)}");
                                        continue;
                                    }

                                    batchFile.Checksum = checksum;
                                    ValidFiles.Add(batchFile);
                                }
                            }
                        }
                    }

                    Logger.Information($"Scanning Completed");
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
    }
}
