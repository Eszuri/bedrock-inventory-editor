using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BedrockInventoryEditor.Core.Storage;

public class RecentWorldEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; } = DateTime.Now;
    public bool IsMcWorld { get; set; }
}

public static class RecentWorldsService
{
    private static readonly string SettingsDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BedrockInventoryEditor"
    );

    private static readonly string SettingsFilePath = System.IO.Path.Combine(SettingsDir, "recent_worlds.json");
    private const int MaxRecentEntries = 10;

    public static List<RecentWorldEntry> LoadRecentWorlds()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return [];

            var json = File.ReadAllText(SettingsFilePath);
            var list = JsonSerializer.Deserialize<List<RecentWorldEntry>>(json);
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void AddRecentWorld(string path, string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            Directory.CreateDirectory(SettingsDir);
            var list = LoadRecentWorlds();

            // Remove existing duplicate
            list.RemoveAll(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

            var isMcWorld = path.EndsWith(".mcworld", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

            list.Insert(0, new RecentWorldEntry
            {
                Path = path,
                Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) : name,
                LastOpened = DateTime.Now,
                IsMcWorld = isMcWorld
            });

            // Limit entries
            if (list.Count > MaxRecentEntries)
            {
                list = list.Take(MaxRecentEntries).ToList();
            }

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }

    public static void ClearRecentWorlds()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
        }
        catch { }
    }
}
