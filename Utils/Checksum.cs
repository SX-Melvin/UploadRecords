using System.Security.Cryptography;

namespace UploadRecords.Utils
{
    public static class Checksum
    {
        public static string GetFromFile(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);

           return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
