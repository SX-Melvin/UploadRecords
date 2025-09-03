using UploadRecords.Models;

namespace UploadRecords.Utils
{
    public static class Audit
    {
        public static void Success(string path, string message)
        {
            Logger.Information(message);
            Write(Path.Combine(path, "success.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public static void Fail(string path, string message)
        {
            Logger.Error(message);
            Write(Path.Combine(path, "fail.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void Write(string path, string message)
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Append text (creates file if not exists)
            File.AppendAllText(path, message + Environment.NewLine);
        }
    }
}
