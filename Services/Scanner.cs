using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Configs;
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
        public string DataFolder = "data";
        public List<string> FoldersContainsFile = ["master", "access"];
        public List<string> ValidFileExtensions = [".tiff", ".pdf"];
        public List<ValidFile> ValidFiles = [];

        public Scanner(string batchPath, string logPath)
        {
            FolderPath = batchPath;
            LogPath = logPath;
        }

        public void ScanValidFiles()
        {
            try
            {
                Logger.Information($"Beginning Scanning");

                string controlFilePath = Path.Combine(FolderPath, ControlFileName);
                var metadata = Excel.ReadControlFile(controlFilePath);

                // Control file not found
                if (metadata == null)
                {
                    Logger.Error("Control file not found, skipped...");
                    return;
                }

                foreach (var subBatchFolder in Directory.GetDirectories(FolderPath))
                {
                    Logger.Information($"Processing {subBatchFolder}");

                    string manifestFilePath = Path.Combine(subBatchFolder, ManifestFileName);
                    var manifest = CSV.ReadManifest(manifestFilePath);

                    // Manifest not found
                    if (manifest == null)
                    {
                        Logger.Error($"Manifest file not found on {subBatchFolder}, skipped...");
                        continue;
                    }

                    foreach (var folder in FoldersContainsFile)
                    {
                        string filesPath = Path.Combine(Path.Combine(subBatchFolder, DataFolder), folder);
                        var logsPath = Path.Combine(LogPath, Path.GetFileName(subBatchFolder));

                        foreach (var file in Directory.GetFiles(filesPath))
                        {
                            Logger.Information($"Processing {file}");
                            var fileName = Path.GetFileName(file);

                            // Check file extensions
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            if (!ValidFileExtensions.Contains(ext))
                            {
                                Logger.Error($"File extension is not valid, skipped...");
                                Audit.Fail(logsPath, $"File {fileName} has no valid extension - {file}");
                                continue;
                            }

                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length == 0)
                            {
                                Logger.Error($"File is empty, skipped...");
                                Audit.Fail(logsPath, $"File {fileName} is empty - {file}");
                                continue;
                            }

                            var checksum = Checksum.GetFromFile(file);
                            var parts = file.Split(Path.DirectorySeparatorChar);
                            var filePathChecksum = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(Math.Max(0, parts.Length - 3)));

                            var findChecksumByPath = manifest.FirstOrDefault(x => x.Path == filePathChecksum);

                            if (findChecksumByPath == null)
                            {
                                Logger.Error($"Checksum not found in manifest file, skipped...");
                                Audit.Fail(logsPath, $"File {fileName} checksum's not found in manifest file - {file}");
                                continue;
                            }

                            if (findChecksumByPath.Checksum != checksum)
                            {
                                Logger.Error($"Checksum mismatch, skipped...");
                                Audit.Fail(logsPath, $"File {fileName} checksum's is mismatch - {file}");
                                continue;
                            }

                            ValidFiles.Add(new() {
                                Checksum = checksum,
                                Path = file,
                                LogDirectory = logsPath,
                                Name = Path.GetFileName(file),
                                //OTCS = new() { ParentID = 1231230 },
                                OTCS = new() { ParentID = 1145353 },
                                BatchFolderPath = filesPath,
                                SubBatchFolderPath = subBatchFolder
                            });
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
