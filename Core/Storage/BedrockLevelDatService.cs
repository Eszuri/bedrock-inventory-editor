using System;
using System.IO;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;

namespace BedrockInventoryEditor.Core.Storage;

public static class BedrockLevelDatService
{
    public static (WorldSettingsModel? Model, NbtCompound? RawNbt, int HeaderVersion, string? Error) LoadWorldSettings(string worldFolderPath)
    {
        var resolvedDir = BedrockWorldService.ResolveWorldDirectory(worldFolderPath) ?? worldFolderPath;
        var levelDatPath = Path.Combine(resolvedDir, "level.dat");

        if (!File.Exists(levelDatPath))
        {
            return (null, null, 10, $"File level.dat tidak ditemukan di: {resolvedDir}");
        }

        try
        {
            var fileBytes = File.ReadAllBytes(levelDatPath);
            if (fileBytes.Length < 8)
            {
                return (null, null, 10, "Format level.dat tidak valid (ukuran file terlalu kecil).");
            }

            int headerVersion = BitConverter.ToInt32(fileBytes, 0);
            int payloadLength = BitConverter.ToInt32(fileBytes, 4);

            var nbtPayload = new byte[fileBytes.Length - 8];
            Array.Copy(fileBytes, 8, nbtPayload, 0, nbtPayload.Length);

            var rootNbt = BedrockNbtReader.ReadFromBytes(nbtPayload);
            var model = new WorldSettingsModel();

            // 🏷️ General Info
            var levelName = rootNbt.GetString("LevelName");
            if (string.IsNullOrWhiteSpace(levelName))
            {
                var txtPath = Path.Combine(resolvedDir, "levelname.txt");
                if (File.Exists(txtPath))
                {
                    levelName = File.ReadAllText(txtPath).Trim();
                }
            }
            model.WorldName = string.IsNullOrWhiteSpace(levelName) ? Path.GetFileName(resolvedDir) : levelName;

            model.Seed = rootNbt.GetLong("RandomSeed", 0);
            model.GameType = rootNbt.GetInt("GameType", 0);
            model.Difficulty = rootNbt.GetInt("Difficulty", 2);
            model.IsHardcore = rootNbt.GetByte("IsHardcore", 0) == 1;
            model.StorageVersion = rootNbt.GetInt("StorageVersion", 10);
            model.InventoryVersion = rootNbt.GetString("InventoryVersion", "1.26.21");
            model.BaseGameVersion = rootNbt.GetString("baseGameVersion", "*");

            // ⏰ Time & Weather
            model.TotalTime = rootNbt.GetLong("Time", 0);
            model.DoDaylightCycle = rootNbt.GetByte("dodaylightcycle", 1) == 1;

            float rainLvl = rootNbt.Get<NbtFloat>("rainLevel")?.Value ?? 0f;
            float lightLvl = rootNbt.Get<NbtFloat>("lightningLevel")?.Value ?? 0f;

            if (lightLvl > 0.5f) model.WeatherType = 2; // Thunder
            else if (rainLvl > 0.5f) model.WeatherType = 1; // Rain
            else model.WeatherType = 0; // Clear

            model.RainTime = rootNbt.GetInt("rainTime", 40000);
            model.LightningTime = rootNbt.GetInt("lightningTime", 60000);
            model.DoWeatherCycle = rootNbt.GetByte("doweathercycle", 1) == 1;

            // ⚙️ Game Rules
            model.FallDamage = rootNbt.GetByte("falldamage", 1) == 1;
            model.FireDamage = rootNbt.GetByte("firedamage", 1) == 1;
            model.DrowningDamage = rootNbt.GetByte("drowningdamage", 1) == 1;
            model.FreezeDamage = rootNbt.GetByte("freezedamage", 1) == 1;
            model.KeepInventory = rootNbt.GetByte("keepinventory", 0) == 1;
            model.MobGriefing = rootNbt.GetByte("mobgriefing", 1) == 1;
            model.DoMobSpawning = rootNbt.GetByte("domobspawning", 1) == 1;
            model.DoMobLoot = rootNbt.GetByte("domobloot", 1) == 1;
            model.DoTileDrops = rootNbt.GetByte("dotiledrops", 1) == 1;
            model.DoEntityDrops = rootNbt.GetByte("doentitydrops", 1) == 1;
            model.NaturalRegeneration = rootNbt.GetByte("naturalregeneration", 1) == 1;
            model.Pvp = rootNbt.GetByte("pvp", 1) == 1;
            model.ShowCoordinates = rootNbt.GetByte("showcoordinates", 1) == 1;
            model.DoImmediateRespawn = rootNbt.GetByte("doimmediaterespawn", 0) == 1;
            model.TntExplodes = rootNbt.GetByte("tntexplodes", 1) == 1;
            model.RespawnBlocksExplode = rootNbt.GetByte("respawnblocksexplode", 1) == 1;
            model.ShowDaysPlayed = rootNbt.GetByte("showdaysplayed", 1) == 1;
            model.RandomTickSpeed = rootNbt.GetInt("randomtickspeed", 1);
            model.PlayersSleepingPercentage = rootNbt.GetInt("playerssleepingpercentage", 100);
            model.SpawnRadius = rootNbt.GetInt("spawnradius", 10);

            // 🏆 Cheats & Xbox Achievements
            model.CheatsEnabled = rootNbt.GetByte("cheatsEnabled", 0) == 1;
            model.CommandsEnabled = rootNbt.GetByte("commandsEnabled", 0) == 1;
            model.HasBeenLoadedInCreative = rootNbt.GetByte("hasBeenLoadedInCreative", 0) == 1;

            return (model, rootNbt, headerVersion, null);
        }
        catch (Exception ex)
        {
            return (null, null, 10, $"Gagal membaca level.dat: {ex.Message}");
        }
    }

