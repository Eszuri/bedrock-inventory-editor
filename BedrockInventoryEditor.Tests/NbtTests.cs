using System;
using System.Collections.Generic;
using System.Text;
using BedrockInventoryEditor.Core.Nbt;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class NbtTests
{
    [Fact]
    public void ReadWrite_SingleCompound_PreservesData()
    {
        var root = new NbtCompound("TestRoot");
        root.SetString("Name", "minecraft:diamond_sword");
        root.SetShort("Damage", 15);
        root.SetByte("Count", 1);

        var innerTag = new NbtCompound("tag");
        innerTag.SetInt("RepairCost", 2);
        root.Set(innerTag);

        var bytes = BedrockNbtWriter.WriteToBytes(root, includeRootHeader: true);
        var reloaded = BedrockNbtReader.ReadFromBytes(bytes, out var hasHeader);

        Assert.True(hasHeader);
        Assert.Equal("minecraft:diamond_sword", reloaded.GetString("Name"));
        Assert.Equal(15, reloaded.GetShort("Damage"));
        Assert.Equal(1, reloaded.GetByte("Count"));
        Assert.NotNull(reloaded.GetCompound("tag"));
        Assert.Equal(2, reloaded.GetCompound("tag")!.GetInt("RepairCost"));
    }

    [Fact]
    public void ReadWrite_MultipleCompounds_Tag49Style_RoundtripsSuccessfully()
    {
        var compounds = new List<NbtCompound>();

        var chest = new NbtCompound();
        chest.SetString("id", "Chest");
        chest.SetInt("x", 100);
        chest.SetInt("y", 64);
        chest.SetInt("z", -200);
        chest.SetString("CustomName", "My Chest");
        compounds.Add(chest);

        var barrel = new NbtCompound();
        barrel.SetString("id", "Barrel");
        barrel.SetInt("x", 101);
        barrel.SetInt("y", 64);
        barrel.SetInt("z", -200);
        compounds.Add(barrel);

        var bytes = BedrockNbtWriter.WriteMultipleCompounds(compounds, includeRootHeader: true);
        var loadedList = BedrockNbtReader.ReadMultipleCompounds(bytes);

        Assert.Equal(2, loadedList.Count);
        Assert.Equal("Chest", loadedList[0].GetString("id"));
        Assert.Equal("My Chest", loadedList[0].GetString("CustomName"));
        Assert.Equal(100, loadedList[0].GetInt("x"));

        Assert.Equal("Barrel", loadedList[1].GetString("id"));
        Assert.Equal(101, loadedList[1].GetInt("x"));
    }
}
