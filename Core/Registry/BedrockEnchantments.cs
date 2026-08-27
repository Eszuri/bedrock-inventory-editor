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
}
