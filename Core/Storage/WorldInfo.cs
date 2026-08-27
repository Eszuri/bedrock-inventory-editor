using System;
using System.IO;

namespace BedrockInventoryEditor.Core.Storage;

public class WorldInfo
{
    public string FolderPath { get; set; } = string.Empty;
    public string DbPath => Path.Combine(FolderPath, "db");
    public string WorldName { get; set; } = string.Empty;
    public string IconPath => Path.Combine(FolderPath, "world_icon.jpeg");
    public DateTime LastModified { get; set; }

    public bool HasIcon => File.Exists(IconPath);
    public bool IsValid => Directory.Exists(DbPath);
}
