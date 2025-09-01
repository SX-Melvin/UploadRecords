using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class CSV
    {
        public static List<ManifestFile>? ReadManifest(string filePath)
        {
            List<ManifestFile>? manifest = null;

            if (File.Exists(filePath))
            {
                manifest = [];

                foreach (var line in File.ReadLines(filePath))
                {
                    // Skip header if it has one
                    if (line.StartsWith("checksum,", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var columns = line.Split(',');

                    // Make sure there are enough columns
                    if (columns.Length >= 2)
                    {
                        manifest.Add(new ManifestFile
                        {
                            Checksum = columns[0].Trim(),
                            Path = columns[1].Trim()
                        });
                    }
                }
            }

            return manifest;
        }
    }
}
