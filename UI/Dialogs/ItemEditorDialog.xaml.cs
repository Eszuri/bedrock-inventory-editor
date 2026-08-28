using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Registry;

namespace BedrockInventoryEditor.UI.Dialogs;

public partial class ItemEditorDialog : Window
{
    private readonly ItemStack _originalItem;
    private readonly ItemStack _workingItem;
    private bool _isInitialized = false;
    private bool _isUpdatingDurability = false;
    private bool _isInternalSearchChange = false;
    private readonly Dictionary<short, short> _initialEnchantments = new();

    public ObservableCollection<EnchantmentEntry> WorkingEnchantments { get; } = [];

    public ItemEditorDialog(ItemStack item)
    {
        InitializeComponent();

        _originalItem = item;
        _workingItem = item.Clone();

        TxtSlotLocation.Text = $"{item.Location} • Slot #{item.Slot}";

        // Setup Item Search Box
        TxtItemSearch.Text = _workingItem.Id;

        // Setup Enchantments ComboBox
        CmbEnchantments.ItemsSource = BedrockEnchantments.All;
        if (BedrockEnchantments.All.Count > 0)
        {
            CmbEnchantments.SelectedIndex = 0;
        }

        // Setup Working Collections & record initial state
        _initialEnchantments.Clear();
        foreach (var ench in _workingItem.Enchantments)
        {
            _initialEnchantments[ench.Id] = ench.Level;
            WorkingEnchantments.Add(new EnchantmentEntry(ench.Id, ench.Name, ench.Level, originalLevel: ench.Level));
        }
        LstEnchantments.ItemsSource = WorkingEnchantments;

        // Populate fields
        TxtCount.Text = _workingItem.Count == 0 ? "1" : _workingItem.Count.ToString();
        TxtCustomName.Text = _workingItem.CustomName;

        _isInitialized = true;

        UpdateDurabilityDisplay();
        UpdateLivePreview();
        UpdateDiffSummary();
    }

