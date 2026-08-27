using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Registry;

namespace BedrockInventoryEditor.UI.Dialogs;

public partial class ItemEditorDialog : Window
{
    private readonly ItemStack _originalItem;
    private readonly ItemStack _workingItem;
    private bool _isInitialized = false;
    private bool _isUpdatingDurability = false;

    public ObservableCollection<EnchantmentEntry> WorkingEnchantments { get; } = [];

    public ItemEditorDialog(ItemStack item)
    {
        InitializeComponent();

        _originalItem = item;
        _workingItem = item.Clone();

        TxtSlotLocation.Text = $"{item.Location} • Slot #{item.Slot}";

        // Setup Item Registry items in ComboBox
        CmbItems.ItemsSource = BedrockItemRegistry.Items.Select(i => i.Id).ToList();
        CmbItems.Text = _workingItem.Id;

        // Setup Enchantments ComboBox
        CmbEnchantments.ItemsSource = BedrockEnchantments.All;
        if (BedrockEnchantments.All.Count > 0)
        {
            CmbEnchantments.SelectedIndex = 0;
        }

        // Setup Working Collections
        foreach (var ench in _workingItem.Enchantments)
        {
            WorkingEnchantments.Add(new EnchantmentEntry(ench.Id, ench.Name, ench.Level));
        }
        LstEnchantments.ItemsSource = WorkingEnchantments;

        // Populate fields
        TxtCount.Text = _workingItem.Count == 0 ? "1" : _workingItem.Count.ToString();
        TxtCustomName.Text = _workingItem.CustomName;

        _isInitialized = true;

        UpdateDurabilityDisplay();
        UpdateLivePreview();
    }

