using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using UploadRecords.Enums;
using UploadRecords.Models;
using UploadRecords.Models.API;
using UploadRecords.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public List<string> ValidFileExtensions = [".tiff", ".pdf", ".tif"];
        public List<BatchFile> InvalidFiles = [];
        public List<BatchFile> ValidFiles = [];
        public OTCS OTCS;
        public CSDB CSDB;
        public long RootNodeID;
        public long UploadNodeID;
        public List<GetNodeAcestorsAncestor> RootAncestors = [];
        public List<string> RootFiles = ["bag-info.txt", "bagit.txt", "manifest-sha256.txt", "tagmanifest-sha256.txt"];
        public List<DivisionData> Divisions;

        public Scanner(string batchPath, string logPath, CSDB csdb, OTCS otcs, ControlFile controlFile, List<DivisionData> divisions, long uploadNodeID)
        {
            FolderPath = batchPath;
            LogPath = logPath;
            OTCS = otcs;
            CSDB = csdb;
            ControlFile = controlFile;
            Divisions = divisions;
            UploadNodeID = uploadNodeID;
        }

        public async Task ScanValidFiles()
        {
            try
            {
                var subBatchFolder = Path.Combine(FolderPath, ControlFile.FolderRef);

                if(!Path.Exists(subBatchFolder))
                {
                    Logger.Error($"{subBatchFolder} path is not exist...");

                    // Add XXXX_ prefix format
                    ControlFile.FolderRef = int.Parse(ControlFile.FolderRef.Split('_').Last().Replace("Vol ", "")).ToString("D4") + "_" + ControlFile.FolderRef;
                    subBatchFolder = Path.Combine(FolderPath, ControlFile.FolderRef);
                    if (!Path.Exists(subBatchFolder))
                    {
                        Logger.Error($"{subBatchFolder} path is also not exist, skipped...");
                        return;
                    }
                }

                Logger.Information($"Processing {subBatchFolder}");

                string root = Path.GetPathRoot(subBatchFolder);
                ControlFile.FolderPath = subBatchFolder.Substring(root.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToList();

                string manifestFilePath = Path.Combine(subBatchFolder, ManifestFileName);
                var logsPath = Path.Combine(LogPath, Path.GetFileName(subBatchFolder));
                var manifest = Manifest.ReadManifest(manifestFilePath);

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

                var folder = await OTCS.CreateFolder(Path.GetFileName(FolderPath), UploadNodeID, getTicket.Ticket!, Divisions);
                var folder2 = await OTCS.CreateFolder(ControlFile.FolderRef, folder.Id, getTicket.Ticket!, Divisions);
                var nodeId = folder2.Id;
                var getAncestors = await OTCS.GetNodeAncestors(nodeId, getTicket.Ticket!);
                var ancestors = getAncestors.Ancestors;

                RootAncestors = ancestors;
                RootNodeID = nodeId;

                foreach (var file in Directory.GetFiles(subBatchFolder))
                {
                    var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileName(file);
                    var batchFile = new BatchFile
                    {
                        ControlFile = ControlFile,
                        Path = file,
                        LogDirectory = logsPath,
                        Name = fileName,
                        StartDate = DateTime.Now,
                        Attempt = 1,
                        SizeInKB = (double)fileInfo.Length / 1024,
                        OTCS = new() { ParentID = nodeId, Ancestors = ancestors },
                        BatchFolderPath = FolderPath,
                        SubBatchFolderPath = subBatchFolder,
                        Checksum = null,
                        PermissionInfo = new()
                        {
                            Division = new()
                        }
                    };
                    ValidFiles.Add(batchFile);
                }

                foreach (var fileRefFolder in Directory.GetDirectories(subBatchFolder))
                {
                    var fileRefFolderName = Path.GetFileName(fileRefFolder);

                    var createFileRefFolder = await OTCS.CreateFolder(fileRefFolderName, nodeId, getTicket.Ticket!, Divisions);

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
                        var fileAncestors = ancestors;
                        var filesPath = Path.Combine(fileRefFolder, folderFile);

                        if (Path.Exists(filesPath))
                        {
                            var createFileFolder = await OTCS.CreateFolder(folderFile, createFileRefFolder.Id, getTicket.Ticket!, Divisions);

                            // Failed to create batch folder
                            if (createFileFolder.Error != null)
                            {
                                Logger.Error("Process aborted due to: " + createFileFolder.Error);
                                continue;
                            }

                            fileAncestors.Add(new()
                            {
                                Id = createFileFolder.Id,
                                Name = folderFile,
                                ParentID = createFileRefFolder.Id,
                                Type = 0
                            });

                            foreach (var file in Directory.GetFiles(filesPath))
                            {
                                var fileInfo = new FileInfo(file);
                                var fileName = Path.GetFileName(file);
                                var batchFile = new BatchFile()
                                {
                                    ControlFile = ControlFile,
                                    Path = file,
                                    LogDirectory = logsPath,
                                    Name = Path.GetFileName(file),
                                    StartDate = DateTime.Now,
                                    Attempt = 1,
                                    SizeInKB = (double)fileInfo.Length / 1024,
                                    OTCS = new() { ParentID = createFileFolder.Id, Ancestors = fileAncestors },
                                    BatchFolderPath = FolderPath,
                                    SubBatchFolderPath = subBatchFolder,
                                    PermissionInfo = new()
                                    {
                                        Division = new()
                                        {
                                            UpdateBasedOnMetadata = Path.GetExtension(fileName) == ".pdf"
                                        }
                                    }
                                };

                                Logger.Information($"Processing {file}");

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

        public async Task AddMetadataFileToValidFiles(string metadataFilePath)
        {
            var fileInfo = new FileInfo(metadataFilePath);
            var nodeId = RootAncestors.First(x => x.Id == RootNodeID).ParentID;

            // Create all the necesarry folders
            var getTicket = await OTCS.GetTicket();
            if (getTicket.Error != null)
            {
                Logger.Error("Control file failed to upload due to: " + getTicket.Error);
                return;
            }

            var getAncestors = await OTCS.GetNodeAncestors(nodeId, getTicket.Ticket!);
            var ancestors = getAncestors.Ancestors;

            var batchFile = new BatchFile
            {
                ControlFile = ControlFile,
                Path = metadataFilePath,
                LogDirectory = Path.Combine(LogPath, Path.GetFileName(FolderPath)),
                Name = Path.GetFileName(metadataFilePath),
                StartDate = DateTime.Now,
                Attempt = 1,
                SizeInKB = (double)fileInfo.Length / 1024,
                OTCS = new() { ParentID = nodeId, Ancestors = ancestors },
                BatchFolderPath = FolderPath,
                SubBatchFolderPath = null,
                Checksum = null,
                PermissionInfo = new()
                {
                    Division = new()
                }
            };

            ValidFiles.Add(batchFile);
        }
    }
}