    public static string? SaveWorldSettings(string worldFolderPath, WorldSettingsModel model, NbtCompound? existingRoot = null, int headerVersion = 10)
    {
        var resolvedDir = BedrockWorldService.ResolveWorldDirectory(worldFolderPath) ?? worldFolderPath;
        var levelDatPath = Path.Combine(resolvedDir, "level.dat");

        try
        {
            var root = existingRoot != null ? (existingRoot.Clone() as NbtCompound ?? new NbtCompound(string.Empty)) : new NbtCompound(string.Empty);

            // 🏷️ General Info
            root.SetString("LevelName", model.WorldName.Trim());
            // Seed is strictly preserved from existingRoot if available, otherwise kept from model
            if (existingRoot != null && existingRoot.ContainsKey("RandomSeed"))
            {
                root.SetLong("RandomSeed", existingRoot.GetLong("RandomSeed"));
            }
            else
            {
                root.SetLong("RandomSeed", model.Seed);
            }
            root.SetInt("GameType", model.GameType);
            root.SetInt("Difficulty", model.Difficulty);
            root.SetByte("IsHardcore", (byte)(model.IsHardcore ? 1 : 0));
            root.SetInt("StorageVersion", model.StorageVersion);
            if (!string.IsNullOrEmpty(model.InventoryVersion)) root.SetString("InventoryVersion", model.InventoryVersion);
            if (!string.IsNullOrEmpty(model.BaseGameVersion)) root.SetString("baseGameVersion", model.BaseGameVersion);

            // ⏰ Time & Weather
            root.SetLong("Time", model.TotalTime);
            root.SetByte("dodaylightcycle", (byte)(model.DoDaylightCycle ? 1 : 0));

            float rainLvl = 0f;
            float lightLvl = 0f;
            if (model.WeatherType == 1) // Rain
            {
                rainLvl = 1.0f;
            }
            else if (model.WeatherType == 2) // Thunder
            {
                rainLvl = 1.0f;
                lightLvl = 1.0f;
            }

            root.SetFloat("rainLevel", rainLvl);
            root.SetFloat("lightningLevel", lightLvl);
            root.SetInt("rainTime", model.RainTime);
            root.SetInt("lightningTime", model.LightningTime);
            root.SetByte("doweathercycle", (byte)(model.DoWeatherCycle ? 1 : 0));

            // ⚙️ Game Rules
            root.SetByte("falldamage", (byte)(model.FallDamage ? 1 : 0));
            root.SetByte("firedamage", (byte)(model.FireDamage ? 1 : 0));
            root.SetByte("drowningdamage", (byte)(model.DrowningDamage ? 1 : 0));
            root.SetByte("freezedamage", (byte)(model.FreezeDamage ? 1 : 0));
            root.SetByte("keepinventory", (byte)(model.KeepInventory ? 1 : 0));
            root.SetByte("mobgriefing", (byte)(model.MobGriefing ? 1 : 0));
            root.SetByte("domobspawning", (byte)(model.DoMobSpawning ? 1 : 0));
            root.SetByte("spawnMobs", (byte)(model.DoMobSpawning ? 1 : 0));
            root.SetByte("domobloot", (byte)(model.DoMobLoot ? 1 : 0));
            root.SetByte("dotiledrops", (byte)(model.DoTileDrops ? 1 : 0));
            root.SetByte("doentitydrops", (byte)(model.DoEntityDrops ? 1 : 0));
            root.SetByte("naturalregeneration", (byte)(model.NaturalRegeneration ? 1 : 0));
            root.SetByte("pvp", (byte)(model.Pvp ? 1 : 0));
            root.SetByte("showcoordinates", (byte)(model.ShowCoordinates ? 1 : 0));
            root.SetByte("doimmediaterespawn", (byte)(model.DoImmediateRespawn ? 1 : 0));
            root.SetByte("tntexplodes", (byte)(model.TntExplodes ? 1 : 0));
            root.SetByte("respawnblocksexplode", (byte)(model.RespawnBlocksExplode ? 1 : 0));
            root.SetByte("showdaysplayed", (byte)(model.ShowDaysPlayed ? 1 : 0));
            root.SetInt("randomtickspeed", model.RandomTickSpeed);
            root.SetInt("playerssleepingpercentage", model.PlayersSleepingPercentage);
            root.SetInt("spawnradius", model.SpawnRadius);

            // 🏆 Cheats & Xbox Achievements
            root.SetByte("cheatsEnabled", (byte)(model.CheatsEnabled ? 1 : 0));
            root.SetByte("commandsEnabled", (byte)(model.CommandsEnabled ? 1 : 0));
            root.SetByte("hasBeenLoadedInCreative", (byte)(model.HasBeenLoadedInCreative ? 1 : 0));

            // Write NBT payload
            byte[] nbtPayload = BedrockNbtWriter.WriteToBytes(root);

            // 8-byte Little-Endian Header: [HeaderVersion: Int32][PayloadLength: Int32]
            byte[] fileBytes = new byte[8 + nbtPayload.Length];
            BitConverter.GetBytes(headerVersion).CopyTo(fileBytes, 0);
            BitConverter.GetBytes(nbtPayload.Length).CopyTo(fileBytes, 4);
            nbtPayload.CopyTo(fileBytes, 8);

            // Backup existing level.dat to level.dat_old
            if (File.Exists(levelDatPath))
            {
                var oldPath = Path.Combine(resolvedDir, "level.dat_old");
                try
                {
                    File.Copy(levelDatPath, oldPath, overwrite: true);
                }
                catch { }
            }

            File.WriteAllBytes(levelDatPath, fileBytes);

            // Update levelname.txt
            var levelNameTxtPath = Path.Combine(resolvedDir, "levelname.txt");
            try
            {
                File.WriteAllText(levelNameTxtPath, model.WorldName.Trim());
            }
            catch { }

            return null;
        }
        catch (Exception ex)
        {
            return $"Gagal menyimpan level.dat: {ex.Message}";
        }
    }
}