    private void UpdateDurabilityDisplay()
    {
        if (!_isInitialized || PnlDurability == null || RunDurabilityRatio == null || RunDurabilityPercent == null || TxtCurrentDurability == null)
            return;

        var rawId = CmbItems?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':'))
        {
            rawId = "minecraft:" + rawId;
        }

        var max = BedrockItemRegistry.GetMaxDurability(rawId);
        if (max > 0)
        {
            PnlDurability.Visibility = Visibility.Visible;
            var current = Math.Max(0, max - _workingItem.Damage);
            if (current > max) current = max;

            var percent = (int)Math.Round((double)current / max * 100.0);

            RunDurabilityRatio.Text = $"({current} / {max})";
            RunDurabilityPercent.Text = $" • {percent}% Sisa";
            RunDurabilityPercent.Foreground = percent switch
            {
                > 60 => new SolidColorBrush(Color.FromRgb(85, 255, 85)),   // #55FF55
                > 25 => new SolidColorBrush(Color.FromRgb(255, 170, 0)),  // #FFAA00
                _ => new SolidColorBrush(Color.FromRgb(255, 85, 85))      // #FF5555
            };

            _isUpdatingDurability = true;
            TxtCurrentDurability.Text = current.ToString();
            _isUpdatingDurability = false;
        }
        else
        {
            if (_workingItem.Damage > 0)
            {
                PnlDurability.Visibility = Visibility.Visible;
                RunDurabilityRatio.Text = $"({_workingItem.Damage})";
                RunDurabilityPercent.Text = " • Damage / Aux";
                RunDurabilityPercent.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));

                _isUpdatingDurability = true;
                TxtCurrentDurability.Text = _workingItem.Damage.ToString();
                _isUpdatingDurability = false;
            }
            else
            {
                PnlDurability.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void OnCurrentDurabilityChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || _isUpdatingDurability) return;

        var rawId = CmbItems?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':'))
        {
            rawId = "minecraft:" + rawId;
        }

        var max = BedrockItemRegistry.GetMaxDurability(rawId);
        if (int.TryParse(TxtCurrentDurability.Text.Trim(), out var typedVal))
        {
            if (max > 0)
            {
                var clampedCurrent = Math.Clamp(typedVal, 0, max);
                _workingItem.Damage = (short)(max - clampedCurrent);
                var percent = (int)Math.Round((double)clampedCurrent / max * 100.0);

                if (RunDurabilityRatio != null && RunDurabilityPercent != null)
                {
                    RunDurabilityRatio.Text = $"({clampedCurrent} / {max})";
                    RunDurabilityPercent.Text = $" • {percent}% Sisa";
                    RunDurabilityPercent.Foreground = percent switch
                    {
                        > 60 => new SolidColorBrush(Color.FromRgb(85, 255, 85)),
                        > 25 => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                        _ => new SolidColorBrush(Color.FromRgb(255, 85, 85))
                    };
                }
            }
            else
            {
                _workingItem.Damage = (short)Math.Max(0, typedVal);
                if (RunDurabilityRatio != null && RunDurabilityPercent != null)
                {
                    RunDurabilityRatio.Text = $"({_workingItem.Damage})";
                    RunDurabilityPercent.Text = " • Damage / Aux";
                }
            }
        }
    }

    private void OnResetDurabilityClick(object sender, RoutedEventArgs e)
    {
        var rawId = CmbItems?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':'))
        {
            rawId = "minecraft:" + rawId;
        }

        _workingItem.Damage = 0;
        _workingItem.ExtraNbt?.Remove("Damage");
        _workingItem.ExtraNbt?.Remove("damage");
        _workingItem.ExtraNbt?.Remove("Aux");
        _workingItem.ExtraNbt?.Remove("aux");

        var max = BedrockItemRegistry.GetMaxDurability(rawId);
        if (max > 0)
        {
            _isUpdatingDurability = true;
            TxtCurrentDurability.Text = max.ToString();
            _isUpdatingDurability = false;

            if (RunDurabilityRatio != null && RunDurabilityPercent != null)
            {
                RunDurabilityRatio.Text = $"({max} / {max})";
                RunDurabilityPercent.Text = " • 100% Sisa";
                RunDurabilityPercent.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 85));
            }
        }
        else
        {
            _isUpdatingDurability = true;
            TxtCurrentDurability.Text = "0";
            _isUpdatingDurability = false;

            if (RunDurabilityRatio != null && RunDurabilityPercent != null)
            {
                RunDurabilityRatio.Text = "(0)";
                RunDurabilityPercent.Text = " • Baru";
            }
        }
    }

    private void UpdateLivePreview()
    {
        if (!_isInitialized || ImgItemPreview == null || TxtItemFallback == null || TxtPvwCount == null || PvwEnchantGlow == null || TxtPvwStar == null)
            return;

        var rawId = CmbItems?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':'))
        {
            rawId = "minecraft:" + rawId;
        }

        var countStr = TxtCount?.Text?.Trim() ?? "1";
        byte.TryParse(countStr, out var count);
        if (count == 0 && !string.IsNullOrEmpty(rawId)) count = 1;

        var customName = TxtCustomName?.Text?.Trim() ?? string.Empty;
        var vanillaName = !string.IsNullOrEmpty(rawId) ? BedrockItemRegistry.GetDisplayName(rawId) : "Slot Kosong";

        if (TxtItemCategory != null)
        {
            TxtItemCategory.Text = !string.IsNullOrEmpty(customName) ? customName : vanillaName;
        }

        var img = ItemTextureService.GetItemImage(rawId);
        if (img != null)
        {
            ImgItemPreview.Source = img;
            ImgItemPreview.Visibility = Visibility.Visible;
            TxtItemFallback.Visibility = Visibility.Collapsed;
        }
        else if (!string.IsNullOrEmpty(rawId))
        {
            ImgItemPreview.Visibility = Visibility.Collapsed;
            TxtItemFallback.Text = BedrockItemRegistry.GetIcon(rawId);
            TxtItemFallback.Visibility = Visibility.Visible;
        }
        else
        {
            ImgItemPreview.Visibility = Visibility.Collapsed;
            TxtItemFallback.Text = _originalItem.Location switch
            {
                SlotLocation.ArmorHelmet => "🪖",
                SlotLocation.ArmorChestplate => "🦺",
                SlotLocation.ArmorLeggings => "👖",
                SlotLocation.ArmorBoots => "👢",
                SlotLocation.Offhand => "🛡️",
                _ => ""
            };
            TxtItemFallback.Visibility = string.IsNullOrEmpty(TxtItemFallback.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        TxtPvwCount.Text = count > 1 ? count.ToString() : "";
        TxtPvwCount.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;

        var hasEnchants = WorkingEnchantments.Count > 0;
        PvwEnchantGlow.Visibility = hasEnchants ? Visibility.Visible : Visibility.Collapsed;
        TxtPvwStar.Visibility = hasEnchants ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnFormValuesChanged(object sender, TextChangedEventArgs e)
    {
        UpdateLivePreview();
    }

    private void OnItemSearchChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDurabilityDisplay();
        UpdateLivePreview();
    }

    private void OnItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbItems.SelectedItem is string selectedId)
        {
            CmbItems.Text = selectedId;
            UpdateDurabilityDisplay();
            UpdateLivePreview();
        }
    }

    private void OnAddEnchantmentClick(object sender, RoutedEventArgs e)
    {
        if (CmbEnchantments.SelectedItem is EnchantmentInfo info)
        {
            if (!short.TryParse(TxtEnchantLevel.Text.Trim(), out var lvl) || lvl < 1)
            {
                lvl = 1;
            }

            var existing = WorkingEnchantments.FirstOrDefault(x => x.Id == info.Id);
            if (existing != null)
            {
                existing.Level = lvl;
            }
            else
            {
                WorkingEnchantments.Add(new EnchantmentEntry(info.Id, info.Name, lvl));
            }
            UpdateLivePreview();
        }
    }

    private void OnRemoveEnchantmentItem(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.DataContext is EnchantmentEntry entry)
        {
            WorkingEnchantments.Remove(entry);
            UpdateLivePreview();
        }
    }

    private void OnMaxValidEnchantsClick(object sender, RoutedEventArgs e)
    {
        var id = CmbItems.Text?.ToLowerInvariant() ?? "";
        if (id.Contains("sword"))
        {
            AddOrUpdateEnchant(9, "Sharpness", 5);
            AddOrUpdateEnchant(13, "Fire Aspect", 2);
            AddOrUpdateEnchant(14, "Looting", 3);
            AddOrUpdateEnchant(17, "Unbreaking", 3);
            AddOrUpdateEnchant(26, "Mending", 1);
        }
        else if (id.Contains("pickaxe") || id.Contains("shovel") || id.Contains("axe") || id.Contains("hoe"))
        {
            AddOrUpdateEnchant(15, "Efficiency", 5);
            AddOrUpdateEnchant(18, "Fortune", 3);
            AddOrUpdateEnchant(17, "Unbreaking", 3);
            AddOrUpdateEnchant(26, "Mending", 1);
        }
        else if (id.Contains("helmet") || id.Contains("chestplate") || id.Contains("leggings") || id.Contains("boots"))
        {
            AddOrUpdateEnchant(0, "Protection", 4);
            AddOrUpdateEnchant(17, "Unbreaking", 3);
            AddOrUpdateEnchant(26, "Mending", 1);
            AddOrUpdateEnchant(5, "Thorns", 3);
        }
        else if (id.Contains("bow"))
        {
            AddOrUpdateEnchant(19, "Power", 5);
            AddOrUpdateEnchant(20, "Punch", 2);
            AddOrUpdateEnchant(21, "Flame", 1);
            AddOrUpdateEnchant(22, "Infinity", 1);
            AddOrUpdateEnchant(17, "Unbreaking", 3);
        }
        else if (id.Contains("elytra"))
        {
            AddOrUpdateEnchant(17, "Unbreaking", 3);
            AddOrUpdateEnchant(26, "Mending", 1);
        }
        else
        {
            AddOrUpdateEnchant(17, "Unbreaking", 3);
            AddOrUpdateEnchant(26, "Mending", 1);
        }
        UpdateLivePreview();
    }

    private void AddOrUpdateEnchant(short id, string name, short lvl)
    {
        var ex = WorkingEnchantments.FirstOrDefault(x => x.Id == id);
        if (ex != null) ex.Level = lvl;
        else WorkingEnchantments.Add(new EnchantmentEntry(id, name, lvl));
    }

    private void OnClearSlotClick(object sender, RoutedEventArgs e)
    {
        CmbItems.Text = "";
        TxtCount.Text = "0";
        TxtCustomName.Text = "";
        _workingItem.Damage = 0;
        _workingItem.ExtraNbt?.Remove("Damage");
        _workingItem.ExtraNbt?.Remove("damage");
        _workingItem.ExtraNbt?.Remove("Aux");
        _workingItem.ExtraNbt?.Remove("aux");
        WorkingEnchantments.Clear();
        UpdateDurabilityDisplay();
        UpdateLivePreview();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var rawId = CmbItems.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':'))
        {
            rawId = "minecraft:" + rawId;
        }

        if (string.IsNullOrEmpty(rawId) || rawId == "minecraft:air")
        {
            _originalItem.Clear();
        }
        else
        {
            _originalItem.Id = rawId;

            if (byte.TryParse(TxtCount.Text.Trim(), out var count))
            {
                _originalItem.Count = count == 0 ? (byte)1 : count;
            }
            else
            {
                _originalItem.Count = 1;
            }

            _originalItem.Damage = _workingItem.Damage;
            if (_originalItem.Damage == 0)
            {
                _originalItem.ExtraNbt?.Remove("Damage");
                _originalItem.ExtraNbt?.Remove("damage");
                _originalItem.ExtraNbt?.Remove("Aux");
                _originalItem.ExtraNbt?.Remove("aux");
            }
            else if (_originalItem.ExtraNbt != null)
            {
                if (_originalItem.ExtraNbt.ContainsKey("Damage")) _originalItem.ExtraNbt.SetShort("Damage", _originalItem.Damage);
                if (_originalItem.ExtraNbt.ContainsKey("damage")) _originalItem.ExtraNbt.SetShort("damage", _originalItem.Damage);
            }

            // Custom Name: If empty, set empty string so no empty display tag is created
            var customName = TxtCustomName.Text.Trim();
            var vanillaName = BedrockItemRegistry.GetDisplayName(rawId);

            if (string.IsNullOrWhiteSpace(customName) || string.Equals(customName, vanillaName, StringComparison.OrdinalIgnoreCase))
            {
                _originalItem.CustomName = string.Empty;
            }
            else
            {
                _originalItem.CustomName = customName;
            }

            _originalItem.Enchantments.Clear();
            foreach (var ench in WorkingEnchantments)
            {
                _originalItem.Enchantments.Add(new EnchantmentEntry(ench.Id, ench.Name, ench.Level));
            }
        }

        DialogResult = true;
        Close();
    }
}
