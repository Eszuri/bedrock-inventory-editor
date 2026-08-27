using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BedrockInventoryEditor.Core.Registry;

public static class ItemTextureService
{
    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string TexturesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Textures");

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
                name == "undyed_shulker_box" ? "shulker_box.png" : null,
                name.Contains("shulker") ? "shulker_box.png" : null,
                name.Contains("stem") || name.Contains("log") ? "oak_log.png" : null,
                name == "enchanted_golden_apple" ? "golden_apple.png" : null,
                name == "golden_apple" ? "enchanted_golden_apple.png" : null,
                name == "shield" ? "totem_of_undying.png" : null,
                name.Contains("helmet") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_helmet.png" : null,
                name.Contains("chestplate") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_chestplate.png" : null,
                name.Contains("leggings") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_leggings.png" : null,
                name.Contains("boots") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_boots.png" : null,
                name.Contains("sword") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_sword.png" : null,
                name.Contains("pickaxe") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_pickaxe.png" : null,
                name.Contains("axe") && !File.Exists(Path.Combine(TexturesDirectory, $"{name}.png")) ? "diamond_axe.png" : null,
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
