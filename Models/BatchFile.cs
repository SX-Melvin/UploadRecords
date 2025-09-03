﻿using UploadRecords.Enums;

namespace UploadRecords.Models
{
    public class BatchFile
    {
        public required string Name { get; set; }
        public required string Path { get; set; }
        public required string LogDirectory { get; set; }
        public string Checksum { get; set; }
        public string Remarks { get; set; }
        public BatchFileStatus Status { get; set; }
        public int Attempt { get; set; } = 1;
        public double SizeInKB { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public required string BatchFolderPath { get; set; }
        public required string SubBatchFolderPath { get; set; }
        public required ValidFileOTCS OTCS { get; set; }
    }

    public class ValidFileOTCS
    {
        public required int ParentID { get; set; }
    }
}
