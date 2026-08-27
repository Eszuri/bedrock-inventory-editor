using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Storage;
using BedrockInventoryEditor.UI.Dialogs;
using Microsoft.Win32;

namespace BedrockInventoryEditor;

public partial class MainWindow : Window
{
    private readonly PlayerInventory _inventory = new();
    private NbtCompound? _currentFullPlayerNbt;
    private string? _currentWorldPath;
    private string _currentPlayerKey = "~local_player";
    private bool _hasRootHeader = true;
    private List<RecentWorldEntry> _recentWorlds = [];
    private bool _isUserSelection = false;

    public MainWindow()
    {
        InitializeComponent();

        SetupInventoryBindings();
        RefreshRecentWorlds();

        _isUserSelection = true;
    }

    private void SetupInventoryBindings()
    {
        // 1. Armor Slots
        SlotHelmet.DataContext = _inventory.Armor[0];
        SlotChestplate.DataContext = _inventory.Armor[1];
        SlotLeggings.DataContext = _inventory.Armor[2];
        SlotBoots.DataContext = _inventory.Armor[3];

        // 2. Offhand Slot
        SlotOffhand.DataContext = _inventory.Offhand[0];

        // 3. Main Bag (27 slots)
        ItemsBag.ItemsSource = _inventory.MainBag;

        // 4. Hotbar (9 slots)
        ItemsHotbar.ItemsSource = _inventory.Hotbar;

        // 5. Ender Chest (27 slots)
        ItemsEnderChest.ItemsSource = _inventory.EnderChest;
    }

    private void RefreshRecentWorlds()
    {
        var prevFlag = _isUserSelection;
        _isUserSelection = false;

        CmbRecentWorlds.Items.Clear();
        _recentWorlds = RecentWorldsService.LoadRecentWorlds();

        if (_recentWorlds.Count == 0)
        {
            CmbRecentWorlds.Items.Add(new ComboBoxItem { Content = "(Belum ada riwayat)", IsEnabled = false });
            CmbRecentWorlds.SelectedIndex = 0;
            _isUserSelection = prevFlag;
            return;
        }

        CmbRecentWorlds.Items.Add(new ComboBoxItem { Content = "(Pilih dari riwayat...)", IsEnabled = false });

        foreach (var r in _recentWorlds)
        {
            var prefix = r.IsMcWorld ? "📦 " : "📁 ";
            var item = new ComboBoxItem
            {
                Content = $"{prefix}{r.Name} ({r.LastOpened:dd/MM HH:mm})",
                Tag = r.Path
            };
            CmbRecentWorlds.Items.Add(item);
        }

        CmbRecentWorlds.SelectedIndex = 0;
        _isUserSelection = prevFlag;
    }

    private void OnRecentWorldSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUserSelection) return;

        if (CmbRecentWorlds.SelectedItem is ComboBoxItem item && item.Tag is string path)
        {
            LoadWorld(path);
        }
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Pilih Folder World Minecraft Bedrock Mana Pun",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        if (dialog.ShowDialog() == true)
        {
            LoadWorld(dialog.FolderName);
        }
    }

    private void OnOpenMcWorldClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pilih File Paket World Minecraft Bedrock (.mcworld)",
            Filter = "Minecraft World (*.mcworld;*.zip)|*.mcworld;*.zip|Semua File (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadWorld(dialog.FileName);
        }
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                LoadWorld(files[0]);
            }
        }
    }

    private void LoadWorld(string inputPath)
    {
        var resolvedDir = BedrockWorldService.ResolveWorldDirectory(inputPath);
        if (string.IsNullOrEmpty(resolvedDir) || !Directory.Exists(resolvedDir))
        {
            MessageBox.Show(
                $"Folder atau file yang dipilih tidak dapat diproses sebagai world Bedrock yang valid:\n{inputPath}",
                "Lokasi Tidak Valid",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        _currentWorldPath = resolvedDir;
        var levelNameFile = Path.Combine(resolvedDir, "levelname.txt");
        var worldName = File.Exists(levelNameFile) ? File.ReadAllText(levelNameFile).Trim() : Path.GetFileName(resolvedDir);

        TxtActiveWorldName.Text = worldName;
        TxtLoadedPath.Text = inputPath;
        TxtStatus.Text = $"Membaca data inventaris dari {worldName}...";

        var (nbt, detectedKey, hasHeader, error) = BedrockWorldService.LoadPlayerNbt(resolvedDir, _currentPlayerKey);
        if (error != null || nbt == null)
        {
            MessageBox.Show(
                $"Gagal memuat data player:\n{error}",
                "Error Memuat Data",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            TxtStatus.Text = $"Gagal memuat data: {error}";
            return;
        }

        _hasRootHeader = hasHeader;
        if (!string.IsNullOrEmpty(detectedKey))
        {
            _currentPlayerKey = detectedKey;
            TxtPlayerKey.Text = $"Key: {detectedKey}";
        }

        _currentFullPlayerNbt = nbt;
        _inventory.LoadFromPlayerNbt(_currentFullPlayerNbt);

        // Record in Recent Worlds History
        RecentWorldsService.AddRecentWorld(inputPath, worldName);
        RefreshRecentWorlds();

        TxtStatus.Text = $"Berhasil memuat world '{worldName}' (Player: {_currentPlayerKey}).";
    }

    private void OnSaveToWorldClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentWorldPath))
        {
            MessageBox.Show("Silakan buka folder world terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_currentFullPlayerNbt == null)
        {
            _currentFullPlayerNbt = new NbtCompound();
        }

        // Show choice modal: Save Direct vs Save with Backup
        var dialog = new SaveOptionsDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || dialog.Result == SaveOptionResult.Cancel)
        {
            return;
        }

        var shouldBackup = dialog.Result == SaveOptionResult.SaveWithBackup;

        try
        {
            _inventory.SaveToPlayerNbt(_currentFullPlayerNbt);

            var (success, backupPath, error) = BedrockWorldService.SavePlayerNbt(
                _currentWorldPath, 
                _currentFullPlayerNbt, 
                _currentPlayerKey, 
                createBackup: shouldBackup,
                hasRootHeader: _hasRootHeader
            );

            if (!success || error != null)
            {
                MessageBox.Show(
                    $"Gagal menyimpan perubahan ke LevelDB:\n{error}",
                    "Gagal Menyimpan",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                TxtStatus.Text = $"Error simpan: {error}";
                return;
            }

            var msg = shouldBackup && !string.IsNullOrEmpty(backupPath)
                ? $"Perubahan inventaris berhasil disimpan langsung ke database LevelDB!\n\n📦 Backup dibuat di:\n{backupPath}"
                : "Perubahan inventaris berhasil disimpan langsung ke database LevelDB!";

            MessageBox.Show(
                msg,
                "Berhasil Disimpan",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            TxtStatus.Text = $"Sukses disimpan pada {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Terjadi error saat proses simpan:\n{ex.Message}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentWorldPath))
        {
            MessageBox.Show("Silakan pilih atau muat world terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var zipPath = BedrockWorldService.CreateBackup(_currentWorldPath);
            MessageBox.Show(
                $"Cadangan world berhasil dibuat!\nLokasi file:\n{zipPath}",
                "Backup Berhasil",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            TxtStatus.Text = $"Backup tersimpan: {Path.GetFileName(zipPath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal membuat backup:\n{ex.Message}", "Error Backup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}