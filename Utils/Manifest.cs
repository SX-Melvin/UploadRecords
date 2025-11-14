using DocumentFormat.OpenXml.ExtendedProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Manifest
    {
        public static List<ManifestFile>? ReadManifest(string filePath)
        {
            List<ManifestFile>? manifest = null;

            if (File.Exists(filePath))
            {
                manifest = [];

                var lines = File.ReadLines(filePath);

                foreach (var line in lines)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Split into 2 columns: hash + path
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    string hash = parts[0];
                    string path = parts[1];

                    manifest.Add(new ManifestFile
                    {
                        Checksum = hash,
                        Path = path
                    });
                }
            }

            return manifest;
        }
    }
}
