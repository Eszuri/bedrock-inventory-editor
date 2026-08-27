using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Registry;

namespace BedrockInventoryEditor.UI.Converters;

public class EmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEmpty)
        {
            var invert = parameter as string == "Invert";
            if (invert) return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte count)
        {
            return count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (value is int intCount)
        {
            return intCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ItemColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var id = value as string;
        if (value is ItemStack item) id = item.Id;

        if (string.IsNullOrEmpty(id)) return new SolidColorBrush(Color.FromArgb(80, 150, 150, 170));
        if (id.Contains("netherite")) return new SolidColorBrush(Color.FromRgb(180, 170, 185));
        if (id.Contains("diamond")) return new SolidColorBrush(Color.FromRgb(85, 255, 255));
        if (id.Contains("gold")) return new SolidColorBrush(Color.FromRgb(255, 230, 80));
        if (id.Contains("iron")) return new SolidColorBrush(Color.FromRgb(230, 230, 230));
        if (id.Contains("emerald")) return new SolidColorBrush(Color.FromRgb(85, 255, 85));
        if (id.Contains("potion") || id.Contains("apple")) return new SolidColorBrush(Color.FromRgb(255, 170, 0));
        return new SolidColorBrush(Color.FromRgb(255, 255, 255));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ItemImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? id = value as string;
        if (value is ItemStack item) id = item.Id;

        if (!string.IsNullOrWhiteSpace(id) && id != "minecraft:air")
        {
            return ItemTextureService.GetItemImage(id);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ItemHasImageToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? id = value as string;
        if (value is ItemStack item) id = item.Id;

        var invert = parameter as string == "Invert";
        if (!string.IsNullOrWhiteSpace(id) && id != "minecraft:air")
        {
            var img = ItemTextureService.GetItemImage(id);
            if (invert) return img == null ? Visibility.Visible : Visibility.Collapsed;
            return img != null ? Visibility.Visible : Visibility.Collapsed;
        }

        return invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ItemIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string id && !string.IsNullOrEmpty(id))
        {
            return BedrockItemRegistry.GetIcon(id);
        }
        else if (value is ItemStack item)
        {
            if (item.IsEmpty)
            {
                return item.Location switch
                {
                    SlotLocation.ArmorHelmet => "🪖",
                    SlotLocation.ArmorChestplate => "🦺",
                    SlotLocation.ArmorLeggings => "👖",
                    SlotLocation.ArmorBoots => "👢",
                    SlotLocation.Offhand => "🛡️",
                    _ => ""
                };
            }
            return BedrockItemRegistry.GetIcon(item.Id);
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ItemShortLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var id = value as string;
        if (value is ItemStack item) id = item.Id;

        if (string.IsNullOrEmpty(id)) return "";
        return BedrockItemRegistry.GetShortLabel(id);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
