using Microsoft.Extensions.Configuration;
using Serilog;

namespace UploadRecords.Utils
{
    public class Logger
    {
        static Logger()
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // needed for console apps
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(config["Logs:Path"],
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
