using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.Core.Nbt;
using BedrockInventoryEditor.Core.Registry;
using BedrockInventoryEditor.Core.Storage;
using BedrockInventoryEditor.UI.Controls;
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
    private bool _hasUnsavedChanges = false;
    private bool _isInitialized = false;
    private List<RecentWorldEntry> _recentWorlds = [];

    private double _playerPosX = 0;
    private double _playerPosY = 64;
    private double _playerPosZ = 0;
    private int _playerDimId = 0;

    private List<BlockEntityContainer> _allLoadedContainers = [];
    private readonly ObservableCollection<BlockEntityContainer> _displayedContainers = [];
    private readonly HashSet<BlockEntityContainer> _modifiedContainers = [];
    private BlockEntityContainer? _selectedContainer;

    public MainWindow()
    {
        InitializeComponent();

        SetupInventoryBindings();
        LstNearbyContainers.ItemsSource = _displayedContainers;
        AddHandler(InventorySlotControl.SlotClickedEvent, new RoutedEventHandler(OnInventorySlotEdited));
        ShowHomeView();
        _isInitialized = true;
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

    private void OnInventorySlotEdited(object sender, RoutedEventArgs e)
    {
        _hasUnsavedChanges = true;
        TxtStatus.Text = "Ada perubahan belum disimpan (*)";

        if (_selectedContainer != null)
        {
            _modifiedContainers.Add(_selectedContainer);
            _selectedContainer.NotifySlotsChanged();
        }
    }

    public void ShowHomeView()
    {
        if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentWorldPath))
        {
            var prompt = MessageBox.Show(
                "Anda memiliki perubahan inventaris yang belum disimpan ke LevelDB.\n\nApakah Anda ingin menyimpan perubahan sebelum kembali ke Home?",
                "Perubahan Belum Disimpan",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (prompt == MessageBoxResult.Yes)
            {
                if (!PerformSave()) return;
            }
            else if (prompt == MessageBoxResult.Cancel)
            {
                return;
            }
        }

        _hasUnsavedChanges = false;
        ViewHome.Visibility = Visibility.Visible;
        ViewEditor.Visibility = Visibility.Collapsed;
        RefreshRecentWorldsList();
    }

    public void ShowEditorView()
    {
        ViewHome.Visibility = Visibility.Collapsed;
        ViewEditor.Visibility = Visibility.Visible;
    }

    private void OnBackToHomeClick(object sender, RoutedEventArgs e)
    {
        ShowHomeView();
    }

    private void RefreshRecentWorldsList()
    {
        PnlRecentWorldsList.Children.Clear();
        _recentWorlds = RecentWorldsService.LoadRecentWorlds();

        if (_recentWorlds.Count == 0)
        {
            var emptyCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(21, 21, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 4, 0, 0)
            };
            var emptyTxt = new TextBlock
            {
                Text = "Belum ada riwayat world tersimpan.\nBuka folder world atau file .mcworld pertama Anda!",
                Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 153)),
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            emptyCard.Child = emptyTxt;
            PnlRecentWorldsList.Children.Add(emptyCard);
            return;
        }

        foreach (var r in _recentWorlds)
        {
            var btn = new Button
            {
                Tag = r.Path,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 8, 10, 8),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(44, 44, 62)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 1. Left Icon Container
            var iconBox = new Border
            {
                Width = 36,
                Height = 36,
                Background = r.IsMcWorld 
                    ? new SolidColorBrush(Color.FromRgb(42, 34, 24)) 
                    : new SolidColorBrush(Color.FromRgb(32, 32, 50)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var iconText = new TextBlock
            {
                Text = r.IsMcWorld ? "📦" : "📁",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBox.Child = iconText;
            Grid.SetColumn(iconBox, 0);
            grid.Children.Add(iconBox);

            // 2. Middle Text Info (Name & Path)
            var sp = new StackPanel 
            { 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            
            var nameBlock = new TextBlock
            {
                Text = r.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            sp.Children.Add(nameBlock);

            var pathBlock = new TextBlock
            {
                Text = r.Path,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(119, 119, 153)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            sp.Children.Add(pathBlock);

            Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);

            // 3. Right Date Badge
            var dateBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(53, 53, 74)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            var dateBlock = new TextBlock
            {
                Text = r.LastOpened.ToString("dd/MM HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                FontWeight = FontWeights.SemiBold
            };
            dateBadge.Child = dateBlock;

            Grid.SetColumn(dateBadge, 2);
            grid.Children.Add(dateBadge);

            btn.Content = grid;
            btn.Click += (s, ev) =>
            {
                if (s is Button b && b.Tag is string worldPath)
                {
                    LoadWorld(worldPath);
                }
            };

            PnlRecentWorldsList.Children.Add(btn);
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
        if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentWorldPath))
        {
            var prompt = MessageBox.Show(
                "Anda memiliki perubahan inventaris yang belum disimpan.\n\nApakah Anda ingin menyimpan perubahan sebelum membuka world baru?",
                "Perubahan Belum Disimpan",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (prompt == MessageBoxResult.Yes)
            {
                if (!PerformSave()) return;
            }
            else if (prompt == MessageBoxResult.Cancel)
            {
                return;
            }
        }

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
        TxtToolbarWorldName.Text = worldName;
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
        _hasUnsavedChanges = false;
        _modifiedContainers.Clear();

        // Extract player position and dimension
        var (px, py, pz, pDim) = BedrockWorldService.GetPlayerPosition(_currentFullPlayerNbt);
        _playerPosX = px;
        _playerPosY = py;
        _playerPosZ = pz;
        _playerDimId = pDim;

        // Load Nearby Containers
        LoadContainersForCurrentSettings();

        // Record in Recent Worlds History
        RecentWorldsService.AddRecentWorld(inputPath, worldName);

        TxtStatus.Text = $"Berhasil memuat world '{worldName}' (Player: {_currentPlayerKey}, Pos: {px:F0}, {py:F0}, {pz:F0}).";

        // Transition from Home to Editor view
        ShowEditorView();
    }

    private void OnSaveToWorldClick(object sender, RoutedEventArgs e)
    {
        PerformSave();
    }

    private bool PerformSave()
    {
        if (string.IsNullOrEmpty(_currentWorldPath))
        {
            MessageBox.Show("Silakan buka folder world terlebih dahulu.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
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
            return false;
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
                return false;
            }

            // Also save modified containers if any
            if (_modifiedContainers.Count > 0)
            {
                var (cSuccess, _, cError) = BedrockWorldService.SaveBlockEntityContainers(
                    _currentWorldPath, 
                    _modifiedContainers, 
                    createBackup: false
                );

                if (!cSuccess || cError != null)
                {
                    MessageBox.Show(
                        $"Inventaris pemain berhasil disimpan, namun terjadi kendala saat menyimpan data container:\n{cError}",
                        "Peringatan Penyimpanan Container",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    _modifiedContainers.Clear();
                }
            }

            _hasUnsavedChanges = false;

            var msg = shouldBackup && !string.IsNullOrEmpty(backupPath)
                ? $"Perubahan inventaris dan container berhasil disimpan langsung ke database LevelDB!\n\n📦 Backup dibuat di:\n{backupPath}"
                : "Perubahan inventaris dan container berhasil disimpan langsung ke database LevelDB!";

            MessageBox.Show(
                msg,
                "Berhasil Disimpan",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            TxtStatus.Text = $"Sukses disimpan pada {DateTime.Now:HH:mm:ss}.";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Terjadi error saat proses simpan:\n{ex.Message}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void LoadContainersForCurrentSettings()
    {
        if (string.IsNullOrEmpty(_currentWorldPath)) return;

        double radius = 64.0;
        if (CmbContainerRadius?.SelectedItem is ComboBoxItem item)
        {
            var text = item.Content?.ToString() ?? "";
            if (text.Contains("32")) radius = 32.0;
            else if (text.Contains("64")) radius = 64.0;
            else if (text.Contains("128")) radius = 128.0;
            else if (text.Contains("256")) radius = 256.0;
            else if (text.Contains("500")) radius = 500.0;
        }

        TxtStatus.Text = $"Memindai container di sekitar (Radius: {radius:0}m)...";

        var (containers, error) = BedrockWorldService.LoadNearbyContainers(
            _currentWorldPath, 
            _playerPosX, 
            _playerPosY, 
            _playerPosZ, 
            _playerDimId, 
            radius
        );

        if (error != null)
        {
            TxtStatus.Text = $"Gagal memindai container: {error}";
            return;
        }

        _allLoadedContainers = containers;
        ApplyContainerFilter();

        TxtStatus.Text = $"Berhasil memindai {_allLoadedContainers.Count} container di sekitar pemain.";
    }

    private void ApplyContainerFilter()
    {
        var category = (CmbContainerType?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Semua Tipe";
        var query = TxtSearchContainers?.Text?.Trim() ?? string.Empty;

        IEnumerable<BlockEntityContainer> filtered = _allLoadedContainers;

        if (!category.StartsWith("Semua"))
        {
            if (category.Contains("Peti & Tong")) filtered = filtered.Where(c => c.TypeId == "Chest" || c.TypeId == "Barrel");
            else if (category.Contains("Shulker")) filtered = filtered.Where(c => c.TypeId == "ShulkerBox");
            else if (category.Contains("Pemasak")) filtered = filtered.Where(c => c.TypeId == "Furnace" || c.TypeId == "BlastFurnace" || c.TypeId == "Smoker" || c.TypeId == "Campfire" || c.TypeId == "SoulCampfire");
            else if (category.Contains("Redstone")) filtered = filtered.Where(c => c.TypeId == "Dispenser" || c.TypeId == "Dropper" || c.TypeId == "Hopper" || c.TypeId == "Crafter");
            else filtered = filtered.Where(c => c.ContainerCategory == "Dekorasi & Buku" || c.ContainerCategory == "Lainnya" || c.ContainerCategory == "Ramuan / Brewing");
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(c => 
                c.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.CoordinatesText.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.TypeId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Slots.Any(s => !s.IsEmpty && (s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) || s.Id.Contains(query, StringComparison.OrdinalIgnoreCase)))
            );
        }

        _displayedContainers.Clear();
        foreach (var c in filtered)
        {
            _displayedContainers.Add(c);
        }

        TxtContainerCount.Text = $"{_displayedContainers.Count} Container Ditemukan";
        PnlNoContainers.Visibility = _displayedContainers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Auto select first container if available and none selected
        if (_displayedContainers.Count > 0)
        {
            if (_selectedContainer == null || !_displayedContainers.Contains(_selectedContainer))
            {
                LstNearbyContainers.SelectedIndex = 0;
            }
        }
        else
        {
            SelectContainer(null);
        }
    }

    private void OnContainerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstNearbyContainers.SelectedItem is BlockEntityContainer selected)
        {
            SelectContainer(selected);
        }
    }

    private void SelectContainer(BlockEntityContainer? container)
    {
        _selectedContainer = container;
        if (container != null)
        {
            TxtSelectedContainerName.Text = container.DisplayName;
            TxtSelectedContainerDim.Text = container.DimensionName;
            TxtSelectedContainerCoords.Text = $"Koordinat: {container.CoordinatesText} • Jarak: {container.DistanceText}";
            ImgSelectedContainer.Source = ItemTextureService.GetItemImage(container.BlockId);

            ItemsContainerSlots.Tag = container;
            ItemsContainerSlots.ItemsSource = container.Slots;
        }
        else
        {
            TxtSelectedContainerName.Text = "Pilih container untuk melihat isi";
            TxtSelectedContainerDim.Text = "-";
            TxtSelectedContainerCoords.Text = "Koordinat: -";
            ImgSelectedContainer.Source = null;
            ItemsContainerSlots.Tag = null;
            ItemsContainerSlots.ItemsSource = null;
        }
    }

    private void OnContainerFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        LoadContainersForCurrentSettings();
    }

    private void OnContainerSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        ApplyContainerFilter();
    }

    private void OnRescanContainersClick(object sender, RoutedEventArgs e)
    {
        LoadContainersForCurrentSettings();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentWorldPath))
        {
            var result = MessageBox.Show(
                "Anda memiliki perubahan inventaris yang belum disimpan ke database LevelDB.\n\nApakah Anda ingin menyimpan perubahan sebelum menutup aplikasi?",
                "Konfirmasi Keluar",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                if (!PerformSave())
                {
                    e.Cancel = true;
                    return;
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnClosing(e);
    }
}