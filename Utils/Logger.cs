using Serilog;

namespace UploadRecords.Utils
{
    public class Logger
    {
        public static string logsPath = "logs/uprec_.log";
        static Logger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(logsPath,
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 30)
                .CreateLogger();
        }
        public static void Information(string message) => Log.Information(message);
        public static void Warning(string message) => Log.Warning(message);
        public static void Error(string message, Exception? ex = null) => Log.Error(ex, message);
        public static void Debug(string message) => Log.Debug(message);
    }
}
