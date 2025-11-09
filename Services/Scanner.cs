using Newtonsoft.Json;
using UploadRecords.Enums;
using UploadRecords.Models;
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

        public Scanner(string batchPath, string logPath, CSDB csdb, OTCS otcs, ControlFile controlFile)
        {
            FolderPath = batchPath;
            LogPath = logPath;
            OTCS = otcs;
            CSDB = csdb;
            ControlFile = controlFile;
        }

        public async Task ScanValidFiles()
        {
            try
            {
                var subBatchFolder = Path.Combine(FolderPath, ControlFile.FolderRef);

                Logger.Information($"Processing {subBatchFolder}");

                string root = Path.GetPathRoot(subBatchFolder);
                ControlFile.FolderPath = subBatchFolder.Substring(root.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToList();

                var batchFolderName = Path.GetFileName(subBatchFolder);
                string manifestFilePath = Path.Combine(subBatchFolder, ManifestFileName);
                var manifest = CSV.ReadManifest(manifestFilePath);
                var logsPath = Path.Combine(LogPath, Path.GetFileName(subBatchFolder));

                // Manifest not found
                if (manifest == null)
                {
                    Logger.Error($"Manifest file not found on {subBatchFolder}, skipped...");
                    return;
                }

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

                foreach (var file in Directory.GetFiles(subBatchFolder))
                {
                    var fileInfo = new FileInfo(file);

                    var batchFile = new BatchFile
                    {
                        ControlFile = ControlFile,
                        Path = file,
                        LogDirectory = logsPath,
                        Name = Path.GetFileName(file),
                        StartDate = DateTime.Now,
                        Attempt = 1,
                        SizeInKB = (double)fileInfo.Length / 1024,
                        OTCS = new() { ParentID = nodeId, Ancestors = ancestors },
                        BatchFolderPath = subBatchFolder,
                        SubBatchFolderPath = subBatchFolder,
                        Checksum = null
                    };
                    ValidFiles.Add(batchFile);
                }

                foreach (var fileRefFolder in Directory.GetDirectories(subBatchFolder))
                {
                    var fileRefFolderName = Path.GetFileName(fileRefFolder);

                    var createFileRefFolder = await OTCS.CreateFolder(fileRefFolderName, nodeId, getTicket.Ticket!);

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
                            ParentID = nodeId,
                            Type = 0
                        }
                    ]);

                    foreach (var folderFile in FoldersContainsFile)
                    {
                        var filesPath = Path.Combine(fileRefFolder, folderFile);

                        if (Path.Exists(filesPath))
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
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
    }
}
