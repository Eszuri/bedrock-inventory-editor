using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LevelDB;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Storage;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class WorldContainerStorageTests : IDisposable
{
    private readonly string _testWorldDir;

    public WorldContainerStorageTests()
    {
        _testWorldDir = Path.Combine(Path.GetTempPath(), "BIE_TestWorld_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testWorldDir);
        Directory.CreateDirectory(Path.Combine(_testWorldDir, "db"));
        File.WriteAllText(Path.Combine(_testWorldDir, "levelname.txt"), "Test World Container Suite");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testWorldDir))
            {
                Directory.Delete(_testWorldDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void FullContainerLifecycle_LoadEditSaveReload_Succeeds()
    {
        string dbPath = Path.Combine(_testWorldDir, "db");
        var options = new Options { CreateIfMissing = true };

        // 1. Create player NBT
        var playerNbt = new NbtCompound();
        var posList = new NbtList("Pos", NbtTagType.Float);
        posList.Add(new NbtFloat("", 100.0f));
        posList.Add(new NbtFloat("", 64.0f));
        posList.Add(new NbtFloat("", -200.0f));
        playerNbt.Set(posList);
        playerNbt.SetInt("DimensionId", 0);

        // 2. Create Block Entities for Tag 49 in Chunk (6, -13)
        int chunkX = 6;
        int chunkZ = -13;
        int dimId = 0;

        var blockEntities = new List<NbtCompound>();

        // Single Chest
        var chest = new NbtCompound();
        chest.SetString("id", "Chest");
        chest.SetInt("x", 102);
        chest.SetInt("y", 64);
        chest.SetInt("z", -200);
        chest.SetString("CustomName", "Peti Uji Coba");
        var chestItems = new NbtList("Items", NbtTagType.Compound);
        var dia = new NbtCompound();
        dia.SetByte("Slot", 0);
        dia.SetString("Name", "minecraft:diamond");
        dia.SetByte("Count", 64);
        chestItems.Add(dia);
        chest.Set(chestItems);
        blockEntities.Add(chest);

        // Double Chest Lead & Partner
        var doubleLead = new NbtCompound();
        doubleLead.SetString("id", "Chest");
        doubleLead.SetInt("x", 105);
        doubleLead.SetInt("y", 64);
        doubleLead.SetInt("z", -200);
        doubleLead.SetByte("pairlead", 1);
        doubleLead.SetInt("pairx", 106);
        doubleLead.SetInt("pairz", -200);
        var leadItems = new NbtList("Items", NbtTagType.Compound);
        var elytra = new NbtCompound();
        elytra.SetByte("Slot", 0);
        elytra.SetString("Name", "minecraft:elytra");
        elytra.SetByte("Count", 1);
        leadItems.Add(elytra);
        doubleLead.Set(leadItems);
        blockEntities.Add(doubleLead);

        var doubleSec = new NbtCompound();
        doubleSec.SetString("id", "Chest");
        doubleSec.SetInt("x", 106);
        doubleSec.SetInt("y", 64);
        doubleSec.SetInt("z", -200);
        doubleSec.SetByte("pairlead", 0);
        doubleSec.SetInt("pairx", 105);
        doubleSec.SetInt("pairz", -200);
        var secItems = new NbtList("Items", NbtTagType.Compound);
        var totem = new NbtCompound();
        totem.SetByte("Slot", 0);
        totem.SetString("Name", "minecraft:totem_of_undying");
        totem.SetByte("Count", 1);
        secItems.Add(totem);
        doubleSec.Set(secItems);
        blockEntities.Add(doubleSec);

        // Barrel
        var barrel = new NbtCompound();
        barrel.SetString("id", "Barrel");
        barrel.SetInt("x", 101);
        barrel.SetInt("y", 64);
        barrel.SetInt("z", -195);
        var barrelItems = new NbtList("Items", NbtTagType.Compound);
        var bread = new NbtCompound();
        bread.SetByte("Slot", 0);
        bread.SetString("Name", "minecraft:bread");
        bread.SetByte("Count", 32);
        barrelItems.Add(bread);
        barrel.Set(barrelItems);
        blockEntities.Add(barrel);

        // Write initial data to LevelDB
        using (var db = new DB(options, dbPath))
        {
            var writeOpts = new WriteOptions { Sync = true };
            db.Put(Encoding.UTF8.GetBytes("~local_player"), BedrockNbtWriter.WriteToBytes(playerNbt, true), writeOpts);

            var chunkKey = BedrockWorldService.GetBlockEntityChunkKey(chunkX, chunkZ, dimId);
            db.Put(chunkKey, BedrockNbtWriter.WriteMultipleCompounds(blockEntities), writeOpts);
        }

        // 3. Test Loading
        var (loadedPlayerNbt, _, _, pErr) = BedrockWorldService.LoadPlayerNbt(_testWorldDir, "~local_player");
        Assert.Null(pErr);
        Assert.NotNull(loadedPlayerNbt);

        var (px, py, pz, pDim) = BedrockWorldService.GetPlayerPosition(loadedPlayerNbt);
        Assert.Equal(100.0, px, precision: 1);
        Assert.Equal(64.0, py, precision: 1);
        Assert.Equal(-200.0, pz, precision: 1);

        var (containers, contErr) = BedrockWorldService.LoadNearbyContainers(_testWorldDir, px, py, pz, pDim, maxRadius: 64);
        Assert.Null(contErr);
        Assert.Equal(3, containers.Count); // Single chest, merged Double chest, Barrel

        var singleChest = containers.FirstOrDefault(c => c.TypeId == "Chest" && !c.IsDoubleChest);
        Assert.NotNull(singleChest);
        Assert.Equal("Peti Uji Coba", singleChest.DisplayName);
        Assert.Equal("minecraft:diamond", singleChest.Slots[0].Id);
        Assert.Equal(64, singleChest.Slots[0].Count);

        var doubleChest = containers.FirstOrDefault(c => c.IsDoubleChest);
        Assert.NotNull(doubleChest);
        Assert.Equal(54, doubleChest.TotalSlots);
        Assert.Equal("minecraft:elytra", doubleChest.Slots[0].Id);
        Assert.Equal("minecraft:totem_of_undying", doubleChest.Slots[27].Id);

        // 4. Test Editing & Saving
        singleChest.Slots[1].Id = "minecraft:netherite_ingot";
        singleChest.Slots[1].Count = 16;

        doubleChest.Slots[27].Id = "minecraft:mace";
        doubleChest.Slots[27].Count = 1;

        var (saveOk, _, saveErr) = BedrockWorldService.SaveBlockEntityContainers(_testWorldDir, containers, createBackup: false);
        Assert.True(saveOk);
        Assert.Null(saveErr);

        // 5. Test Reload & Verification
        var (reloaded, _) = BedrockWorldService.LoadNearbyContainers(_testWorldDir, px, py, pz, pDim, maxRadius: 64);
        var reloadedSingle = reloaded.First(c => c.TypeId == "Chest" && !c.IsDoubleChest);
        Assert.Equal("minecraft:netherite_ingot", reloadedSingle.Slots[1].Id);
        Assert.Equal(16, reloadedSingle.Slots[1].Count);

        var reloadedDouble = reloaded.First(c => c.IsDoubleChest);
        Assert.Equal("minecraft:elytra", reloadedDouble.Slots[0].Id);
        Assert.Equal("minecraft:mace", reloadedDouble.Slots[27].Id);
    }
}
