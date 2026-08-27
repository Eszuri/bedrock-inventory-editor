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
        // 1. Sword with Sharpness gets Sharpness V + non-conflicting
        var enchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_sword", new short[] { 9 });
        Assert.Contains(enchants, e => e.Info.Name == "Sharpness" && e.Level == 5);
        Assert.Contains(enchants, e => e.Info.Name == "Fire Aspect" && e.Level == 2);
        Assert.Contains(enchants, e => e.Info.Name == "Looting" && e.Level == 3);
        Assert.Contains(enchants, e => e.Info.Name == "Unbreaking" && e.Level == 3);
        Assert.Contains(enchants, e => e.Info.Name == "Mending" && e.Level == 1);

        // 2. Fresh Sword (neither Sharpness nor Smite nor Bane) does NOT get either damage enchant
        var freshEnchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_sword");
        Assert.DoesNotContain(freshEnchants, e => e.Info.Name == "Sharpness");
        Assert.DoesNotContain(freshEnchants, e => e.Info.Name == "Smite");
        Assert.DoesNotContain(freshEnchants, e => e.Info.Name == "Bane of Arthropods");
        Assert.Contains(freshEnchants, e => e.Info.Name == "Fire Aspect");
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
    public void ShovelAndAxe_EnforceMutualExclusivity_NoFortuneAndSilkTouchConflict()
    {
        // 1. Fresh Shovel gets NEITHER Fortune nor Silk Touch (neither was present)
        var shovelEnchants = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_shovel");
        Assert.DoesNotContain(shovelEnchants, e => e.Info.Name == "Fortune");
        Assert.DoesNotContain(shovelEnchants, e => e.Info.Name == "Silk Touch");
        Assert.Contains(shovelEnchants, e => e.Info.Name == "Efficiency" && e.Level == 5);
        Assert.Contains(shovelEnchants, e => e.Info.Name == "Unbreaking" && e.Level == 3);
        Assert.Contains(shovelEnchants, e => e.Info.Name == "Mending" && e.Level == 1);

        // 2. Shovel with Fortune gets Fortune III and NOT Silk Touch
        var shovelWithFortune = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_shovel", new short[] { 18 });
        Assert.Contains(shovelWithFortune, e => e.Info.Name == "Fortune" && e.Level == 3);
        Assert.DoesNotContain(shovelWithFortune, e => e.Info.Name == "Silk Touch");

        // 3. Shovel with Silk Touch gets Silk Touch I and NOT Fortune
        var shovelWithSilk = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_shovel", new short[] { 16 });
        Assert.Contains(shovelWithSilk, e => e.Info.Name == "Silk Touch" && e.Level == 1);
        Assert.DoesNotContain(shovelWithSilk, e => e.Info.Name == "Fortune");

        // 4. Axe with Smite gets Smite V and NOT Sharpness or Bane
        var axeWithSmite = BedrockEnchantments.GetCompatibleEnchantments("minecraft:diamond_axe", new short[] { 10 });
        Assert.Contains(axeWithSmite, e => e.Info.Name == "Smite" && e.Level == 5);
        Assert.DoesNotContain(axeWithSmite, e => e.Info.Name == "Sharpness");
        Assert.DoesNotContain(axeWithSmite, e => e.Info.Name == "Bane of Arthropods");
    }

    [Fact]
    public void Bow_Crossbow_Trident_Mace_EnforceMutualExclusivity()
    {
        // 1. Bow: Fresh gets neither Infinity nor Mending
        var freshBow = BedrockEnchantments.GetCompatibleEnchantments("minecraft:bow");
        Assert.DoesNotContain(freshBow, e => e.Info.Name == "Infinity");
        Assert.DoesNotContain(freshBow, e => e.Info.Name == "Mending");
        Assert.Contains(freshBow, e => e.Info.Name == "Power" && e.Level == 5);

        // Bow with Infinity gets Infinity and NOT Mending
        var infBow = BedrockEnchantments.GetCompatibleEnchantments("minecraft:bow", new short[] { 22 });
        Assert.Contains(infBow, e => e.Info.Name == "Infinity" && e.Level == 1);
        Assert.DoesNotContain(infBow, e => e.Info.Name == "Mending");

        // Bow with Mending gets Mending and NOT Infinity
        var mendBow = BedrockEnchantments.GetCompatibleEnchantments("minecraft:bow", new short[] { 26 });
        Assert.Contains(mendBow, e => e.Info.Name == "Mending" && e.Level == 1);
        Assert.DoesNotContain(mendBow, e => e.Info.Name == "Infinity");

        // 2. Trident: Riptide excludes Loyalty & Channeling
        var ripTrident = BedrockEnchantments.GetCompatibleEnchantments("minecraft:trident", new short[] { 30 });
        Assert.Contains(ripTrident, e => e.Info.Name == "Riptide" && e.Level == 3);
        Assert.DoesNotContain(ripTrident, e => e.Info.Name == "Loyalty");
        Assert.DoesNotContain(ripTrident, e => e.Info.Name == "Channeling");

        // Trident with Loyalty gets Loyalty & Channeling and NOT Riptide
        var loyTrident = BedrockEnchantments.GetCompatibleEnchantments("minecraft:trident", new short[] { 31 });
        Assert.Contains(loyTrident, e => e.Info.Name == "Loyalty" && e.Level == 3);
        Assert.Contains(loyTrident, e => e.Info.Name == "Channeling" && e.Level == 1);
        Assert.DoesNotContain(loyTrident, e => e.Info.Name == "Riptide");

        // 3. Crossbow: Multishot vs Piercing
        var pierceXbow = BedrockEnchantments.GetCompatibleEnchantments("minecraft:crossbow", new short[] { 34 });
        Assert.Contains(pierceXbow, e => e.Info.Name == "Piercing" && e.Level == 4);
        Assert.DoesNotContain(pierceXbow, e => e.Info.Name == "Multishot");

        // 4. Mace: Density vs Breach
        var breachMace = BedrockEnchantments.GetCompatibleEnchantments("minecraft:mace", new short[] { 40 });
        Assert.Contains(breachMace, e => e.Info.Name == "Breach" && e.Level == 4);
        Assert.DoesNotContain(breachMace, e => e.Info.Name == "Density");
    }

    [Fact]
    public void ItemStack_ToNbt_NeverWritesDamageInsideInnerTag()
    {
        var item = new ItemStack(0, SlotLocation.MainBag)
        {
            Id = "minecraft:diamond_axe",
            Count = 1,
            Damage = 50
        };

        // Put a fake damage in ExtraNbt to simulate old corrupt state
        item.ExtraNbt = new BedrockInventoryEditor.Core.Nbt.NbtCompound("tag");
        item.ExtraNbt.SetShort("Damage", 50);

        var nbt = item.ToNbt();
        Assert.Equal((short)50, nbt.GetShort("Damage"));

        var innerTag = nbt.GetCompound("tag");
        if (innerTag != null)
        {
            Assert.False(innerTag.ContainsKey("Damage"), "Damage should NEVER be written inside inner tag!");
            Assert.False(innerTag.ContainsKey("damage"), "damage should NEVER be written inside inner tag!");
        }
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
