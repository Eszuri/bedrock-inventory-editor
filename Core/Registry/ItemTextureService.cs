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
            string stripName = name;
            if (stripName.StartsWith("item.")) stripName = stripName["item.".Length..];
            if (stripName.StartsWith("tile.")) stripName = stripName["tile.".Length..];

            var candidates = new[]
            {
                $"{name}.png",
                $"{stripName}.png",
                stripName.StartsWith("waxed_") ? $"{stripName["waxed_".Length..]}.png" : null,
                stripName.StartsWith("wooden_") ? $"wood_{stripName["wooden_".Length..]}.png" : null,
                stripName.StartsWith("wood_") ? $"wooden_{stripName["wood_".Length..]}.png" : null,
                stripName.StartsWith("golden_") ? $"gold_{stripName["golden_".Length..]}.png" : null,
                stripName.StartsWith("gold_") ? $"golden_{stripName["gold_".Length..]}.png" : null,
                stripName.StartsWith("darkoak_") ? $"{stripName.Replace("darkoak_", "dark_oak_")}.png" : null,
                $"{stripName}_top.png",
                $"{stripName}_side.png",
                $"{stripName}_front.png",
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

                        // If the texture is an animated sprite strip (e.g. 16x80, 16x512), crop to the first 1:1 square frame
                        if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0 && bitmap.PixelWidth != bitmap.PixelHeight)
                        {
                            int squareSize = Math.Min(bitmap.PixelWidth, bitmap.PixelHeight);
                            var cropped = new CroppedBitmap(bitmap, new System.Windows.Int32Rect(0, 0, squareSize, squareSize));
                            cropped.Freeze();
                            return cropped;
                        }

                        return bitmap;
                    }
                    catch
                    {
                        // Ignore and try next
                    }
                }
            }

            return null;
        });
    }
}
