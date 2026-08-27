using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BedrockInventoryEditor.Core.Models;
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

    public static void EnsureCurrentManifestValid(string dbPath)
    {
        try
        {
            var lockFile = Path.Combine(dbPath, "LOCK");
            if (File.Exists(lockFile))
            {
                try { File.Delete(lockFile); } catch { }
            }

            var currentFile = Path.Combine(dbPath, "CURRENT");
            if (File.Exists(currentFile))
            {
                var content = File.ReadAllText(currentFile).Trim();
                var targetManifest = Path.Combine(dbPath, content);
                if (!File.Exists(targetManifest))
                {
                    var manifestFiles = Directory.GetFiles(dbPath, "MANIFEST*");
                    if (manifestFiles.Length > 0)
                    {
                        Array.Sort(manifestFiles);
                        var latestManifest = Path.GetFileName(manifestFiles[^1]);
                        File.WriteAllBytes(currentFile, Encoding.ASCII.GetBytes(latestManifest + "\n"));
                    }
                }
            }
        }
        catch { }
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

        EnsureCurrentManifestValid(dbPath);

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
            EnsureCurrentManifestValid(dbPath);

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

    public static (double X, double Y, double Z, int DimensionId) GetPlayerPosition(NbtCompound? playerNbt)
    {
        if (playerNbt == null) return (0, 64, 0, 0);

        double x = 0, y = 64, z = 0;
        int dim = 0;

        if (playerNbt.GetList("Pos") is NbtList posList && posList.Count >= 3)
        {
            if (posList[0] is NbtFloat fx) x = fx.Value;
            else if (posList[0] is NbtDouble dx) x = dx.Value;

            if (posList[1] is NbtFloat fy) y = fy.Value;
            else if (posList[1] is NbtDouble dy) y = dy.Value;

            if (posList[2] is NbtFloat fz) z = fz.Value;
            else if (posList[2] is NbtDouble dz) z = dz.Value;
        }

        if (playerNbt.ContainsKey("DimensionId"))
        {
            dim = playerNbt.GetInt("DimensionId");
        }

        return (x, y, z, dim);
    }

    public static byte[] GetBlockEntityChunkKey(int chunkX, int chunkZ, int dimensionId)
    {
        if (dimensionId == 0)
        {
            var key = new byte[9];
            BitConverter.GetBytes(chunkX).CopyTo(key, 0);
            BitConverter.GetBytes(chunkZ).CopyTo(key, 4);
            key[8] = 49; // 0x31 = Tag 49 (BlockEntity)
            return key;
        }
        else
        {
            var key = new byte[13];
            BitConverter.GetBytes(chunkX).CopyTo(key, 0);
            BitConverter.GetBytes(chunkZ).CopyTo(key, 4);
            BitConverter.GetBytes(dimensionId).CopyTo(key, 8);
            key[12] = 49; // 0x31 = Tag 49 (BlockEntity)
            return key;
        }
    }

    public static (List<BlockEntityContainer> Containers, string? Error) LoadNearbyContainers(
        string worldFolderPath, 
        double playerX, 
        double playerY, 
        double playerZ, 
        int playerDimId = 0, 
        double maxRadius = 64.0)
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
                return (new List<BlockEntityContainer>(), $"Folder db tidak ditemukan di: {resolvedDir}");
            }
        }

        EnsureCurrentManifestValid(dbPath);

        try
        {
            var options = new Options { CreateIfMissing = false };
            using var db = new DB(options, dbPath);

            var rawContainers = new List<BlockEntityContainer>();

            if (maxRadius <= 0) maxRadius = 64.0;
            if (maxRadius > 1000.0) maxRadius = 1000.0;

            int playerChunkX = (int)Math.Floor(playerX) >> 4;
            int playerChunkZ = (int)Math.Floor(playerZ) >> 4;
            int chunkRadius = (int)Math.Ceiling(maxRadius / 16.0);

            // TARGETED CHUNK SCAN: Read only the chunks within bounding radius directly
            for (int cx = playerChunkX - chunkRadius; cx <= playerChunkX + chunkRadius; cx++)
            {
                for (int cz = playerChunkZ - chunkRadius; cz <= playerChunkZ + chunkRadius; cz++)
                {
                    byte[] keyBytes = GetBlockEntityChunkKey(cx, cz, playerDimId);
                    byte[]? valBytes = null;
                    try
                    {
                        valBytes = db.Get(keyBytes);
                    }
                    catch
                    {
                        // Skip individual corrupted or non-standard compression chunk blocks
                        continue;
                    }

                    if (valBytes == null || valBytes.Length == 0) continue;

                    try
                    {
                        var compounds = BedrockNbtReader.ReadMultipleCompounds(valBytes);
                        foreach (var comp in compounds)
                        {
                            ProcessBlockEntityCompound(comp, cx, cz, playerDimId, playerX, playerY, playerZ, maxRadius, rawContainers);
                        }
                    }
                    catch { }
                }
            }

            // Merge Double Chests
            var finalContainers = new List<BlockEntityContainer>();
            var mergedSecondarySet = new HashSet<BlockEntityContainer>();

            foreach (var c in rawContainers)
            {
                if (mergedSecondarySet.Contains(c)) continue;

                if (c.TypeId == "Chest" && c.PairX.HasValue && c.PairZ.HasValue && c.PairLead == 1)
                {
                    // Look for partner chest
                    var partner = rawContainers.FirstOrDefault(p => 
                        p.TypeId == "Chest" && 
                        p.X == c.PairX.Value && 
                        p.Z == c.PairZ.Value && 
                        p.Y == c.Y && 
                        p.DimensionId == c.DimensionId &&
                        p != c);

                    if (partner != null)
                    {
                        c.IsDoubleChest = true;
                        c.TotalSlots = 54;
                        c.GridRows = 6;
                        c.GridColumns = 9;
                        c.SecondaryNbt = partner.PrimaryNbt;
                        c.PairChunkX = partner.ChunkX;
                        c.PairChunkZ = partner.ChunkZ;

                        // Expand slots to 54
                        while (c.Slots.Count < 54)
                        {
                            c.Slots.Add(new ItemStack((byte)c.Slots.Count, SlotLocation.Container));
                        }

                        // Add partner items to slots 27..53
                        if (partner.PrimaryNbt.GetList("Items") is NbtList partnerItems)
                        {
                            foreach (var itemComp in partnerItems.OfType<NbtCompound>())
                            {
                                var slot = itemComp.GetByte("Slot");
                                int targetSlot = slot + 27;
                                if (targetSlot < c.Slots.Count)
                                {
                                    var loaded = ItemStack.FromNbt(itemComp, SlotLocation.Container, (byte)targetSlot);
                                    ItemStack.CopyProperties(loaded, c.Slots[targetSlot]);
                                }
                            }
                        }

                        mergedSecondarySet.Add(partner);
                    }
                }

                finalContainers.Add(c);
            }

            // Remove merged partner chests
            finalContainers.RemoveAll(mergedSecondarySet.Contains);

            // Sort by distance ascending
            finalContainers.Sort((a, b) => a.DistanceToPlayer.CompareTo(b.DistanceToPlayer));

            return (finalContainers, null);
        }
        catch (Exception ex)
        {
            return (new List<BlockEntityContainer>(), $"Gagal memindai container: {ex.Message}");
        }
    }

    private static void ProcessBlockEntityCompound(
        NbtCompound comp, 
        int chunkX, 
        int chunkZ, 
        int dimId, 
        double playerX, 
        double playerY, 
        double playerZ, 
        double maxRadius, 
        List<BlockEntityContainer> rawContainers)
    {
        var id = comp.GetString("id");
        if (string.IsNullOrWhiteSpace(id)) return;

        // Filter for recognized container types
        var (totalSlots, rows, cols) = BlockEntityContainer.GetContainerDimensions(id);
        if (totalSlots == 0) return;

        int bx = comp.GetInt("x");
        int by = comp.GetInt("y");
        int bz = comp.GetInt("z");

        double dx = bx + 0.5 - playerX;
        double dy = by + 0.5 - playerY;
        double dz = bz + 0.5 - playerZ;
        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (maxRadius > 0 && dist > maxRadius) return;

        var container = new BlockEntityContainer
        {
            TypeId = id,
            BlockId = BlockEntityContainer.GetDefaultBlockId(id),
            CustomName = comp.GetString("CustomName"),
            X = bx,
            Y = by,
            Z = bz,
            DimensionId = dimId,
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            DistanceToPlayer = dist,
            TotalSlots = totalSlots,
            GridRows = rows,
            GridColumns = cols,
            PrimaryNbt = comp
        };

        if (comp.ContainsKey("pairlead"))
        {
            container.PairLead = comp.GetByte("pairlead");
            if (comp.ContainsKey("pairx")) container.PairX = comp.GetInt("pairx");
            if (comp.ContainsKey("pairz")) container.PairZ = comp.GetInt("pairz");
        }

        // Initialize Slots
        for (byte s = 0; s < totalSlots; s++)
        {
            container.Slots.Add(new ItemStack(s, SlotLocation.Container));
        }

        // Populate items from NBT
        PopulateContainerSlots(container, comp);

        rawContainers.Add(container);
    }

    private static void PopulateContainerSlots(BlockEntityContainer container, NbtCompound comp)
    {
        // 1. Standard Items list (Chests, Shulkers, Barrels, Furnaces, Hoppers, Dispensers, etc.)
        if (comp.GetList("Items") is NbtList itemsList)
        {
            foreach (var itemComp in itemsList.OfType<NbtCompound>())
            {
                var slot = itemComp.GetByte("Slot");
                if (slot < container.Slots.Count)
                {
                    var loaded = ItemStack.FromNbt(itemComp, SlotLocation.Container, slot);
                    ItemStack.CopyProperties(loaded, container.Slots[slot]);
                }
            }
        }

        // 2. Campfires can also store Item1..Item4 directly on root
        if (container.TypeId == "Campfire" || container.TypeId == "SoulCampfire")
        {
            for (byte i = 0; i < 4; i++)
            {
                var tagName = $"Item{i + 1}";
                if (comp.GetCompound(tagName) is NbtCompound itemComp && itemComp.Count > 0)
                {
                    var loaded = ItemStack.FromNbt(itemComp, SlotLocation.Container, i);
                    ItemStack.CopyProperties(loaded, container.Slots[i]);
                }
            }
        }

        // 3. Lectern stores book
        if (container.TypeId == "Lectern" && comp.GetCompound("book") is NbtCompound bookComp)
        {
            var loaded = ItemStack.FromNbt(bookComp, SlotLocation.Container, 0);
            ItemStack.CopyProperties(loaded, container.Slots[0]);
        }

        // 4. Jukebox stores RecordItem
        if (container.TypeId == "Jukebox" && comp.GetCompound("RecordItem") is NbtCompound recordComp)
        {
            var loaded = ItemStack.FromNbt(recordComp, SlotLocation.Container, 0);
            ItemStack.CopyProperties(loaded, container.Slots[0]);
        }
    }

    public static (bool Success, string? BackupPath, string? Error) SaveBlockEntityContainers(
        string worldFolderPath, 
        IEnumerable<BlockEntityContainer> containers, 
        bool createBackup = false)
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
            EnsureCurrentManifestValid(dbPath);

            string? backupPath = null;
            if (createBackup)
            {
                backupPath = CreateBackup(resolvedDir);
            }

            var options = new Options { CreateIfMissing = false };

            using (var db = new DB(options, dbPath))
            {
                var writeOptions = new WriteOptions { Sync = true };

                // Group containers by chunk (ChunkX, ChunkZ, DimensionId)
                var chunkGroups = containers.GroupBy(c => (c.ChunkX, c.ChunkZ, c.DimensionId));

                foreach (var group in chunkGroups)
                {
                    var (chunkX, chunkZ, dimId) = group.Key;
                    var keyBytes = GetBlockEntityChunkKey(chunkX, chunkZ, dimId);
                    var existingVal = db.Get(keyBytes);

                    var compounds = existingVal != null && existingVal.Length > 0 
                        ? BedrockNbtReader.ReadMultipleCompounds(existingVal) 
                        : new List<NbtCompound>();

                    foreach (var container in group)
                    {
                        // 1. Sync container items back into PrimaryNbt
                        SyncContainerToNbt(container, container.PrimaryNbt, isSecondary: false);

                        // 2. Replace or add compound in chunk list
                        var idx = compounds.FindIndex(c => 
                            c.GetInt("x") == container.X && 
                            c.GetInt("y") == container.Y && 
                            c.GetInt("z") == container.Z);

                        if (idx >= 0)
                        {
                            compounds[idx] = container.PrimaryNbt;
                        }
                        else
                        {
                            compounds.Add(container.PrimaryNbt);
                        }

                        // 3. If Double Chest, handle secondary chest
                        if (container.IsDoubleChest && container.SecondaryNbt != null && container.PairX.HasValue && container.PairZ.HasValue)
                        {
                            SyncContainerToNbt(container, container.SecondaryNbt, isSecondary: true);

                            // If in the same chunk:
                            if (container.PairChunkX == chunkX && container.PairChunkZ == chunkZ)
                            {
                                var secIdx = compounds.FindIndex(c => 
                                    c.GetInt("x") == container.PairX.Value && 
                                    c.GetInt("y") == container.Y && 
                                    c.GetInt("z") == container.PairZ.Value);

                                if (secIdx >= 0)
                                {
                                    compounds[secIdx] = container.SecondaryNbt;
                                }
                                else
                                {
                                    compounds.Add(container.SecondaryNbt);
                                }
                            }
                            else
                            {
                                // Different chunk for partner chest
                                var partnerKey = GetBlockEntityChunkKey(container.PairChunkX, container.PairChunkZ, dimId);
                                var partnerVal = db.Get(partnerKey);
                                var partnerCompounds = partnerVal != null && partnerVal.Length > 0
                                    ? BedrockNbtReader.ReadMultipleCompounds(partnerVal)
                                    : new List<NbtCompound>();

                                var secIdx = partnerCompounds.FindIndex(c => 
                                    c.GetInt("x") == container.PairX.Value && 
                                    c.GetInt("y") == container.Y && 
                                    c.GetInt("z") == container.PairZ.Value);

                                if (secIdx >= 0)
                                {
                                    partnerCompounds[secIdx] = container.SecondaryNbt;
                                }
                                else
                                {
                                    partnerCompounds.Add(container.SecondaryNbt);
                                }

                                var partnerBytes = BedrockNbtWriter.WriteMultipleCompounds(partnerCompounds);
                                db.Put(partnerKey, partnerBytes, writeOptions);
                            }
                        }
                    }

                    // Write updated chunk compounds back to DB
                    var newBytes = BedrockNbtWriter.WriteMultipleCompounds(compounds);
                    db.Put(keyBytes, newBytes, writeOptions);
                }
            }

            var lockFile = Path.Combine(dbPath, "LOCK");
            if (File.Exists(lockFile))
            {
                try { File.Delete(lockFile); } catch { }
            }

            return (true, backupPath, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Gagal menyimpan container ke LevelDB: {ex.Message}\n(Pastikan Minecraft sedang ditutup).");
        }
    }

    private static void SyncContainerToNbt(BlockEntityContainer container, NbtCompound targetNbt, bool isSecondary)
    {
        int startSlot = isSecondary ? 27 : 0;
        int count = isSecondary ? 27 : (container.IsDoubleChest ? 27 : container.TotalSlots);

        // Standard Items list
        var itemsList = new NbtList("Items", NbtTagType.Compound);
        for (int i = 0; i < count; i++)
        {
            int slotIdx = startSlot + i;
            if (slotIdx < container.Slots.Count)
            {
                var item = container.Slots[slotIdx];
                if (!item.IsEmpty)
                {
                    var itemComp = item.ToNbt();
                    itemComp.SetByte("Slot", (byte)i); // LevelDB stores 0..26 for each chest half
                    itemsList.Add(itemComp);
                }
            }
        }
        targetNbt.Set(itemsList);

        // Campfires
        if (!isSecondary && (container.TypeId == "Campfire" || container.TypeId == "SoulCampfire"))
        {
            for (byte i = 0; i < 4; i++)
            {
                var tagName = $"Item{i + 1}";
                if (i < container.Slots.Count && !container.Slots[i].IsEmpty)
                {
                    targetNbt.Set(container.Slots[i].ToNbt(tagName));
                }
                else
                {
                    targetNbt.Remove(tagName);
                }
            }
        }

        // Lectern
        if (!isSecondary && container.TypeId == "Lectern")
        {
            if (container.Slots.Count > 0 && !container.Slots[0].IsEmpty)
            {
                targetNbt.Set(container.Slots[0].ToNbt("book"));
            }
            else
            {
                targetNbt.Remove("book");
            }
        }

        // Jukebox
        if (!isSecondary && container.TypeId == "Jukebox")
        {
            if (container.Slots.Count > 0 && !container.Slots[0].IsEmpty)
            {
                targetNbt.Set(container.Slots[0].ToNbt("RecordItem"));
            }
            else
            {
                targetNbt.Remove("RecordItem");
            }
        }
    }

    public static void ExtractPlayerStats(NbtCompound playerNbt, WorldSettingsModel model)
    {
        if (playerNbt.GetList("Attributes") is NbtList attrs)
        {
            foreach (var attr in attrs.OfType<NbtCompound>())
            {
                var name = attr.GetString("Name");
                if (name == "minecraft:health")
                {
                    model.Health = attr.Get<NbtFloat>("Current")?.Value ?? 20f;
                    model.MaxHealth = attr.Get<NbtFloat>("Max")?.Value ?? 20f;
                }
                else if (name == "minecraft:player.hunger")
                {
                    model.Hunger = attr.Get<NbtFloat>("Current")?.Value ?? 20f;
                }
                else if (name == "minecraft:player.saturation")
                {
                    model.Saturation = attr.Get<NbtFloat>("Current")?.Value ?? 20f;
                }
                else if (name == "minecraft:player.level")
                {
                    model.XpLevel = (int)(attr.Get<NbtFloat>("Current")?.Value ?? 0);
                }
                else if (name == "minecraft:player.experience")
                {
                    model.XpProgress = attr.Get<NbtFloat>("Current")?.Value ?? 0f;
                }
            }
        }

        if (playerNbt.ContainsKey("PlayerLevel"))
        {
            model.XpLevel = playerNbt.GetInt("PlayerLevel", model.XpLevel);
        }
        if (playerNbt.ContainsKey("PlayerLevelProgress"))
        {
            model.XpProgress = playerNbt.Get<NbtFloat>("PlayerLevelProgress")?.Value ?? model.XpProgress;
        }

        model.Dimension = playerNbt.GetInt("Dimension", 0);

        if (playerNbt.GetList("Pos") is NbtList posList && posList.Count >= 3)
        {
            model.PosX = (posList[0] as NbtFloat)?.Value ?? 0f;
            model.PosY = (posList[1] as NbtFloat)?.Value ?? 0f;
            model.PosZ = (posList[2] as NbtFloat)?.Value ?? 0f;
        }
    }

    public static void UpdatePlayerStats(NbtCompound playerNbt, WorldSettingsModel model)
    {
        // 1. Update or create Attributes list
        var attrs = playerNbt.GetList("Attributes") ?? new NbtList("Attributes", NbtTagType.Compound);
        
        NbtCompound GetOrCreateAttr(string name)
        {
            var found = attrs.OfType<NbtCompound>().FirstOrDefault(a => a.GetString("Name") == name);
            if (found == null)
            {
                found = new NbtCompound();
                found.SetString("Name", name);
                attrs.Add(found);
            }
            return found;
        }

        var healthAttr = GetOrCreateAttr("minecraft:health");
        healthAttr.SetFloat("Current", model.Health);
        healthAttr.SetFloat("Base", model.MaxHealth);
        healthAttr.SetFloat("Max", model.MaxHealth);
        healthAttr.SetFloat("Min", 0f);

        var hungerAttr = GetOrCreateAttr("minecraft:player.hunger");
        hungerAttr.SetFloat("Current", model.Hunger);
        hungerAttr.SetFloat("Base", 20f);
        hungerAttr.SetFloat("Max", 20f);
        hungerAttr.SetFloat("Min", 0f);

        var satAttr = GetOrCreateAttr("minecraft:player.saturation");
        satAttr.SetFloat("Current", model.Saturation);
        satAttr.SetFloat("Base", 5f);
        satAttr.SetFloat("Max", 20f);
        satAttr.SetFloat("Min", 0f);

        var xpLvlAttr = GetOrCreateAttr("minecraft:player.level");
        xpLvlAttr.SetFloat("Current", model.XpLevel);
        xpLvlAttr.SetFloat("Base", model.XpLevel);
        xpLvlAttr.SetFloat("Max", 24791f);
        xpLvlAttr.SetFloat("Min", 0f);

        var xpProgAttr = GetOrCreateAttr("minecraft:player.experience");
        xpProgAttr.SetFloat("Current", Math.Clamp(model.XpProgress, 0f, 1f));
        xpProgAttr.SetFloat("Base", Math.Clamp(model.XpProgress, 0f, 1f));
        xpProgAttr.SetFloat("Max", 1f);
        xpProgAttr.SetFloat("Min", 0f);

        playerNbt.Set(attrs);

        // 2. Root player level tags
        playerNbt.SetInt("PlayerLevel", model.XpLevel);
        playerNbt.SetFloat("PlayerLevelProgress", Math.Clamp(model.XpProgress, 0f, 1f));
        playerNbt.SetInt("Dimension", model.Dimension);

        // 3. Update Player Position
        var posList = new NbtList("Pos", NbtTagType.Float)
        {
            new NbtFloat(string.Empty, (float)model.PosX),
            new NbtFloat(string.Empty, (float)model.PosY),
            new NbtFloat(string.Empty, (float)model.PosZ)
        };
        playerNbt.Set(posList);
    }
}
