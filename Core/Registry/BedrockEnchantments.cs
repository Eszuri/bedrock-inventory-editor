using System.Collections.Generic;

namespace BedrockInventoryEditor.Core.Registry;

public record EnchantmentInfo(short Id, string Name, string MaxVanillaLevel);

public static class BedrockEnchantments
{
    public static readonly List<EnchantmentInfo> All = new()
    {
        new(0, "Protection", "IV"),
        new(1, "Fire Protection", "IV"),
        new(2, "Feather Falling", "IV"),
        new(3, "Blast Protection", "IV"),
        new(4, "Projectile Protection", "IV"),
        new(5, "Thorns", "III"),
        new(6, "Respiration", "III"),
        new(7, "Depth Strider", "III"),
        new(8, "Aqua Affinity", "I"),
        new(9, "Sharpness", "V"),
        new(10, "Smite", "V"),
        new(11, "Bane of Arthropods", "V"),
        new(12, "Knockback", "II"),
        new(13, "Fire Aspect", "II"),
        new(14, "Looting", "III"),
        new(15, "Efficiency", "V"),
        new(16, "Silk Touch", "I"),
        new(17, "Unbreaking", "III"),
        new(18, "Fortune", "III"),
        new(19, "Power", "V"),
        new(20, "Punch", "II"),
        new(21, "Flame", "I"),
        new(22, "Infinity", "I"),
        new(23, "Luck of the Sea", "III"),
        new(24, "Lure", "III"),
        new(25, "Frost Walker", "II"),
        new(26, "Mending", "I"),
        new(27, "Curse of Binding", "I"),
        new(28, "Curse of Vanishing", "I"),
        new(29, "Impaling", "V"),
        new(30, "Riptide", "III"),
        new(31, "Loyalty", "III"),
        new(32, "Channeling", "I"),
        new(33, "Multishot", "I"),
        new(34, "Piercing", "IV"),
        new(35, "Quick Charge", "III"),
        new(36, "Soul Speed", "III"),
        new(37, "Swift Sneak", "III"),
        new(38, "Wind Burst", "III"),
        new(39, "Density", "V"),
        new(40, "Breach", "IV")
    };

    private static readonly Dictionary<short, string> NameMap = new();

    static BedrockEnchantments()
    {
        foreach (var item in All)
        {
            NameMap[item.Id] = item.Name;
        }
    }

    public static string GetName(short id)
    {
        return NameMap.TryGetValue(id, out var name) ? name : $"Unknown Enchant ({id})";
    }

    public static readonly Dictionary<short, short[]> IncompatibleGroups = new()
    {
        // 1. Tool Harvesting: Fortune (18) vs Silk Touch (16)
        { 16, new short[] { 18 } },
        { 18, new short[] { 16 } },

        // 2. Weapon Damage: Sharpness (9) vs Smite (10) vs Bane (11)
        { 9, new short[] { 10, 11 } },
        { 10, new short[] { 9, 11 } },
        { 11, new short[] { 9, 10 } },

        // 3. Armor Protection: Prot (0) vs Fire Prot (1) vs Blast Prot (3) vs Proj Prot (4)
        { 0, new short[] { 1, 3, 4 } },
        { 1, new short[] { 0, 3, 4 } },
        { 3, new short[] { 0, 1, 4 } },
        { 4, new short[] { 0, 1, 3 } },

        // 4. Boots Water Movement: Depth Strider (7) vs Frost Walker (25)
        { 7, new short[] { 25 } },
        { 25, new short[] { 7 } },

        // 5. Bow: Infinity (22) vs Mending (26)
        { 22, new short[] { 26 } },
        { 26, new short[] { 22 } },

        // 6. Crossbow: Multishot (33) vs Piercing (34)
        { 33, new short[] { 34 } },
        { 34, new short[] { 33 } },

        // 7. Trident: Riptide (30) vs Loyalty (31) / Channeling (32)
        { 30, new short[] { 31, 32 } },
        { 31, new short[] { 30 } },
        { 32, new short[] { 30 } },

        // 8. Mace: Density (39) vs Breach (40) vs Smite (10) vs Bane (11)
        { 39, new short[] { 40, 10, 11 } },
        { 40, new short[] { 39, 10, 11 } }
    };

