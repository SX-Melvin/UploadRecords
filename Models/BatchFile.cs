using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using UploadRecords.Enums;
using UploadRecords.Models.API;

namespace UploadRecords.Models
{
    public class BatchFile
    {
        public required string Name { get; set; }
        public required string Path { get; set; }
        public required string LogDirectory { get; set; }
        public string? Checksum { get; set; } = null;
        public ControlFile ControlFile { get; set; }
        public string Remarks { get; set; }
        public BatchFileStatus Status { get; set; }
        public int Attempt { get; set; } = 1;
        public double SizeInKB { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public required string BatchFolderPath { get; set; }
        public required string? SubBatchFolderPath { get; set; } = null;
        public required PermissionInfo PermissionInfo { get; set; }
        public required ValidFileOTCS OTCS { get; set; }
    }

    public class ValidFileOTCS
    {
        public required long ParentID { get; set; }
        public required List<GetNodeAcestorsAncestor> Ancestors { get; set; }
    }

    public class PermissionInfo
    {
        public required PermissionInfoDivision Division { get; set; }
    }
    public class PermissionInfoDivision
    {
        public bool All { get; set; } = false;
        public bool UpdateBasedOnMetadata { get; set; } = false;
    }
}
