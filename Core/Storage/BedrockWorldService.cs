using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BedrockInventoryEditor.Core.Nbt;
using LevelDB;

namespace BedrockInventoryEditor.Core.Storage;

public record WorldMetadata(string FolderName, string DisplayName, string FolderPath, DateTime LastModified);

public static class BedrockWorldService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BedrockInventoryEditor"
    );

    public static string? ResolveWorldDirectory(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath)) return null;

        // 1. If it's a .mcworld or .zip file, extract it to a persistent folder
        if (File.Exists(inputPath))
        {
            var ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext == ".mcworld" || ext == ".zip")
            {
                var safeName = Path.GetFileNameWithoutExtension(inputPath);
                var extractFolder = Path.Combine(AppDataFolder, "ExtractedWorlds", safeName);

                if (Directory.Exists(extractFolder))
                {
                    try { Directory.Delete(extractFolder, true); } catch { }
                }
                Directory.CreateDirectory(extractFolder);

                ZipFile.ExtractToDirectory(inputPath, extractFolder);

                // Check if files are nested in a single root subfolder
                if (!File.Exists(Path.Combine(extractFolder, "level.dat")) && !Directory.Exists(Path.Combine(extractFolder, "db")))
                {
                    var subDirs = Directory.GetDirectories(extractFolder);
                    if (subDirs.Length == 1 && (File.Exists(Path.Combine(subDirs[0], "level.dat")) || Directory.Exists(Path.Combine(subDirs[0], "db"))))
                    {
                        return subDirs[0];
                    }
                }

                return extractFolder;
            }
        }

        // 2. If it's a directory
        if (Directory.Exists(inputPath))
        {
            return inputPath;
        }

        return null;
    }

    public static string CreateBackup(string worldFolderPath)
    {
        var resolvedDir = ResolveWorldDirectory(worldFolderPath) ?? worldFolderPath;
        var globalBackupsDir = Path.Combine(AppDataFolder, "Backups");
        Directory.CreateDirectory(globalBackupsDir);

        var worldName = "world";
        var levelNameFile = Path.Combine(resolvedDir, "levelname.txt");
        if (File.Exists(levelNameFile))
        {
            worldName = File.ReadAllText(levelNameFile).Trim();
            foreach (var c in Path.GetInvalidFileNameChars()) worldName = worldName.Replace(c, '_');
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFile = Path.Combine(globalBackupsDir, $"{worldName}_{timestamp}.zip");

        var tempFolder = Path.Combine(Path.GetTempPath(), "BedrockBackup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        try
        {
            var dbPath = Path.Combine(resolvedDir, "db");
            if (Directory.Exists(dbPath))
            {
                var targetDb = Path.Combine(tempFolder, "db");
                Directory.CreateDirectory(targetDb);
                foreach (var file in Directory.GetFiles(dbPath))
                {
                    var fname = Path.GetFileName(file);
                    if (fname.Equals("LOCK", StringComparison.OrdinalIgnoreCase)) continue;
                    try { File.Copy(file, Path.Combine(targetDb, fname), true); } catch { }
                }
            }

            var levelDat = Path.Combine(resolvedDir, "level.dat");
            if (File.Exists(levelDat)) File.Copy(levelDat, Path.Combine(tempFolder, "level.dat"), true);

            var levelName = Path.Combine(resolvedDir, "levelname.txt");
            if (File.Exists(levelName)) File.Copy(levelName, Path.Combine(tempFolder, "levelname.txt"), true);

            var worldIcon = Path.Combine(resolvedDir, "world_icon.jpeg");
            if (File.Exists(worldIcon)) File.Copy(worldIcon, Path.Combine(tempFolder, "world_icon.jpeg"), true);

            ZipFile.CreateFromDirectory(tempFolder, backupFile, System.IO.Compression.CompressionLevel.Optimal, false);
            return backupFile;
        }
        finally
        {
            try { Directory.Delete(tempFolder, true); } catch { }
        }
    }

    public static (NbtCompound? Nbt, string? DetectedKey, bool HasRootHeader, string? Error) LoadPlayerNbt(string worldFolderPath, string playerKey = "~local_player")
    {
        var resolvedDir = ResolveWorldDirectory(worldFolderPath) ?? worldFolderPath;
        var dbPath = Path.Combine(resolvedDir, "db");

        if (!Directory.Exists(dbPath))
        {
            if (File.Exists(Path.Combine(resolvedDir, "CURRENT")) || Directory.GetFiles(resolvedDir, "MANIFEST*").Length > 0)
            {
                dbPath = resolvedDir;
            }
            else
            {
                return (null, null, true, $"Folder database LevelDB ('db') tidak ditemukan di:\n{resolvedDir}");
            }
        }

        try
        {
            var options = new Options { CreateIfMissing = true };
            using var db = new DB(options, dbPath);

            var keyBytes = Encoding.UTF8.GetBytes(playerKey);
            var valueBytes = db.Get(keyBytes);

            if (valueBytes != null && valueBytes.Length > 0)
            {
                var nbt = BedrockNbtReader.ReadFromBytes(valueBytes, out var hasHeader);
                return (nbt, playerKey, hasHeader, null);
            }

            // Fallback: search for any available player keys in the DB
            var playerKeys = FindAllPlayerKeys(db);
            if (playerKeys.Count > 0)
            {
                foreach (var k in playerKeys)
                {
                    var data = db.Get(Encoding.UTF8.GetBytes(k));
                    if (data != null && data.Length > 0)
                    {
                        var nbtComp = BedrockNbtReader.ReadFromBytes(data, out var hasHeader);
                        return (nbtComp, k, hasHeader, null);
                    }
                }
            }

            // Check if DB is completely empty (0 keys)
            int totalKeys = 0;
            foreach (var _ in db)
            {
                totalKeys++;
                if (totalKeys > 0) break;
            }

            if (totalKeys == 0)
            {
                return (null, null, true, "Database LevelDB world ini masih kosong (0 keys).\n\nSilakan buka world di Minecraft terlebih dahulu, lalu pilih 'Simpan & Keluar' (Save & Quit) agar Minecraft menulis data pemain ke disk.");
            }

            return (null, null, true, $"Data player dengan key '{playerKey}' tidak ditemukan di LevelDB world ini.\n\nPastikan Anda telah memainkan world ini dan memilih 'Simpan & Keluar'.");
        }
        catch (Exception ex)
        {
            return (null, null, true, $"Gagal membuka database LevelDB: {ex.Message}\n(Pastikan game Minecraft sedang ditutup).");
        }
    }

    public static (bool Success, string? BackupPath, string? Error) SavePlayerNbt(
        string worldFolderPath, 
        NbtCompound playerNbt, 
        string playerKey = "~local_player", 
        bool createBackup = false,
        bool hasRootHeader = true)
    {
        var resolvedDir = ResolveWorldDirectory(worldFolderPath) ?? worldFolderPath;
        var dbPath = Path.Combine(resolvedDir, "db");

        if (!Directory.Exists(dbPath))
        {
            if (File.Exists(Path.Combine(resolvedDir, "CURRENT")) || Directory.GetFiles(resolvedDir, "MANIFEST*").Length > 0)
            {
                dbPath = resolvedDir;
            }
            else
            {
                return (false, null, "Folder 'db' LevelDB tidak ditemukan.");
            }
        }

        try
        {
            string? backupPath = null;
            if (createBackup)
            {
                backupPath = CreateBackup(resolvedDir);
            }

            var options = new Options { CreateIfMissing = true };

            // Open LevelDB, write player NBT, and explicitly dispose to flush
            using (var db = new DB(options, dbPath))
            {
                var valueBytes = BedrockNbtWriter.WriteToBytes(playerNbt, includeRootHeader: hasRootHeader);
                var writeOptions = new WriteOptions { Sync = true };

                // Save to primary playerKey
                db.Put(Encoding.UTF8.GetBytes(playerKey), valueBytes, writeOptions);

                // Synchronize with ~local_player and any player_server_* keys
                var allPlayerKeys = FindAllPlayerKeys(db);
                foreach (var pk in allPlayerKeys)
                {
                    if (pk != playerKey)
                    {
                        db.Put(Encoding.UTF8.GetBytes(pk), valueBytes, writeOptions);
                    }
                }

                if (playerKey != "~local_player")
                {
                    db.Put(Encoding.UTF8.GetBytes("~local_player"), valueBytes, writeOptions);
                }
            } // DB is fully flushed and closed here

            // Remove any LOCK file left by LevelDB
            var lockFile = Path.Combine(dbPath, "LOCK");
            if (File.Exists(lockFile))
            {
                try { File.Delete(lockFile); } catch { }
            }

            return (true, backupPath, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Gagal menyimpan ke LevelDB: {ex.Message}\n(Pastikan Minecraft sedang ditutup).");
        }
    }

    public static List<string> FindAllPlayerKeys(DB db)
    {
        var keys = new List<string>();
        try
        {
            foreach (var kvp in db)
            {
                var keyStr = Encoding.UTF8.GetString(kvp.Key.ToArray());
                if (keyStr == "~local_player" || keyStr.StartsWith("player_server_") || keyStr.StartsWith("player_"))
                {
                    keys.Add(keyStr);
                }
            }
        }
        catch { }
        return keys;
    }
}
