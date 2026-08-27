using System;
using System.Collections.Generic;
using System.Linq;

namespace BedrockInventoryEditor.Core.Registry;

public record ItemDefinition(string Id, string DisplayName, string Category, string Icon);

public static class BedrockItemRegistry
{
    public static readonly List<ItemDefinition> Items = new()
    {
        // Weapons & Combat
        new("minecraft:netherite_sword", "Netherite Sword", "Combat", "🗡️"),
        new("minecraft:diamond_sword", "Diamond Sword", "Combat", "🗡️"),
        new("minecraft:iron_sword", "Iron Sword", "Combat", "🗡️"),
        new("minecraft:golden_sword", "Golden Sword", "Combat", "🗡️"),
        new("minecraft:stone_sword", "Stone Sword", "Combat", "🗡️"),
        new("minecraft:wooden_sword", "Wooden Sword", "Combat", "🗡️"),
        new("minecraft:mace", "Mace", "Combat", "🔨"),
        new("minecraft:trident", "Trident", "Combat", "🔱"),
        new("minecraft:bow", "Bow", "Combat", "🏹"),
        new("minecraft:crossbow", "Crossbow", "Combat", "🎯"),
        new("minecraft:arrow", "Arrow", "Combat", "🏹"),
        new("minecraft:shield", "Shield", "Combat", "🛡️"),
        new("minecraft:totem_of_undying", "Totem of Undying", "Combat", "🌟"),
        new("minecraft:tnt", "TNT", "Combat", "🧨"),
        new("minecraft:firework_rocket", "Firework Rocket", "Combat", "🚀"),
        new("minecraft:wind_charge", "Wind Charge", "Combat", "💨"),

        // Tools
        new("minecraft:netherite_pickaxe", "Netherite Pickaxe", "Tools", "⛏️"),
        new("minecraft:diamond_pickaxe", "Diamond Pickaxe", "Tools", "⛏️"),
        new("minecraft:iron_pickaxe", "Iron Pickaxe", "Tools", "⛏️"),
        new("minecraft:golden_pickaxe", "Golden Pickaxe", "Tools", "⛏️"),
        new("minecraft:stone_pickaxe", "Stone Pickaxe", "Tools", "⛏️"),
        new("minecraft:wooden_pickaxe", "Wooden Pickaxe", "Tools", "⛏️"),

        new("minecraft:netherite_axe", "Netherite Axe", "Tools", "🪓"),
        new("minecraft:diamond_axe", "Diamond Axe", "Tools", "🪓"),
        new("minecraft:iron_axe", "Iron Axe", "Tools", "🪓"),
        new("minecraft:golden_axe", "Golden Axe", "Tools", "🪓"),
        new("minecraft:stone_axe", "Stone Axe", "Tools", "🪓"),
        new("minecraft:wooden_axe", "Wooden Axe", "Tools", "🪓"),

        new("minecraft:netherite_shovel", "Netherite Shovel", "Tools", "🥄"),
        new("minecraft:diamond_shovel", "Diamond Shovel", "Tools", "🥄"),
        new("minecraft:iron_shovel", "Iron Shovel", "Tools", "🥄"),
        new("minecraft:golden_shovel", "Golden Shovel", "Tools", "🥄"),
        new("minecraft:stone_shovel", "Stone Shovel", "Tools", "🥄"),
        new("minecraft:wooden_shovel", "Wooden Shovel", "Tools", "🥄"),

        new("minecraft:netherite_hoe", "Netherite Hoe", "Tools", "🌾"),
        new("minecraft:diamond_hoe", "Diamond Hoe", "Tools", "🌾"),
        new("minecraft:iron_hoe", "Iron Hoe", "Tools", "🌾"),
        new("minecraft:flint_and_steel", "Flint and Steel", "Tools", "🔥"),
        new("minecraft:shears", "Shears", "Tools", "✂️"),
        new("minecraft:fishing_rod", "Fishing Rod", "Tools", "🎣"),
        new("minecraft:compass", "Compass", "Tools", "🧭"),
        new("minecraft:clock", "Clock", "Tools", "⏰"),
        new("minecraft:spyglass", "Spyglass", "Tools", "🔭"),
        new("minecraft:lead", "Lead", "Tools", "🪢"),
        new("minecraft:name_tag", "Name Tag", "Tools", "🏷️"),

        // Armor
        new("minecraft:netherite_helmet", "Netherite Helmet", "Armor", "🪖"),
        new("minecraft:netherite_chestplate", "Netherite Chestplate", "Armor", "🦺"),
        new("minecraft:netherite_leggings", "Netherite Leggings", "Armor", "👖"),
        new("minecraft:netherite_boots", "Netherite Boots", "Armor", "👢"),

        new("minecraft:diamond_helmet", "Diamond Helmet", "Armor", "🪖"),
        new("minecraft:diamond_chestplate", "Diamond Chestplate", "Armor", "🦺"),
        new("minecraft:diamond_leggings", "Diamond Leggings", "Armor", "👖"),
        new("minecraft:diamond_boots", "Diamond Boots", "Armor", "👢"),

        new("minecraft:iron_helmet", "Iron Helmet", "Armor", "🪖"),
        new("minecraft:iron_chestplate", "Iron Chestplate", "Armor", "🦺"),
        new("minecraft:iron_leggings", "Iron Leggings", "Armor", "👖"),
        new("minecraft:iron_boots", "Iron Boots", "Armor", "👢"),

        new("minecraft:golden_helmet", "Golden Helmet", "Armor", "🪖"),
        new("minecraft:golden_chestplate", "Golden Chestplate", "Armor", "🦺"),
        new("minecraft:golden_leggings", "Golden Leggings", "Armor", "👖"),
        new("minecraft:golden_boots", "Golden Boots", "Armor", "👢"),

        new("minecraft:chainmail_helmet", "Chainmail Helmet", "Armor", "🪖"),
        new("minecraft:chainmail_chestplate", "Chainmail Chestplate", "Armor", "🦺"),
        new("minecraft:chainmail_leggings", "Chainmail Leggings", "Armor", "👖"),
        new("minecraft:chainmail_boots", "Chainmail Boots", "Armor", "👢"),

        new("minecraft:leather_helmet", "Leather Helmet", "Armor", "🪖"),
        new("minecraft:leather_chestplate", "Leather Chestplate", "Armor", "🦺"),
        new("minecraft:leather_leggings", "Leather Leggings", "Armor", "👖"),
        new("minecraft:leather_boots", "Leather Boots", "Armor", "👢"),

        new("minecraft:elytra", "Elytra", "Armor", "🪽"),
        new("minecraft:turtle_helmet", "Turtle Shell", "Armor", "🐢"),

        // Consumables & Valuables
        new("minecraft:enchanted_golden_apple", "Enchanted Golden Apple", "Food", "🍎"),
        new("minecraft:golden_apple", "Golden Apple", "Food", "🍏"),
        new("minecraft:golden_carrot", "Golden Carrot", "Food", "🥕"),
        new("minecraft:cooked_beef", "Steak", "Food", "🥩"),
        new("minecraft:cooked_porkchop", "Cooked Porkchop", "Food", "🍖"),
        new("minecraft:bread", "Bread", "Food", "🍞"),
        new("minecraft:potion", "Potion", "Potions", "🧪"),
        new("minecraft:splash_potion", "Splash Potion", "Potions", "🧪"),
        new("minecraft:lingering_potion", "Lingering Potion", "Potions", "🧪"),
        new("minecraft:experience_bottle", "Bottle o' Enchanting", "Items", "🍾"),

        // Minerals & Resources
        new("minecraft:netherite_ingot", "Netherite Ingot", "Materials", "🧱"),
        new("minecraft:netherite_scrap", "Netherite Scrap", "Materials", "🪙"),
        new("minecraft:diamond", "Diamond", "Materials", "💎"),
        new("minecraft:emerald", "Emerald", "Materials", "🟢"),
        new("minecraft:gold_ingot", "Gold Ingot", "Materials", "🟡"),
        new("minecraft:iron_ingot", "Iron Ingot", "Materials", "⚪"),
        new("minecraft:copper_ingot", "Copper Ingot", "Materials", "🟤"),
        new("minecraft:lapis_lazuli", "Lapis Lazuli", "Materials", "🔵"),
        new("minecraft:redstone", "Redstone Dust", "Materials", "🔴"),
        new("minecraft:coal", "Coal", "Materials", "⚫"),
        new("minecraft:amethyst_shard", "Amethyst Shard", "Materials", "🔮"),
        new("minecraft:quartz", "Nether Quartz", "Materials", "⚪"),

        // Utility & Storage
        new("minecraft:bundle", "Bundle", "Items", "🎒"),
        new("minecraft:ender_pearl", "Ender Pearl", "Items", "🔮"),
        new("minecraft:eye_of_ender", "Eye of Ender", "Items", "👁️"),
        new("minecraft:water_bucket", "Water Bucket", "Items", "🪣"),
        new("minecraft:lava_bucket", "Lava Bucket", "Items", "🪣"),
        new("minecraft:milk_bucket", "Milk Bucket", "Items", "🪣"),
        new("minecraft:powder_snow_bucket", "Powder Snow Bucket", "Items", "🪣"),
        new("minecraft:bucket", "Bucket", "Items", "🪣"),
        new("minecraft:stick", "Stick", "Items", "🥢"),
        new("minecraft:string", "String", "Items", "🧵"),
        new("minecraft:feather", "Feather", "Items", "🪶"),
        new("minecraft:gunpowder", "Gunpowder", "Items", "⚫"),
        new("minecraft:bone", "Bone", "Items", "🦴"),
        new("minecraft:blaze_rod", "Blaze Rod", "Items", "🪄"),
        new("minecraft:nether_star", "Nether Star", "Items", "⭐"),
        new("minecraft:enchanted_book", "Enchanted Book", "Items", "📖"),
        new("minecraft:book", "Book", "Items", "📕"),

        // Blocks
        new("minecraft:undyed_shulker_box", "Shulker Box", "Blocks", "📦"),
        new("minecraft:white_shulker_box", "White Shulker Box", "Blocks", "📦"),
        new("minecraft:ender_chest", "Ender Chest", "Blocks", "👁️"),
        new("minecraft:chest", "Chest", "Blocks", "🧰"),
        new("minecraft:barrel", "Barrel", "Blocks", "🛢️"),
        new("minecraft:crafting_table", "Crafting Table", "Blocks", "🪑"),
        new("minecraft:furnace", "Furnace", "Blocks", "🔥"),
        new("minecraft:blast_furnace", "Blast Furnace", "Blocks", "🔥"),
        new("minecraft:smoker", "Smoker", "Blocks", "🔥"),
        new("minecraft:anvil", "Anvil", "Blocks", "⚒️"),
        new("minecraft:beacon", "Beacon", "Blocks", "🔦"),
        new("minecraft:conduit", "Conduit", "Blocks", "🐚"),
        new("minecraft:obsidian", "Obsidian", "Blocks", "⬛"),
        new("minecraft:crying_obsidian", "Crying Obsidian", "Blocks", "💜"),
        new("minecraft:respawn_anchor", "Respawn Anchor", "Blocks", "⚓"),
        new("minecraft:lodestone", "Lodestone", "Blocks", "🧭"),
        new("minecraft:diamond_block", "Block of Diamond", "Blocks", "🔷"),
        new("minecraft:netherite_block", "Block of Netherite", "Blocks", "⬛"),
        new("minecraft:gold_block", "Block of Gold", "Blocks", "🟨"),
        new("minecraft:emerald_block", "Block of Emerald", "Blocks", "🟩"),
        new("minecraft:iron_block", "Block of Iron", "Blocks", "⬜"),
        new("minecraft:bookshelf", "Bookshelf", "Blocks", "📚"),
        new("minecraft:glass", "Glass", "Blocks", "🪟"),
        new("minecraft:torch", "Torch", "Blocks", "🕯️"),
        new("minecraft:sea_lantern", "Sea Lantern", "Blocks", "💡"),
        new("minecraft:glowstone", "Glowstone", "Blocks", "💡"),
        new("minecraft:oak_log", "Oak Log", "Blocks", "🪵"),
        new("minecraft:warped_stem", "Warped Stem", "Blocks", "🪵"),
        new("minecraft:bone_block", "Bone Block", "Blocks", "🦴"),
        new("minecraft:cobblestone", "Cobblestone", "Blocks", "🧱"),
        new("minecraft:stone", "Stone", "Blocks", "🪨"),
        new("minecraft:dirt", "Dirt", "Blocks", "🟫"),
        new("minecraft:grass_block", "Grass Block", "Blocks", "🌱"),
        new("minecraft:bedrock", "Bedrock", "Blocks", "🪨")
    };

