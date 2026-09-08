using Microsoft.Win32;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;

namespace UploadRecords.Utils
{
    public static class Registry
    {
        public static string GetRegistryValue(string keyToGet, string path = @"SwiftXSolutions\BatchJobCredentials")
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Batch job credentials require the Windows registry and DPAPI.");
            }

            string registryPath = $@"SOFTWARE\{path}";
            using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
            var stored = key?.GetValue(keyToGet)?.ToString();
            if (string.IsNullOrEmpty(stored))
            {
                throw new InvalidOperationException($"Registry credential '{keyToGet}' is missing.");
            }

            return Unprotect(stored);
        }

        [SupportedOSPlatform("windows")]
        private static string Unprotect(string encryptedText)
        {
            byte[] data = Convert.FromBase64String(encryptedText);
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
