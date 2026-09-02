using System;
using System.IO;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Storage;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class WorldSettingsTests
{
    [Fact]
    public void WorldSettingsModel_TotalTimeConversion_CalculatesDaysAndTicksCorrectly()
    {
        var model = new WorldSettingsModel();

        // 1. Set total time = 126000 (Day 5, 6000 ticks / 12:00 Siang)
        model.TotalTime = 126000;
        Assert.Equal(5, model.DayCount);
        Assert.Equal(6000, model.TimeOfDay);
        Assert.Equal("12:00", model.FormattedTimeOfDay);

        // 2. Set day = 10, timeOfDay = 18000 (00:00 Midnight)
        model.DayCount = 10;
        model.TimeOfDay = 18000;
        Assert.Equal(258000, model.TotalTime);
        Assert.Equal("00:00", model.FormattedTimeOfDay);

        // 3. Set 0 ticks (06:00 Sunrise)
        model.TimeOfDay = 0;
        Assert.Equal("06:00", model.FormattedTimeOfDay);
    }

    [Fact]
    public void BedrockLevelDatService_LoadAndSave_RoundtripsSuccessfullyWithHeaderAndOldBackup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "BedrockTestWorld_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Create dummy initial level.dat
            var initialRoot = new NbtCompound(string.Empty);
            initialRoot.SetString("LevelName", "Original World Name");
            initialRoot.SetLong("RandomSeed", 1234567890L);
            initialRoot.SetInt("GameType", 0); // Survival
            initialRoot.SetInt("Difficulty", 2); // Normal
            initialRoot.SetByte("IsHardcore", 0);
            initialRoot.SetLong("Time", 48000L); // Day 2, 0 ticks
            initialRoot.SetByte("dodaylightcycle", 1);
            initialRoot.SetByte("falldamage", 1);
            initialRoot.SetByte("keepinventory", 0);
            initialRoot.SetByte("mobgriefing", 1);
            initialRoot.SetByte("cheatsEnabled", 1);
            initialRoot.SetByte("hasBeenLoadedInCreative", 1); // Locked achievements

            var nbtBytes = BedrockNbtWriter.WriteToBytes(initialRoot);
            var fileBytes = new byte[8 + nbtBytes.Length];
            BitConverter.GetBytes(10).CopyTo(fileBytes, 0); // HeaderVersion = 10
            BitConverter.GetBytes(nbtBytes.Length).CopyTo(fileBytes, 4);
            nbtBytes.CopyTo(fileBytes, 8);

            var levelDatPath = Path.Combine(tempDir, "level.dat");
            File.WriteAllBytes(levelDatPath, fileBytes);
            File.WriteAllText(Path.Combine(tempDir, "levelname.txt"), "Original World Name");

            // 2. Load with BedrockLevelDatService
            var (loadedModel, loadedRawNbt, headerVer, loadErr) = BedrockLevelDatService.LoadWorldSettings(tempDir);
            Assert.Null(loadErr);
            Assert.NotNull(loadedModel);
            Assert.NotNull(loadedRawNbt);
            Assert.Equal(10, headerVer);
            Assert.Equal("Original World Name", loadedModel.WorldName);
            Assert.Equal(1234567890L, loadedModel.Seed);
            Assert.True(loadedModel.FallDamage);
            Assert.False(loadedModel.KeepInventory);
            Assert.True(loadedModel.CheatsEnabled);
            Assert.True(loadedModel.HasBeenLoadedInCreative);
            Assert.True(loadedModel.ProjectilesCanBreakBlocks);
            Assert.False(loadedModel.RecipesUnlock);
            Assert.True(loadedModel.DoFireTick);

            // 3. Modify settings
            loadedModel.WorldName = "Super Survival World";
            loadedModel.KeepInventory = true;
            loadedModel.FallDamage = false;
            loadedModel.CheatsEnabled = false;
            loadedModel.CommandsEnabled = false;
            loadedModel.HasBeenLoadedInCreative = false; // Restore Xbox Achievements
            loadedModel.ProjectilesCanBreakBlocks = false;
            loadedModel.RecipesUnlock = true;
            loadedModel.DoFireTick = false;
            loadedModel.CommandBlockOutput = false;
            loadedModel.SendCommandFeedback = false;
            loadedModel.ShowDeathMessages = false;
            loadedModel.ShowTags = false;
            loadedModel.ShowBorderEffect = false;
            loadedModel.DayCount = 100;
            loadedModel.TimeOfDay = 6000; // 12:00
            loadedModel.WeatherType = 0; // Clear

            // 4. Save with BedrockLevelDatService
            var saveErr = BedrockLevelDatService.SaveWorldSettings(tempDir, loadedModel, loadedRawNbt, headerVer);
            Assert.Null(saveErr);

            // 5. Verify level.dat_old backup exists
            var backupPath = Path.Combine(tempDir, "level.dat_old");
            Assert.True(File.Exists(backupPath), "Backup level.dat_old harus dibuat otomatis.");

            // 6. Reload from disk and verify persistence
            var (reloadedModel, _, reloadedHeader, reloadErr) = BedrockLevelDatService.LoadWorldSettings(tempDir);
            Assert.Null(reloadErr);
            Assert.NotNull(reloadedModel);
            Assert.Equal(10, reloadedHeader);
            Assert.Equal("Super Survival World", reloadedModel.WorldName);
            Assert.True(reloadedModel.KeepInventory);
            Assert.False(reloadedModel.FallDamage);
            Assert.False(reloadedModel.CheatsEnabled);
            Assert.False(reloadedModel.HasBeenLoadedInCreative);
            Assert.False(reloadedModel.ProjectilesCanBreakBlocks);
            Assert.True(reloadedModel.RecipesUnlock);
            Assert.False(reloadedModel.DoFireTick);
            Assert.False(reloadedModel.CommandBlockOutput);
            Assert.False(reloadedModel.SendCommandFeedback);
            Assert.False(reloadedModel.ShowDeathMessages);
            Assert.False(reloadedModel.ShowTags);
            Assert.False(reloadedModel.ShowBorderEffect);
            Assert.Equal(100, reloadedModel.DayCount);
            Assert.Equal(6000, reloadedModel.TimeOfDay);

            // Verify levelname.txt updated
            var txtContent = File.ReadAllText(Path.Combine(tempDir, "levelname.txt")).Trim();
            Assert.Equal("Super Survival World", txtContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public void BedrockWorldService_ExtractAndUpdatePlayerStats_RoundtripsAccurately()
    {
        var playerNbt = new NbtCompound(string.Empty);
        var attrs = new NbtList("Attributes", NbtTagType.Compound);

        var healthComp = new NbtCompound();
        healthComp.SetString("Name", "minecraft:health");
        healthComp.SetFloat("Current", 15.5f);
        healthComp.SetFloat("Base", 20f);
        healthComp.SetFloat("Max", 20f);
        attrs.Add(healthComp);

        var hungerComp = new NbtCompound();
        hungerComp.SetString("Name", "minecraft:player.hunger");
        hungerComp.SetFloat("Current", 18f);
        hungerComp.SetFloat("Base", 20f);
        hungerComp.SetFloat("Max", 20f);
        attrs.Add(hungerComp);

        var satComp = new NbtCompound();
        satComp.SetString("Name", "minecraft:player.saturation");
        satComp.SetFloat("Current", 10f);
        satComp.SetFloat("Base", 5f);
        satComp.SetFloat("Max", 20f);
        attrs.Add(satComp);

        var xpLvlComp = new NbtCompound();
        xpLvlComp.SetString("Name", "minecraft:player.level");
        xpLvlComp.SetFloat("Current", 30f);
        xpLvlComp.SetFloat("Base", 30f);
        attrs.Add(xpLvlComp);

        var xpProgComp = new NbtCompound();
        xpProgComp.SetString("Name", "minecraft:player.experience");
        xpProgComp.SetFloat("Current", 0.5f);
        xpProgComp.SetFloat("Base", 0.5f);
        attrs.Add(xpProgComp);

        playerNbt.Set(attrs);
        playerNbt.SetInt("PlayerLevel", 30);
        playerNbt.SetFloat("PlayerLevelProgress", 0.5f);
        playerNbt.SetInt("Dimension", 0);

        var posList = new NbtList("Pos", NbtTagType.Float)
        {
            new NbtFloat(string.Empty, 120.5f),
            new NbtFloat(string.Empty, 64f),
            new NbtFloat(string.Empty, -350.25f)
        };
        playerNbt.Set(posList);

        // 1. Extract to WorldSettingsModel
        var model = new WorldSettingsModel();
        BedrockWorldService.ExtractPlayerStats(playerNbt, model);

        Assert.Equal(15.5f, model.Health);
        Assert.Equal(18f, model.Hunger);
        Assert.Equal(10f, model.Saturation);
        Assert.Equal(30, model.XpLevel);
        Assert.Equal(0.5f, model.XpProgress);
        Assert.Equal(0, model.Dimension);
        Assert.Equal(120.5, model.PosX, 1);
        Assert.Equal(64, model.PosY, 1);
        Assert.Equal(-350.25, model.PosZ, 1);

        // 2. Modify in Model (Max Buffs)
        model.Health = 40f;
        model.MaxHealth = 40f;
        model.Hunger = 20f;
        model.Saturation = 20f;
        model.XpLevel = 100;
        model.XpProgress = 0.85f;
        model.Dimension = 1; // Nether
        model.PosX = 500;
        model.PosY = 75;
        model.PosZ = -100;

        // 3. Update back to Player NBT
        BedrockWorldService.UpdatePlayerStats(playerNbt, model);

        Assert.Equal(100, playerNbt.GetInt("PlayerLevel"));
        Assert.Equal(0.85f, playerNbt.Get<NbtFloat>("PlayerLevelProgress")?.Value ?? 0f, 2);
        Assert.Equal(1, playerNbt.GetInt("Dimension"));

        var updatedAttrs = playerNbt.GetList("Attributes")!;
        var updatedHealth = updatedAttrs.OfType<NbtCompound>().First(a => a.GetString("Name") == "minecraft:health");
        Assert.Equal(40f, updatedHealth.Get<NbtFloat>("Current")?.Value);
        Assert.Equal(40f, updatedHealth.Get<NbtFloat>("Max")?.Value);

        var updatedPos = playerNbt.GetList("Pos")!;
        Assert.Equal(500f, (updatedPos[0] as NbtFloat)?.Value);
        Assert.Equal(75f, (updatedPos[1] as NbtFloat)?.Value);
        Assert.Equal(-100f, (updatedPos[2] as NbtFloat)?.Value);
    }
}