    public static int GetMaxDurability(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        var clean = itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..] : itemId;
        clean = clean.ToLowerInvariant();

        // Netherite Gear
        if (clean == "netherite_sword" || clean == "netherite_pickaxe" || clean == "netherite_axe" || clean == "netherite_shovel" || clean == "netherite_hoe") return 2031;
        if (clean == "netherite_helmet") return 407;
        if (clean == "netherite_chestplate") return 592;
        if (clean == "netherite_leggings") return 555;
        if (clean == "netherite_boots") return 481;

        // Diamond Gear
        if (clean == "diamond_sword" || clean == "diamond_pickaxe" || clean == "diamond_axe" || clean == "diamond_shovel" || clean == "diamond_hoe") return 1561;
        if (clean == "diamond_helmet") return 363;
        if (clean == "diamond_chestplate") return 528;
        if (clean == "diamond_leggings") return 495;
        if (clean == "diamond_boots") return 429;

        // Iron Gear
        if (clean == "iron_sword" || clean == "iron_pickaxe" || clean == "iron_axe" || clean == "iron_shovel" || clean == "iron_hoe") return 250;
        if (clean == "iron_helmet") return 165;
        if (clean == "iron_chestplate") return 240;
        if (clean == "iron_leggings") return 225;
        if (clean == "iron_boots") return 195;