    public static short[] GetIncompatibleEnchantmentIds(short enchantId, string? itemId = null)
    {
        var clean = string.IsNullOrWhiteSpace(itemId) ? "" : (itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..].ToLowerInvariant() : itemId.ToLowerInvariant());

        // Mending (26) and Infinity (22) are only mutually exclusive on Bows
        if ((enchantId == 26 || enchantId == 22) && clean != "bow")
        {
            return System.Array.Empty<short>();
        }

        return IncompatibleGroups.TryGetValue(enchantId, out var incomp) ? incomp : System.Array.Empty<short>();
    }

    public static List<(EnchantmentInfo Info, short Level)> GetCompatibleEnchantments(string? itemId, IEnumerable<short>? currentEnchantIds = null)
    {
        var result = new List<(EnchantmentInfo Info, short Level)>();
        if (string.IsNullOrWhiteSpace(itemId) || itemId == "minecraft:air") return result;

        var clean = itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..].ToLowerInvariant() : itemId.ToLowerInvariant();
        var existing = new HashSet<short>(currentEnchantIds ?? System.Linq.Enumerable.Empty<short>());

        void Add(short id, short lvl)
        {
            var info = All.Find(e => e.Id == id);
            if (info != null)
            {
                result.Add((info, lvl));
            }
        }

        // Helper: Damage enchants (Sharpness 9, Smite 10, Bane 11)
        // If one is present, keep and max it. If none is present, DO NOT add either!
        void AddWeaponDamage()
        {
            if (existing.Contains(9)) Add(9, 5);
            else if (existing.Contains(10)) Add(10, 5);
            else if (existing.Contains(11)) Add(11, 5);
        }

        // Helper: Armor protection enchants (Prot 0, Fire Prot 1, Blast Prot 3, Proj Prot 4)
        // If one is present, keep and max it. If none is present, DO NOT add either!
        void AddArmorProtection()
        {
            if (existing.Contains(0)) Add(0, 4);
            else if (existing.Contains(1)) Add(1, 4);
            else if (existing.Contains(3)) Add(3, 4);
            else if (existing.Contains(4)) Add(4, 4);
        }

        // Helper: Tool harvesting enchants (Fortune 18 vs Silk Touch 16)
        // If one is present, keep and max it. If none is present, DO NOT add either!
        void AddToolHarvesting()
        {
            if (existing.Contains(18)) Add(18, 3);
            else if (existing.Contains(16)) Add(16, 1);
        }

        // 1. Helmet
        if (clean.EndsWith("_helmet") || clean == "turtle_helmet")
        {
            AddArmorProtection();
            Add(5, 3);  // Thorns III
            Add(6, 3);  // Respiration III
            Add(8, 1);  // Aqua Affinity I
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 2. Chestplate
        if (clean.EndsWith("_chestplate"))
        {
            AddArmorProtection();
            Add(5, 3);  // Thorns III
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 3. Leggings
        if (clean.EndsWith("_leggings"))
        {
            AddArmorProtection();
            Add(5, 3);  // Thorns III
            Add(37, 3); // Swift Sneak III
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 4. Boots
        if (clean.EndsWith("_boots"))
        {
            AddArmorProtection();
            Add(2, 4);  // Feather Falling IV
            Add(5, 3);  // Thorns III
            if (existing.Contains(7)) Add(7, 3);        // Depth Strider III
            else if (existing.Contains(25)) Add(25, 2); // Frost Walker II
            Add(36, 3); // Soul Speed III
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 5. Elytra
        if (clean == "elytra")
        {
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 6. Shield
        if (clean == "shield")
        {
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 7. Sword
        if (clean.EndsWith("_sword"))
        {
            AddWeaponDamage();
            Add(12, 2); // Knockback II
            Add(13, 2); // Fire Aspect II
            Add(14, 3); // Looting III
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 8. Spear
        if (clean.Contains("spear"))
        {
            AddWeaponDamage();
            Add(12, 2); // Knockback II
            Add(13, 2); // Fire Aspect II
            Add(14, 3); // Looting III
            Add(29, 5); // Impaling V
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 9. Mace
        if (clean == "mace")
        {
            if (existing.Contains(39)) Add(39, 5);       // Density V
            else if (existing.Contains(40)) Add(40, 4);  // Breach IV
            else if (existing.Contains(10)) Add(10, 5);  // Smite V
            else if (existing.Contains(11)) Add(11, 5);  // Bane of Arthropods V
            Add(38, 3); // Wind Burst III
            Add(13, 2); // Fire Aspect II
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 10. Trident
        if (clean == "trident")
        {
            Add(29, 5); // Impaling V
            if (existing.Contains(30))
            {
                Add(30, 3); // Riptide III
            }
            else if (existing.Contains(31) || existing.Contains(32))
            {
                Add(31, 3); // Loyalty III
                Add(32, 1); // Channeling I
            }
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 11. Bow
        if (clean == "bow")
        {
            Add(19, 5); // Power V
            Add(20, 2); // Punch II
            Add(21, 1); // Flame I
            if (existing.Contains(22)) Add(22, 1);      // Infinity I
            else if (existing.Contains(26)) Add(26, 1); // Mending I
            Add(17, 3); // Unbreaking III
            return result;
        }

        // 12. Crossbow
        if (clean == "crossbow")
        {
            Add(35, 3); // Quick Charge III
            if (existing.Contains(33)) Add(33, 1);      // Multishot I
            else if (existing.Contains(34)) Add(34, 4); // Piercing IV
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 13. Pickaxe
        if (clean.EndsWith("_pickaxe"))
        {
            Add(15, 5); // Efficiency V
            AddToolHarvesting();
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 14. Axe
        if (clean.EndsWith("_axe") && !clean.Contains("pickaxe"))
        {
            Add(15, 5); // Efficiency V
            AddToolHarvesting();
            AddWeaponDamage();
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 15. Shovel
        if (clean.EndsWith("_shovel"))
        {
            Add(15, 5); // Efficiency V
            AddToolHarvesting();
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 16. Hoe
        if (clean.EndsWith("_hoe"))
        {
            Add(15, 5); // Efficiency V
            AddToolHarvesting();
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 17. Fishing Rod
        if (clean == "fishing_rod")
        {
            Add(23, 3); // Luck of the Sea III
            Add(24, 3); // Lure III
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 18. Shears
        if (clean == "shears")
        {
            Add(15, 5); // Efficiency V
            Add(16, 1); // Silk Touch I
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 19. Other durability tools & items
        if (clean == "brush" || clean == "flint_and_steel" || clean == "carrot_on_a_stick" || clean == "warped_fungus_on_a_stick" || clean == "wolf_armor" || clean.Contains("horse_armor") || clean.Contains("nautilus_armor"))
        {
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
            return result;
        }

        // 20. Book / Enchanted Book (can hold all)
        if (clean == "book" || clean == "enchanted_book")
        {
            foreach (var e in All)
            {
                var maxLvl = e.MaxVanillaLevel switch
                {
                    "V" => (short)5,
                    "IV" => (short)4,
                    "III" => (short)3,
                    "II" => (short)2,
                    _ => (short)1
                };
                result.Add((e, maxLvl));
            }
            return result;
        }

        // 21. Generic Durability Fallback
        if (BedrockItemRegistry.GetMaxDurability(itemId) > 0)
        {
            Add(17, 3); // Unbreaking III
            Add(26, 1); // Mending I
        }

        return result;
    }
}
