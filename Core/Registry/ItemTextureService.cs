using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BedrockInventoryEditor.Core.Registry;

public static class ItemTextureService
{
    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string TexturesDirectory = GetTexturesDirectory();

    private static string GetTexturesDirectory()
    {
        var primary = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Textures");
        if (Directory.Exists(primary)) return primary;

        var devFallback = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "Textures"));
        if (Directory.Exists(devFallback)) return devFallback;

        return primary;
    }

    public static ImageSource? GetItemImage(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || itemId == "minecraft:air")
            return null;

        var clean = itemId.StartsWith("minecraft:") ? itemId["minecraft:".Length..] : itemId;
        clean = clean.ToLowerInvariant();

        return Cache.GetOrAdd(clean, name =>
        {
            var candidates = new[]
            {
                $"{name}.png",
                name == "shield" ? "shield_base.png" : null,
                name == "scute" ? "turtle_scute.png" : null,
                name == "potion_bottle_drinkable" ? "potion.png" : null,
                name == "undyed_shulker_box" ? "shulker_box.png" : null,
                name.EndsWith("_shulker_box") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "shulker_box.png" : null,
                name.EndsWith("_bundle") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "bundle.png" : null,
                name == "crafter" && !File.Exists(Path.Combine(TexturesDirectory, "crafter.png")) ? "crafter_top.png" : null,
                name == "vault" && !File.Exists(Path.Combine(TexturesDirectory, "vault.png")) ? "vault_front.png" : null,
                name == "trial_spawner" && !File.Exists(Path.Combine(TexturesDirectory, "trial_spawner.png")) ? "trial_spawner_top.png" : null,
                name.EndsWith("_bed") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "bed.png" : null,
                name == "straw_bed" && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "bed.png" : null,
                name.EndsWith("_cushion") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "bed.png" : null,
                name.EndsWith("_shelf") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "bookshelf.png" : null,
                name.EndsWith("_harness") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "lead.png" : null,
                name.EndsWith("_spawn_egg") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "spawn_egg.png" : null,
                name.Contains("spear") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "trident.png" : null,
                name.Contains("nautilus_armor") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_horse_armor.png" : null,
                name == "eyeblossom" && !File.Exists(Path.Combine(TexturesDirectory, "eyeblossom.png")) ? "open_eyeblossom.png" : null,
                name == "creaking_heart" && !File.Exists(Path.Combine(TexturesDirectory, "creaking_heart.png")) ? "creaking_heart_active.png" : null,
                name == "sulfur_cube_bucket" && !File.Exists(Path.Combine(TexturesDirectory, "sulfur_cube_bucket.png")) ? "bucket.png" : null,
                name.Contains("helmet") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_helmet.png" : null,
                name.Contains("chestplate") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_chestplate.png" : null,
                name.Contains("leggings") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_leggings.png" : null,
                name.Contains("boots") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_boots.png" : null,
                name.Contains("sword") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_sword.png" : null,
                name.Contains("pickaxe") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_pickaxe.png" : null,
                name.Contains("axe") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_axe.png" : null,
                name.Contains("shovel") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_shovel.png" : null,
                name.Contains("hoe") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_hoe.png" : null,
            };

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate)) continue;
                var filePath = Path.Combine(TexturesDirectory, candidate);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        // Fallback
                    }
                }
            }

            return null;
        });
    }
}
