using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Utils;

namespace UploadRecords.Services
{
    public class Main
    {
        public string folderPath;
        public string controlFileName = "metadata.xlsx";
        public string manifestFileName = "manifest-sha256.txt";
        public string dataFolder = "data";
        public List<string> foldersContainsFile = ["master", "access"];
        public List<string> validFileExtensions = [".tiff", ".pdf"];

        public Main(string batchPath)
        {
            folderPath = batchPath;
        }

        public void Run()
        {
            try
            {
                string controlFilePath = Path.Combine(folderPath, controlFileName);
                var metadata = Excel.ReadControlFile(controlFilePath);

                // Control file not found
                if (metadata == null)
                {
                    Logger.Error("Control file not found, skipped...");
                    return;
                }

                foreach (var subBatchFolder in Directory.GetDirectories(folderPath))
                {
                    Logger.Information($"Processing {subBatchFolder}");

                    string manifestFilePath = Path.Combine(subBatchFolder, manifestFileName);
                    var manifest = CSV.ReadManifest(manifestFilePath);

                    // Manifest not found
                    if (manifest == null)
                    {
                        Logger.Error($"Manifest file not found on {subBatchFolder}, skipped...");
                        continue;
                    }

                    foreach (var folder in foldersContainsFile)
                    {
                        string filesPath = Path.Combine(Path.Combine(subBatchFolder, dataFolder), folder);

                        foreach (var file in Directory.GetFiles(filesPath))
                        {
                            Logger.Information($"Processing {file}");

                            // Check file extensions
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            if (!validFileExtensions.Contains(ext))
                            {
                                Logger.Error($"File extension is not valid, skipped...");
                                continue;
                            }

                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length == 0)
                            {
                                Logger.Error($"File is empty, skipped...");
                                continue;
                            }

                            var checksum = Checksum.GetFromFile(file);
                            var parts = file.Split(Path.DirectorySeparatorChar);
                            var filePathChecksum = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(Math.Max(0, parts.Length - 3)));

                            var findChecksumByPath = manifest.FirstOrDefault(x => x.Checksum == checksum && x.Path == filePathChecksum);

                            if (findChecksumByPath == null)
                            {
                                Logger.Error($"Checksum not found in manifest file, skipped...");
                                continue;
                            }

                            if (findChecksumByPath.Checksum != checksum)
                            {
                                Logger.Error($"Checksum mismatch, skipped...");
                                continue;
                            }

                            Logger.Information($"File valid so far...");
                        }
                    }

                    Logger.Information($"End");
                }

            }
            catch (Exception ex)
            {
                    Logger.Error(ex.Message);
            }
        }
    }
}
