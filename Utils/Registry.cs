using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace UploadRecords.Utils
{
    public static class Registry
    {
        public static string GetRegistryValue(string keyToGet, string path = @"SwiftXSolutions\BatchJobCredentials")
        {
            string result = "";
            string registryPath = $@"SOFTWARE\{path}";
            using RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
            if (key != null)
            {
                string stored = key.GetValue(keyToGet)?.ToString();
                string decrypted = Unprotect(stored);
                result = decrypted;
            }

            return result;
        }

        private static string Unprotect(string encryptedText)
        {
            byte[] data = Convert.FromBase64String(encryptedText);
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