        // Golden Gear
        if (clean == "golden_sword" || clean == "golden_pickaxe" || clean == "golden_axe" || clean == "golden_shovel" || clean == "golden_hoe") return 32;
        if (clean == "golden_helmet") return 77;
        if (clean == "golden_chestplate") return 112;
        if (clean == "golden_leggings") return 105;
        if (clean == "golden_boots") return 91;

        // Stone Tools
        if (clean == "stone_sword" || clean == "stone_pickaxe" || clean == "stone_axe" || clean == "stone_shovel" || clean == "stone_hoe") return 131;

        // Wooden Tools
        if (clean == "wooden_sword" || clean == "wooden_pickaxe" || clean == "wooden_axe" || clean == "wooden_shovel" || clean == "wooden_hoe") return 59;

        // Chainmail
        if (clean == "chainmail_helmet") return 165;
        if (clean == "chainmail_chestplate") return 240;
        if (clean == "chainmail_leggings") return 225;
        if (clean == "chainmail_boots") return 195;

        // Leather
        if (clean == "leather_helmet") return 55;
        if (clean == "leather_chestplate") return 80;
        if (clean == "leather_leggings") return 75;
        if (clean == "leather_boots") return 65;

        // Other Items with Durability
        if (clean == "elytra") return 432;
        if (clean == "bow") return 384;
        if (clean == "crossbow") return 465;
        if (clean == "trident") return 250;
        if (clean == "shield") return 336;
        if (clean == "fishing_rod" || clean == "flint_and_steel" || clean == "brush" || clean == "carrot_on_a_stick" || clean == "warped_fungus_on_a_stick") return 64;
        if (clean == "shears") return 238;
        if (clean == "turtle_helmet") return 275;
        if (clean == "mace") return 500;

