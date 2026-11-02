using System;
using System.IO;
using System.Text.Json;

namespace MinecraftHotbar
{
    internal static class ConfigManager
    {
        public static string ConfigPath { get; private set; }

        static ConfigManager()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string directPath = Path.Combine(baseDir, "config.json");
            string appSubPath = Path.Combine(baseDir, "app", "config.json");
            string parentAppPath = Path.Combine(baseDir, "..", "app", "config.json");

            if (File.Exists(directPath))
                ConfigPath = Path.GetFullPath(directPath);
            else if (File.Exists(appSubPath))
                ConfigPath = Path.GetFullPath(appSubPath);
            else if (File.Exists(parentAppPath))
                ConfigPath = Path.GetFullPath(parentAppPath);
            else
                ConfigPath = Path.GetFullPath(directPath);
        }

        public static string ReadRawConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    return File.ReadAllText(ConfigPath);
                }
            }
            catch { }
            return "{}";
        }

        public static void SaveRawConfig(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return;

                // Formatear con sangría limpia
                using var doc = JsonDocument.Parse(json);
                string pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, pretty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando config.json: {ex.Message}");
            }
        }
    }
}
