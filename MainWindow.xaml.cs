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

    private WorldSettingsModel? _worldSettings;
    private NbtCompound? _rawLevelDatNbt;
    private int _levelDatHeaderVersion = 10;

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

        // Load World Settings & Player Attributes (Tab 4)
        LoadWorldSettingsFromDisk();

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

    // =========================================================================
    // 🌍 WORLD SETTINGS & PLAYER ATTRIBUTES (TAB 4)
    // =========================================================================

    private void LoadWorldSettingsFromDisk()
    {
        if (string.IsNullOrEmpty(_currentWorldPath)) return;

        var (model, rawNbt, headerVer, error) = BedrockLevelDatService.LoadWorldSettings(_currentWorldPath);
        if (error != null || model == null)
        {
            TxtStatus.Text = $"Gagal memuat level.dat: {error}";
            return;
        }

        _worldSettings = model;
        _rawLevelDatNbt = rawNbt;
        _levelDatHeaderVersion = headerVer;

        if (_currentFullPlayerNbt != null)
        {
            BedrockWorldService.ExtractPlayerStats(_currentFullPlayerNbt, _worldSettings);
        }

        PopulateWorldSettingsUI();
    }

    private void PopulateWorldSettingsUI()
    {
        if (_worldSettings == null) return;

        TxtWorldName.Text = _worldSettings.WorldName;
        TxtWorldSeedDisplay.Text = _worldSettings.Seed.ToString();
        CmbGameMode.SelectedIndex = Math.Clamp(_worldSettings.GameType, 0, 3);
        CmbDifficulty.SelectedIndex = Math.Clamp(_worldSettings.Difficulty, 0, 3);
        ChkHardcore.IsChecked = _worldSettings.IsHardcore;

        TxtDayCount.Text = _worldSettings.DayCount.ToString();
        TxtTimeOfDay.Text = _worldSettings.TimeOfDay.ToString();
        UpdateTimeOfDayLabel(_worldSettings.TimeOfDay);
        ChkDaylightCycle.IsChecked = _worldSettings.DoDaylightCycle;

        CmbWeather.SelectedIndex = Math.Clamp(_worldSettings.WeatherType, 0, 2);
        ChkWeatherCycle.IsChecked = _worldSettings.DoWeatherCycle;

        TxtHealth.Text = _worldSettings.Health.ToString("0.#");
        TxtHunger.Text = _worldSettings.Hunger.ToString("0.#");
        TxtSaturation.Text = _worldSettings.Saturation.ToString("0.#");
        TxtXpLevel.Text = _worldSettings.XpLevel.ToString();
        TxtXpProgress.Text = ((int)(_worldSettings.XpProgress * 100)).ToString();

        TxtPlayerPosX.Text = _worldSettings.PosX.ToString("0.##");
        TxtPlayerPosY.Text = _worldSettings.PosY.ToString("0.##");
        TxtPlayerPosZ.Text = _worldSettings.PosZ.ToString("0.##");
        CmbPlayerDim.SelectedIndex = Math.Clamp(_worldSettings.Dimension, 0, 2);

        // Game Rules
        ChkFallDamage.IsChecked = _worldSettings.FallDamage;
        ChkFireDamage.IsChecked = _worldSettings.FireDamage;
        ChkDrowningDamage.IsChecked = _worldSettings.DrowningDamage;
        ChkFreezeDamage.IsChecked = _worldSettings.FreezeDamage;
        ChkKeepInventory.IsChecked = _worldSettings.KeepInventory;
        ChkMobGriefing.IsChecked = _worldSettings.MobGriefing;
        ChkMobSpawning.IsChecked = _worldSettings.DoMobSpawning;
        ChkMobLoot.IsChecked = _worldSettings.DoMobLoot;
        ChkTileDrops.IsChecked = _worldSettings.DoTileDrops;
        ChkEntityDrops.IsChecked = _worldSettings.DoEntityDrops;
        ChkNaturalRegen.IsChecked = _worldSettings.NaturalRegeneration;
        ChkPvp.IsChecked = _worldSettings.Pvp;
        ChkShowCoordinates.IsChecked = _worldSettings.ShowCoordinates;
        ChkImmediateRespawn.IsChecked = _worldSettings.DoImmediateRespawn;
        ChkTntExplodes.IsChecked = _worldSettings.TntExplodes;
        ChkShowDaysPlayed.IsChecked = _worldSettings.ShowDaysPlayed;
        TxtRandomTickSpeed.Text = _worldSettings.RandomTickSpeed.ToString();
        TxtSleepingPercent.Text = _worldSettings.PlayersSleepingPercentage.ToString();

        // Cheats & Achievements
        ChkCheatsEnabled.IsChecked = _worldSettings.CheatsEnabled;
        ChkCommandsEnabled.IsChecked = _worldSettings.CommandsEnabled;
        UpdateAchievementBadge();
    }

    private void OnCopySeedClick(object sender, RoutedEventArgs e)
    {
        if (_worldSettings != null)
        {
            Clipboard.SetText(_worldSettings.Seed.ToString());
            TxtStatus.Text = $"Seed world '{_worldSettings.Seed}' berhasil disalin ke Clipboard! 📋";
            MessageBox.Show($"Seed world berhasil disalin ke Clipboard:\n\n{_worldSettings.Seed}", "Seed Disalin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnDayDecrementClick(object sender, RoutedEventArgs e)
    {
        if (long.TryParse(TxtDayCount.Text, out var day))
        {
            TxtDayCount.Text = Math.Max(0, day - 1).ToString();
        }
    }

    private void OnDayIncrementClick(object sender, RoutedEventArgs e)
    {
        if (long.TryParse(TxtDayCount.Text, out var day))
        {
            TxtDayCount.Text = (day + 1).ToString();
        }
    }

    private void OnTimeDecrementClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtTimeOfDay.Text, out var t))
        {
            int next = (t - 1000 + 24000) % 24000;
            TxtTimeOfDay.Text = next.ToString();
        }
    }

    private void OnTimeIncrementClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtTimeOfDay.Text, out var t))
        {
            int next = (t + 1000) % 24000;
            TxtTimeOfDay.Text = next.ToString();
        }
    }

    private void OnHealthDecrementClick(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(TxtHealth.Text, out var h))
        {
            TxtHealth.Text = Math.Max(1f, h - 1f).ToString("0.#");
        }
    }

    private void OnHealthIncrementClick(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(TxtHealth.Text, out var h))
        {
            TxtHealth.Text = Math.Min(40f, h + 1f).ToString("0.#");
        }
    }

    private void OnHungerDecrementClick(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(TxtHunger.Text, out var h))
        {
            TxtHunger.Text = Math.Max(0f, h - 1f).ToString("0.#");
        }
    }

    private void OnHungerIncrementClick(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(TxtHunger.Text, out var h))
        {
            TxtHunger.Text = Math.Min(20f, h + 1f).ToString("0.#");
        }
    }

    private void OnHealthFullClick(object sender, RoutedEventArgs e) => TxtHealth.Text = "20";
    private void OnHealthDoubleClick(object sender, RoutedEventArgs e) => TxtHealth.Text = "40";
    private void OnHungerFullClick(object sender, RoutedEventArgs e) { TxtHunger.Text = "20"; TxtSaturation.Text = "20"; }
    private void OnXpLevel30Click(object sender, RoutedEventArgs e) => TxtXpLevel.Text = "30";
    private void OnXpLevel100Click(object sender, RoutedEventArgs e) => TxtXpLevel.Text = "100";
    private void OnXpLevel1000Click(object sender, RoutedEventArgs e) => TxtXpLevel.Text = "1000";

    private void UpdateAchievementBadge()
    {
        if (_worldSettings == null) return;

        if (!_worldSettings.HasBeenLoadedInCreative && !_worldSettings.CheatsEnabled)
        {
            TxtAchievementStatus.Text = "🔓 Achievement Aktif (World Murni • Belum Pernah Creative & Cheat)";
            TxtAchievementStatus.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 85));
            PnlAchievementStatus.Background = new SolidColorBrush(Color.FromRgb(26, 40, 26));
            PnlAchievementStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 90, 46));
        }
        else
        {
            TxtAchievementStatus.Text = "🔒 Achievement Terkunci (Pernah Mode Creative / Cheat Aktif)";
            TxtAchievementStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 85));
            PnlAchievementStatus.Background = new SolidColorBrush(Color.FromRgb(34, 24, 38));
            PnlAchievementStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 40, 85));
        }
    }

    private void UpdateTimeOfDayLabel(int ticks)
    {
        int totalHours = (ticks / 1000 + 6) % 24;
        int totalMinutes = (int)((ticks % 1000) * 60 / 1000.0);
        string period = totalHours switch
        {
            >= 5 and < 11 => "Pagi Hari 🌅",
            >= 11 and < 15 => "Siang Hari ☀️",
            >= 15 and < 19 => "Sore / Senja 🌇",
            _ => "Malam Hari 🌙"
        };
        TxtTimeOfDayLabel.Text = $"🕐 {totalHours:D2}:{totalMinutes:D2} ({ticks} ticks) • {period}";
    }

    private void OnTimeOfDayTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || _worldSettings == null) return;
        if (int.TryParse(TxtTimeOfDay.Text, out var ticks))
        {
            ticks = Math.Clamp(ticks, 0, 24000);
            _worldSettings.TimeOfDay = ticks;
            UpdateTimeOfDayLabel(ticks);
        }
    }

    private void OnPresetMorningClick(object sender, RoutedEventArgs e) => TxtTimeOfDay.Text = "1000";
    private void OnPresetNoonClick(object sender, RoutedEventArgs e) => TxtTimeOfDay.Text = "6000";
    private void OnPresetSunsetClick(object sender, RoutedEventArgs e) => TxtTimeOfDay.Text = "12000";
    private void OnPresetMidnightClick(object sender, RoutedEventArgs e) => TxtTimeOfDay.Text = "18000";

    private void OnRestoreAchievementsClick(object sender, RoutedEventArgs e)
    {
        if (_worldSettings == null) return;

        _worldSettings.HasBeenLoadedInCreative = false;
        _worldSettings.CheatsEnabled = false;
        _worldSettings.CommandsEnabled = false;
        ChkCheatsEnabled.IsChecked = false;
        ChkCommandsEnabled.IsChecked = false;
        UpdateAchievementBadge();

        MessageBox.Show(
            "Status Achievement Xbox berhasil dipulihkan!\n\nJangan lupa klik '💾 Simpan Pengaturan' untuk menerapkan perubahan ke file level.dat.",
            "Achievement Dipulihkan",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void OnReloadWorldSettingsClick(object sender, RoutedEventArgs e)
    {
        LoadWorldSettingsFromDisk();
        TxtStatus.Text = "Pengaturan world berhasil dimuat ulang dari disk.";
    }

    private void OnSaveWorldSettingsClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentWorldPath) || _worldSettings == null)
        {
            MessageBox.Show("Tidak ada world yang sedang terbuka.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Read UI inputs into model (Seed is NOT modified from UI!)
        _worldSettings.WorldName = string.IsNullOrWhiteSpace(TxtWorldName.Text) ? "Bedrock World" : TxtWorldName.Text.Trim();
        _worldSettings.GameType = CmbGameMode.SelectedIndex;
        _worldSettings.Difficulty = CmbDifficulty.SelectedIndex;
        _worldSettings.IsHardcore = ChkHardcore.IsChecked == true;

        if (long.TryParse(TxtDayCount.Text, out var day)) _worldSettings.DayCount = day;
        if (int.TryParse(TxtTimeOfDay.Text, out var t)) _worldSettings.TimeOfDay = Math.Clamp(t, 0, 24000);
        _worldSettings.DoDaylightCycle = ChkDaylightCycle.IsChecked == true;

        _worldSettings.WeatherType = CmbWeather.SelectedIndex;
        _worldSettings.DoWeatherCycle = ChkWeatherCycle.IsChecked == true;

        if (float.TryParse(TxtHealth.Text, out var hp))
        {
            _worldSettings.Health = Math.Clamp(hp, 1f, 40f);
            _worldSettings.MaxHealth = Math.Max(20f, _worldSettings.Health);
        }
        if (float.TryParse(TxtHunger.Text, out var hunger)) _worldSettings.Hunger = Math.Clamp(hunger, 0f, 20f);
        if (float.TryParse(TxtSaturation.Text, out var sat)) _worldSettings.Saturation = sat;
        if (int.TryParse(TxtXpLevel.Text, out var xpLvl)) _worldSettings.XpLevel = xpLvl;
        if (int.TryParse(TxtXpProgress.Text, out var xpProg)) _worldSettings.XpProgress = (float)Math.Clamp(xpProg / 100.0, 0.0, 1.0);

        if (double.TryParse(TxtPlayerPosX.Text, out var px)) _worldSettings.PosX = px;
        if (double.TryParse(TxtPlayerPosY.Text, out var py)) _worldSettings.PosY = py;
        if (double.TryParse(TxtPlayerPosZ.Text, out var pz)) _worldSettings.PosZ = pz;
        _worldSettings.Dimension = CmbPlayerDim.SelectedIndex;

        // GameRules
        _worldSettings.FallDamage = ChkFallDamage.IsChecked == true;
        _worldSettings.FireDamage = ChkFireDamage.IsChecked == true;
        _worldSettings.DrowningDamage = ChkDrowningDamage.IsChecked == true;
        _worldSettings.FreezeDamage = ChkFreezeDamage.IsChecked == true;
        _worldSettings.KeepInventory = ChkKeepInventory.IsChecked == true;
        _worldSettings.MobGriefing = ChkMobGriefing.IsChecked == true;
        _worldSettings.DoMobSpawning = ChkMobSpawning.IsChecked == true;
        _worldSettings.DoMobLoot = ChkMobLoot.IsChecked == true;
        _worldSettings.DoTileDrops = ChkTileDrops.IsChecked == true;
        _worldSettings.DoEntityDrops = ChkEntityDrops.IsChecked == true;
        _worldSettings.NaturalRegeneration = ChkNaturalRegen.IsChecked == true;
        _worldSettings.Pvp = ChkPvp.IsChecked == true;
        _worldSettings.ShowCoordinates = ChkShowCoordinates.IsChecked == true;
        _worldSettings.DoImmediateRespawn = ChkImmediateRespawn.IsChecked == true;
        _worldSettings.TntExplodes = ChkTntExplodes.IsChecked == true;
        _worldSettings.ShowDaysPlayed = ChkShowDaysPlayed.IsChecked == true;
        if (int.TryParse(TxtRandomTickSpeed.Text, out var rts)) _worldSettings.RandomTickSpeed = rts;
        if (int.TryParse(TxtSleepingPercent.Text, out var sp)) _worldSettings.PlayersSleepingPercentage = sp;

        _worldSettings.CheatsEnabled = ChkCheatsEnabled.IsChecked == true;
        _worldSettings.CommandsEnabled = ChkCommandsEnabled.IsChecked == true;

        // 1. Save level.dat
        var error = BedrockLevelDatService.SaveWorldSettings(_currentWorldPath, _worldSettings, _rawLevelDatNbt, _levelDatHeaderVersion);
        if (error != null)
        {
            MessageBox.Show($"Gagal menyimpan level.dat:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 2. Save player attributes to LevelDB
        if (_currentFullPlayerNbt != null)
        {
            BedrockWorldService.UpdatePlayerStats(_currentFullPlayerNbt, _worldSettings);
            var (pSuccess, _, pError) = BedrockWorldService.SavePlayerNbt(
                _currentWorldPath, 
                _currentFullPlayerNbt, 
                _currentPlayerKey, 
                createBackup: false, 
                hasRootHeader: _hasRootHeader
            );

            if (!pSuccess)
            {
                MessageBox.Show($"Level.dat tersimpan, tetapi gagal menyimpan status player ke LevelDB:\n{pError}", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Update titles
        TxtActiveWorldName.Text = _worldSettings.WorldName;
        TxtToolbarWorldName.Text = _worldSettings.WorldName;
        UpdateAchievementBadge();

        TxtStatus.Text = $"Pengaturan world '{_worldSettings.WorldName}' dan status pemain berhasil disimpan!";
        MessageBox.Show(
            $"Pengaturan world '{_worldSettings.WorldName}' dan atribut pemain berhasil disimpan secara aman ke level.dat dan LevelDB!",
            "Berhasil Disimpan",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
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