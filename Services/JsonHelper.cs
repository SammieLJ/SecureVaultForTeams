using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SecureVaultForTeams.Services
{
    public static class JsonHelper
    {
        public static T SafeReadJsonFile<T>(string path, T defaultValue)
        {
            try
            {
                if (!File.Exists(path))
                    return defaultValue;

                var content = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(content) || content[0] == '\0')
                    return defaultValue;

                return JsonSerializer.Deserialize<T>(content) ?? defaultValue;
            }
            catch (Exception)
            {
                // Optionally log the error
                return defaultValue;
            }
        }
    }
}
