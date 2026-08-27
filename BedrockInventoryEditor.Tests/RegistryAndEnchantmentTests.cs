using System;
using System.Linq;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Registry;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class RegistryAndEnchantmentTests
{
    [Fact]
    public void ItemRegistry_IsPopulated_AndSortedAlphabetically()
    {
        Assert.True(BedrockItemRegistry.Items.Count > 1000, "Item registry should have over 1000 items");

        // Verify alphabetical sorting A-Z by DisplayName
        for (int i = 0; i < BedrockItemRegistry.Items.Count - 1; i++)
        {
            var curr = BedrockItemRegistry.Items[i].DisplayName;
            var next = BedrockItemRegistry.Items[i + 1].DisplayName;
            Assert.True(string.Compare(curr, next, StringComparison.OrdinalIgnoreCase) <= 0, 
                $"Registry is not sorted: '{curr}' should precede '{next}'");
        }
    }

    [Fact]
    public void Elytra_OnlyGetsUnbreakingAndMending()
    {
        var enchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:elytra");
        
        Assert.Equal(2, enchants.Count);
        Assert.Contains(enchants, e => e.Info.Name == "Unbreaking" && e.Level == 3);
        Assert.Contains(enchants, e => e.Info.Name == "Mending" && e.Level == 1);
    }

    [Fact]
    public void Sword_GetsValidWeaponEnchantments()
    {
        var enchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_sword");
        
        Assert.Contains(enchants, e => e.Info.Name == "Sharpness" && e.Level == 5);
        Assert.Contains(enchants, e => e.Info.Name == "Fire Aspect" && e.Level == 2);
        Assert.Contains(enchants, e => e.Info.Name == "Looting" && e.Level == 3);
        Assert.Contains(enchants, e => e.Info.Name == "Unbreaking" && e.Level == 3);
        Assert.Contains(enchants, e => e.Info.Name == "Mending" && e.Level == 1);

        // Sword should NOT get Infinity or Protection
        Assert.DoesNotContain(enchants, e => e.Info.Name == "Infinity");
        Assert.DoesNotContain(enchants, e => e.Info.Name == "Protection");
    }

    [Fact]
    public void NonEnchantableItem_ReturnsEmptyList()
    {
        var enchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:apple");
        Assert.Empty(enchants);

        var dirtEnchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:dirt");
        Assert.Empty(dirtEnchants);
    }

    [Fact]
    public void EnchantmentEntry_DiffStatus_New_Modified_Unchanged()
    {
        // 1. Unchanged
        var entryUnchanged = new EnchantmentEntry(9, "Sharpness", 5, 5);
        Assert.Equal(EnchantmentDiffStatus.Unchanged, entryUnchanged.DiffStatus);
        Assert.True(entryUnchanged.IsUnchanged);
        Assert.False(entryUnchanged.IsNew);
        Assert.False(entryUnchanged.HasLevelChange);

        // 2. New
        var entryNew = new EnchantmentEntry(17, "Unbreaking", 3, null);
        Assert.Equal(EnchantmentDiffStatus.New, entryNew.DiffStatus);
        Assert.True(entryNew.IsNew);
        Assert.Equal("+ BARU", entryNew.ChangeTag);

        // 3. Modified Level
        var entryModified = new EnchantmentEntry(9, "Sharpness", 5, 2);
        Assert.Equal(EnchantmentDiffStatus.Modified, entryModified.DiffStatus);
        Assert.True(entryModified.HasLevelChange);
        Assert.Equal("Lvl 2 ➔ Lvl 5", entryModified.LevelChangeText);
        Assert.Equal("Sebelumnya: Lvl 2", entryModified.ChangeTag);
    }
}