        return 0;
    }

    public static string GetDisplayName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return "Empty";
        var found = Items.FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (found != null) return found.DisplayName;

        var clean = itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..] : itemId;
        var words = clean.Split('_').Select(w => char.ToUpperInvariant(w[0]) + w[1..]);
        return string.Join(' ', words);
    }

    public static string GetIcon(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return "";
        var found = Items.FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (found != null) return found.Icon;

        if (itemId.Contains("sword")) return "🗡️";
        if (itemId.Contains("pickaxe")) return "⛏️";
        if (itemId.Contains("axe")) return "🪓";
        if (itemId.Contains("shovel")) return "🥄";
        if (itemId.Contains("helmet")) return "🪖";
        if (itemId.Contains("chestplate")) return "🦺";
        if (itemId.Contains("leggings")) return "👖";
        if (itemId.Contains("boots")) return "👢";
        if (itemId.Contains("apple")) return "🍎";
        if (itemId.Contains("book")) return "📖";
        return "📦";
    }

    public static string GetShortLabel(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return "";
        var clean = itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..] : itemId;
        if (clean.Contains('_'))
        {
            var parts = clean.Split('_');
            if (parts.Length >= 2)
            {
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            }
        }
        return clean.Length > 2 ? clean[..2].ToUpper() : clean.ToUpper();
    }
}