    private void OnItemSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || _isInternalSearchChange) return;

        var query = TxtItemSearch.Text.Trim();
        FilterSuggestions(query);

        if (TxtItemSearch.IsFocused && !PopupSuggestions.IsOpen)
        {
            PopupSuggestions.IsOpen = true;
        }

        _workingItem.Id = string.IsNullOrEmpty(query) ? "minecraft:air" : (query.Contains(':') ? query : "minecraft:" + query);
        UpdateLivePreview();
        UpdateDurabilityDisplay();
    }

    private void OnItemSearchGotFocus(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        FilterSuggestions(TxtItemSearch.Text.Trim());
        PopupSuggestions.IsOpen = true;
    }

    private void OnToggleSuggestionsClick(object sender, RoutedEventArgs e)
    {
        if (PopupSuggestions.IsOpen)
        {
            PopupSuggestions.IsOpen = false;
        }
        else
        {
            FilterSuggestions(TxtItemSearch.Text.Trim());
            PopupSuggestions.IsOpen = true;
            TxtItemSearch.Focus();
        }
    }

    private void FilterSuggestions(string query)
    {
        List<ItemDefinition> matches;
        if (string.IsNullOrWhiteSpace(query))
        {
            matches = BedrockItemRegistry.Items
                .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            var cleanQuery = query.StartsWith("minecraft:") ? query["minecraft:".Length..] : query;
            matches = BedrockItemRegistry.Items
                .Where(i => i.DisplayName.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                            i.Id.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                            i.Category.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        LstSuggestions.ItemsSource = matches;
    }

    private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LstSuggestions.SelectedItem is ItemDefinition selected)
        {
            _isInternalSearchChange = true;
            TxtItemSearch.Text = selected.Id;
            _isInternalSearchChange = false;

            PopupSuggestions.IsOpen = false;
            _workingItem.Id = selected.Id;

            UpdateLivePreview();
            UpdateDurabilityDisplay();
        }
    }

    private void OnItemSearchPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (!PopupSuggestions.IsOpen)
            {
                PopupSuggestions.IsOpen = true;
            }
            if (LstSuggestions.Items.Count > 0)
            {
                LstSuggestions.Focus();
                LstSuggestions.SelectedIndex = 0;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (LstSuggestions.SelectedItem is ItemDefinition selected)
            {
                _isInternalSearchChange = true;
                TxtItemSearch.Text = selected.Id;
                _isInternalSearchChange = false;
                _workingItem.Id = selected.Id;
            }
            PopupSuggestions.IsOpen = false;
            UpdateLivePreview();
            UpdateDurabilityDisplay();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PopupSuggestions.IsOpen = false;
            e.Handled = true;
        }
    }

    private void OnSuggestionsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && LstSuggestions.SelectedItem is ItemDefinition selected)
        {
            _isInternalSearchChange = true;
            TxtItemSearch.Text = selected.Id;
            _isInternalSearchChange = false;

            PopupSuggestions.IsOpen = false;
            _workingItem.Id = selected.Id;

            UpdateLivePreview();
            UpdateDurabilityDisplay();
            TxtItemSearch.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PopupSuggestions.IsOpen = false;
            TxtItemSearch.Focus();
            e.Handled = true;
        }
    }

    private void UpdateDurabilityDisplay()
    {
        if (!_isInitialized || PnlDurability == null || RunDurabilityRatio == null || RunDurabilityPercent == null || TxtCurrentDurability == null)
            return;

        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
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
                > 60 => new SolidColorBrush(Color.FromRgb(85, 255, 85)),
                > 25 => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                _ => new SolidColorBrush(Color.FromRgb(255, 85, 85))
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

        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':')) rawId = "minecraft:" + rawId;

        var max = BedrockItemRegistry.GetMaxDurability(rawId);
        if (max > 0)
        {
            if (int.TryParse(TxtCurrentDurability.Text, out var remaining))
            {
                if (remaining < 0) remaining = 0;
                if (remaining > max) remaining = max;

                _workingItem.Damage = (short)(max - remaining);

                var percent = (int)Math.Round((double)remaining / max * 100.0);
                RunDurabilityRatio.Text = $"({remaining} / {max})";
                RunDurabilityPercent.Text = $" • {percent}% Sisa";
                RunDurabilityPercent.Foreground = percent switch
                {
                    > 60 => new SolidColorBrush(Color.FromRgb(85, 255, 85)),
                    > 25 => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                    _ => new SolidColorBrush(Color.FromRgb(255, 85, 85))
                };
            }
        }
    }

    private void OnResetDurabilityClick(object sender, RoutedEventArgs e)
    {
        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(rawId) && !rawId.Contains(':')) rawId = "minecraft:" + rawId;

        var max = BedrockItemRegistry.GetMaxDurability(rawId);
        if (max > 0)
        {
            _workingItem.Damage = 0;
            if (_workingItem.ExtraNbt != null)
            {
                _workingItem.ExtraNbt.Remove("Damage");
                _workingItem.ExtraNbt.Remove("damage");
                _workingItem.ExtraNbt.Remove("Aux");
                _workingItem.ExtraNbt.Remove("aux");
                if (_workingItem.ExtraNbt.GetCompound("tag") is NbtCompound innerTag)
                {
                    innerTag.Remove("Damage");
                    innerTag.Remove("damage");
                    innerTag.Remove("Aux");
                    innerTag.Remove("aux");
                }
            }

            _isUpdatingDurability = true;
            TxtCurrentDurability.Text = max.ToString();
            _isUpdatingDurability = false;

            RunDurabilityRatio.Text = $"({max} / {max})";
            RunDurabilityPercent.Text = " • 100% Sisa";
            RunDurabilityPercent.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        }
    }

    private void OnFormValuesChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;

        if (byte.TryParse(TxtCount.Text, out var count))
        {
            _workingItem.Count = count;
        }

        _workingItem.CustomName = TxtCustomName.Text.Trim();

        UpdateLivePreview();
    }

    private void UpdateLivePreview()
    {
        if (!_isInitialized) return;

        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawId) || rawId == "minecraft:air")
        {
            ImgItemPreview.Source = null;
            TxtItemFallback.Visibility = Visibility.Collapsed;
            TxtPvwCount.Text = "";
            TxtItemCategory.Text = "Slot Kosong";
            TxtItemCategory.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170));
            PvwEnchantGlow.Visibility = Visibility.Collapsed;
            TxtPvwStar.Visibility = Visibility.Collapsed;
            return;
        }

        if (!rawId.Contains(':')) rawId = "minecraft:" + rawId;

        var img = ItemTextureService.GetItemImage(rawId);
        if (img != null)
        {
            ImgItemPreview.Source = img;
            ImgItemPreview.Visibility = Visibility.Visible;
            TxtItemFallback.Visibility = Visibility.Collapsed;
        }
        else
        {
            ImgItemPreview.Source = null;
            ImgItemPreview.Visibility = Visibility.Collapsed;
            TxtItemFallback.Text = BedrockItemRegistry.GetIcon(rawId);
            TxtItemFallback.Visibility = Visibility.Visible;
        }

        var count = byte.TryParse(TxtCount.Text, out var c) ? c : (byte)1;
        TxtPvwCount.Text = count > 1 ? count.ToString() : "";

        var hasEnchants = WorkingEnchantments.Count > 0;
        PvwEnchantGlow.Visibility = hasEnchants ? Visibility.Visible : Visibility.Collapsed;
        TxtPvwStar.Visibility = hasEnchants ? Visibility.Visible : Visibility.Collapsed;

        var dispName = string.IsNullOrEmpty(TxtCustomName.Text.Trim())
            ? BedrockItemRegistry.GetDisplayName(rawId)
            : TxtCustomName.Text.Trim();

        TxtItemCategory.Text = dispName;
        TxtItemCategory.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0));
    }

    private void OnAddEnchantmentClick(object sender, RoutedEventArgs e)
    {
        if (CmbEnchantments.SelectedItem is EnchantmentInfo selectedEnch &&
            int.TryParse(TxtEnchantLevel.Text, out var parsedLvl))
        {
            short lvl = (short)Math.Clamp(parsedLvl, 1, 32767);
            var origLvl = _initialEnchantments.TryGetValue(selectedEnch.Id, out var oLvl) ? (short?)oLvl : null;

            // Remove any mutually exclusive / conflicting enchantments already on this item
            var incompIds = BedrockEnchantments.GetIncompatibleEnchantmentIds(selectedEnch.Id, _workingItem.Id);
            var conflictingEntries = WorkingEnchantments.Where(en => incompIds.Contains(en.Id)).ToList();
            foreach (var conf in conflictingEntries)
            {
                WorkingEnchantments.Remove(conf);
            }

            var existing = WorkingEnchantments.FirstOrDefault(en => en.Id == selectedEnch.Id);
            if (existing != null)
            {
                var idx = WorkingEnchantments.IndexOf(existing);
                WorkingEnchantments[idx] = new EnchantmentEntry(existing.Id, existing.Name, lvl, origLvl);
            }
            else
            {
                WorkingEnchantments.Add(new EnchantmentEntry(selectedEnch.Id, selectedEnch.Name, lvl, origLvl));
            }

            UpdateLivePreview();
            UpdateDiffSummary();
        }
    }

    private void OnRemoveEnchantmentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EnchantmentEntry entry)
        {
            WorkingEnchantments.Remove(entry);
            UpdateLivePreview();
            UpdateDiffSummary();
        }
    }

    private void OnMaxValidEnchantsClick(object sender, RoutedEventArgs e)
    {
        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawId) || rawId == "minecraft:air")
        {
            MessageBox.Show(this, 
                "Slot ini kosong. Silakan pilih item terlebih dahulu sebelum memasang enchantment.", 
                "Slot Kosong", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            return;
        }

        var currentEnchantIds = WorkingEnchantments.Select(e => e.Id).ToList();
        var validEnchants = BedrockEnchantments.GetCompatibleEnchantments(rawId, currentEnchantIds);

        if (validEnchants.Count == 0)
        {
            var itemName = BedrockItemRegistry.Items.FirstOrDefault(i => i.Id.Equals(rawId, StringComparison.OrdinalIgnoreCase) || i.Id.Equals("minecraft:" + rawId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? rawId;

            MessageBox.Show(this,
                $"Item '{itemName}' tidak memiliki enchantment bawaan (vanilla) yang kompatibel secara otomatis.\n\nNamun, Anda tetap dapat menambahkan enchantment apa pun secara manual melalui menu dropdown di bawah lalu klik tombol '+ Pasang'.",
                "Info Kompatibilitas Enchantment",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        WorkingEnchantments.Clear();
        foreach (var (info, lvl) in validEnchants)
        {
            var origLvl = _initialEnchantments.TryGetValue(info.Id, out var oLvl) ? (short?)oLvl : null;
            WorkingEnchantments.Add(new EnchantmentEntry(info.Id, info.Name, lvl, origLvl));
        }

        UpdateLivePreview();
        UpdateDiffSummary();
    }

    private void UpdateDiffSummary()
    {
        var newCount = WorkingEnchantments.Count(e => e.IsNew);
        var modCount = WorkingEnchantments.Count(e => e.HasLevelChange);
        var remCount = _initialEnchantments.Keys.Count(origId => !WorkingEnchantments.Any(we => we.Id == origId));

        if (newCount > 0 || modCount > 0 || remCount > 0)
        {
            PnlDiffSummary.Visibility = Visibility.Visible;

            if (newCount > 0)
            {
                TxtDiffNew.Text = $"+{newCount} Baru";
                BadgeDiffNew.Visibility = Visibility.Visible;
            }
            else
            {
                BadgeDiffNew.Visibility = Visibility.Collapsed;
            }

            if (modCount > 0)
            {
                TxtDiffMod.Text = $"~{modCount} Diubah";
                BadgeDiffMod.Visibility = Visibility.Visible;
            }
            else
            {
                BadgeDiffMod.Visibility = Visibility.Collapsed;
            }

            if (remCount > 0)
            {
                TxtDiffRem.Text = $"-{remCount} Dihapus";
                BadgeDiffRem.Visibility = Visibility.Visible;
            }
            else
            {
                BadgeDiffRem.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            PnlDiffSummary.Visibility = Visibility.Collapsed;
        }
    }

    private void OnClearSlotClick(object sender, RoutedEventArgs e)
    {
        _isInternalSearchChange = true;
        TxtItemSearch.Text = "minecraft:air";
        _isInternalSearchChange = false;

        TxtCount.Text = "0";
        TxtCustomName.Text = "";
        WorkingEnchantments.Clear();

        _workingItem.Clear();

        UpdateLivePreview();
        UpdateDurabilityDisplay();
        UpdateDiffSummary();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var rawId = TxtItemSearch?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(rawId) || rawId == "minecraft:air")
        {
            _originalItem.Clear();
        }
        else
        {
            if (!rawId.Contains(':')) rawId = "minecraft:" + rawId;
            _originalItem.Id = rawId;
            _originalItem.Count = byte.TryParse(TxtCount.Text, out var cnt) ? cnt : (byte)1;
            _originalItem.Damage = _workingItem.Damage;
            _originalItem.CustomName = TxtCustomName.Text.Trim();

            _originalItem.Enchantments.Clear();
            foreach (var ench in WorkingEnchantments)
            {
                _originalItem.Enchantments.Add(new EnchantmentEntry(ench.Id, ench.Name, ench.Level));
            }
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
