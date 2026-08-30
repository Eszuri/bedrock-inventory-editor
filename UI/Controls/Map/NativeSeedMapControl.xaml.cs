using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using BedrockInventoryEditor.Core.Map;
using BedrockInventoryEditor.Core.Map.Biome;
using BedrockInventoryEditor.Core.Map.Structure;
using BedrockInventoryEditor.Core.Models;

namespace BedrockInventoryEditor.UI.Controls.Map;

public class TeleportEventArgs : RoutedEventArgs
{
    public double TargetX { get; }
    public double TargetZ { get; }
    public int DimensionId { get; }

    public TeleportEventArgs(RoutedEvent routedEvent, double x, double z, int dimensionId)
        : base(routedEvent)
    {
        TargetX = x;
        TargetZ = z;
        DimensionId = dimensionId;
    }
}

public partial class NativeSeedMapControl : UserControl
{
    public static readonly RoutedEvent TeleportRequestedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TeleportRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<TeleportEventArgs>),
            typeof(NativeSeedMapControl));

    public event EventHandler<TeleportEventArgs> TeleportRequested
    {
        add => AddHandler(TeleportRequestedEvent, value);
        remove => RemoveHandler(TeleportRequestedEvent, value);
    }

    // Map state
    private long _worldSeed = 0;
    private int _dimensionId = 0;
    private double _centerX = 0;
    private double _centerZ = 0;
    private double _zoom = 0.5;

    private double _playerX = 0;
    private double _playerZ = 0;
    private int _playerDimensionId = 0;

    private double _spawnX = 0;
    private double _spawnZ = 0;

    private IEnumerable<BlockEntityContainer> _containers = [];
    private List<StructureDefinition> _visibleStructures = [];

    // Selected Pin Detail state
    private double _selectedX = 0;
    private double _selectedZ = 0;
    private int _selectedDimensionId = 0;
    private string _selectedTitle = "";

    private Point? _lastMousePos;
    private Point? _mouseDownPos;
    private bool _isPanning = false;
    private Point _hoverMousePos;

    private WriteableBitmap? _mapBitmap;
    private uint[] _sharedPixelBuffer = new uint[1920 * 1080];
    private bool _needsRender = false;
    private bool _isRenderRunning = false;
    private bool _isInitialized = false;

    // Full 28+ Structure Filter List matching Chunkbase
    private readonly List<StructureFilterItem> _structureFilters =
    [
        new(StructureType.Village, "Village", "village.png", 0),
        new(StructureType.AncientCity, "Ancient City", "ancient_city.png", 0),
        new(StructureType.Dungeon, "Dungeon", "dungeon.png", 0),
        new(StructureType.Stronghold, "Stronghold", "stronghold.png", 0),
        new(StructureType.Mansion, "Mansion", "mansion.png", 0),
        new(StructureType.Monument, "Monument", "monument.png", 0),
        new(StructureType.Outpost, "Outpost", "outpost.png", 0),
        new(StructureType.Mineshaft, "Mineshaft", "mineshaft.png", 0),
        new(StructureType.RuinedPortal, "Ruined Portal", "ruined_portal.png", -1),
        new(StructureType.JungleTemple, "Jungle Temple", "jungle_temple.png", 0),
        new(StructureType.DesertTemple, "Desert Temple", "desert_temple.png", 0),
        new(StructureType.WitchHut, "Witch Hut", "witch_hut.png", 0),
        new(StructureType.Treasure, "Treasure", "treasure.png", 0),
        new(StructureType.Shipwreck, "Shipwreck", "shipwreck.png", 0),
        new(StructureType.Igloo, "Igloo", "igloo.png", 0),
        new(StructureType.OceanRuins, "Ocean Ruins", "ocean_ruins.png", 0),
        new(StructureType.Fossil, "Fossil", "fossil.png", 0),
        new(StructureType.Cave, "Cave", "cave.png", 0),
        new(StructureType.Ravine, "Ravine", "ravine.png", 0),
        new(StructureType.LavaPool, "Lava Pool", "lava_pool.png", 0),
        new(StructureType.Geode, "Geode", "geode.png", 0),
        new(StructureType.Apple, "Apple", "apple.png", 0),
        new(StructureType.OreVeins, "Ore Veins", "ore_veins.png", 0),
        new(StructureType.DesertWell, "Desert Well", "desert_well.png", 0),
        new(StructureType.TrailRuins, "Trail Ruins", "trail_ruins.png", 0),
        new(StructureType.TrialChamber, "Trial Chamber", "trial_chamber.png", 0),
        new(StructureType.NetherFortress, "Nether Fortress", "nether_fortress.png", 1),
        new(StructureType.BastionRemnant, "Bastion", "bastion.png", 1),
        new(StructureType.EndCity, "End City", "end_city.png", 2)
    ];

    public long WorldSeed
    {
        get => _worldSeed;
        set
        {
            if (_worldSeed != value)
            {
                _worldSeed = value;
                RequestRender();
            }
        }
    }

    public int DimensionId
    {
        get => _dimensionId;
        set
        {
            int clamped = Math.Clamp(value, 0, 2);
            if (_dimensionId != clamped)
            {
                _dimensionId = clamped;
                if (CmbDimension != null && CmbDimension.SelectedIndex != clamped)
                {
                    CmbDimension.SelectedIndex = clamped;
                }
                RequestRender();
            }
        }
    }

    public double CenterX
    {
        get => _centerX;
        set { _centerX = value; RequestRender(); }
    }

    public double CenterZ
    {
        get => _centerZ;
        set { _centerZ = value; RequestRender(); }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.05, 4.0);
            TxtZoomLevel.Text = $"{(_zoom * 200):0}%";
            RequestRender();
        }
    }

    public NativeSeedMapControl()
    {
        InitializeComponent();
        CompositionTarget.Rendering += OnCompositionRendering;
        LstStructureFilters.ItemsSource = _structureFilters;
        UpdateActiveFilterCountBadge();
        _isInitialized = true;
    }

    public void SetPlayerPosition(double x, double z, int dimensionId)
    {
        _playerX = x;
        _playerZ = z;
        _playerDimensionId = dimensionId;
        RenderOverlays();
    }

    public void SetWorldSpawn(double x, double z)
    {
        _spawnX = x;
        _spawnZ = z;
        RenderOverlays();
    }

    public void SetContainers(IEnumerable<BlockEntityContainer> containers)
    {
        _containers = containers ?? [];
        RenderOverlays();
    }

    public void CenterOn(double x, double z, int? dimensionId = null)
    {
        _centerX = x;
        _centerZ = z;
        if (dimensionId.HasValue)
        {
            DimensionId = dimensionId.Value;
        }
        else
        {
            RequestRender();
        }
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RequestRender();
    }

    public void RequestRender()
    {
        _needsRender = true;
    }

    private HashSet<StructureType> GetEnabledStructureTypes()
    {
        var set = new HashSet<StructureType>();
        foreach (var item in _structureFilters)
        {
            if (item.IsChecked)
            {
                set.Add(item.Type);
            }
        }
        return set;
    }

    private void UpdateActiveFilterCountBadge()
    {
        int count = _structureFilters.Count(f => f.IsChecked);
        if (TxtActiveStructuresCount != null)
        {
            TxtActiveStructuresCount.Text = count.ToString();
        }
    }

    private void OnCompositionRendering(object? sender, EventArgs e)
    {
        if (!_needsRender || _isRenderRunning || !_isInitialized) return;
        if (ViewportGrid.ActualWidth < 10 || ViewportGrid.ActualHeight < 10) return;

        _needsRender = false;
        _isRenderRunning = true;

        int width = (int)Math.Max(10, ViewportGrid.ActualWidth);
        int height = (int)Math.Max(10, ViewportGrid.ActualHeight);

        // Ensure shared pixel buffer has sufficient capacity (Zero GC allocation)
        int totalPixels = width * height;
        if (_sharedPixelBuffer.Length < totalPixels)
        {
            _sharedPixelBuffer = new uint[Math.Max(totalPixels, _sharedPixelBuffer.Length * 2)];
        }

        double zoom = _zoom;
        int dim = _dimensionId;
        long seed = _worldSeed;
        double cx = _centerX;
        double cz = _centerZ;
        bool isPanning = _isPanning;

        bool showBiomes = ChkLayerBiomes.IsChecked == true;
        bool showGrid = ChkLayerGrid?.IsChecked == true && zoom >= 0.15;
        bool showSlime = ChkLayerSlime?.IsChecked == true && dim == 0;
        var enabledStructures = GetEnabledStructureTypes();

        // Ultra-light step: 6 during mouse drag (takes <0.2ms), 2-3 when resting (takes ~1ms)
        int step = isPanning ? 6 : (zoom < 0.35 ? 3 : 2);

        Task.Run(() =>
        {
            double halfW = width / 2.0;
            double halfH = height / 2.0;

            Parallel.For(0, (height + step - 1) / step, pyStep =>
            {
                int py = pyStep * step;
                double bz = cz + (py - halfH) / zoom;
                int chunkZ = (int)Math.Floor(bz / 16.0);
                int subZ = (int)Math.Floor(bz) & 15;

                for (int px = 0; px < width; px += step)
                {
                    double bx = cx + (px - halfW) / zoom;
                    int chunkX = (int)Math.Floor(bx / 16.0);
                    int subX = (int)Math.Floor(bx) & 15;

                    uint color = 0xFF0A0E18;

                    if (showBiomes)
                    {
                        var biome = BiomeRegistry.SampleBiome(seed, dim, bx, bz);
                        color = biome.ColorArgb;
                    }

                    if (showSlime && ChunkbaseService.IsBedrockSlimeChunk(chunkX, chunkZ))
                    {
                        color = BlendColor(color, 0xFF10B981, showBiomes ? (byte)70 : (byte)150);
                    }

                    if (showGrid && (subX == 0 || subZ == 0))
                    {
                        color = BlendColor(color, showBiomes ? 0xFF000000 : 0xFF38BDF8, (byte)80);
                    }

                    // Direct buffer block write
                    for (int dy = 0; dy < step && (py + dy) < height; dy++)
                    {
                        int rowIdx = (py + dy) * width;
                        for (int dx = 0; dx < step && (px + dx) < width; dx++)
                        {
                            _sharedPixelBuffer[rowIdx + px + dx] = color;
                        }
                    }
                }
            });

            // Fast structure scan only when not dragging
            List<StructureDefinition> structures = _visibleStructures;
            if (!isPanning && enabledStructures.Count > 0)
            {
                double minBx = cx - halfW / zoom;
                double maxBx = cx + halfW / zoom;
                double minBz = cz - halfH / zoom;
                double maxBz = cz + halfH / zoom;
                structures = StructureFinder.FindStructures(seed, dim, minBx, minBz, maxBx, maxBz, enabledStructures);
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                if (_mapBitmap == null || _mapBitmap.PixelWidth != width || _mapBitmap.PixelHeight != height)
                {
                    _mapBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
                    ImgMapRenderer.Source = _mapBitmap;
                }

                _mapBitmap.WritePixels(
                    new Int32Rect(0, 0, width, height),
                    _sharedPixelBuffer,
                    width * 4,
                    0
                );

                _visibleStructures = structures;
                RenderOverlays();

                _isRenderRunning = false;
                UpdateInspectorHud(_hoverMousePos);
            });
        });
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static uint BlendColor(uint baseColor, uint overlayColor, byte alpha)
    {
        uint invAlpha = 255u - alpha;
        uint rb = (((baseColor & 0x00FF00FFu) * invAlpha) + ((overlayColor & 0x00FF00FFu) * alpha)) >> 8;
        uint g = (((baseColor & 0x0000FF00u) * invAlpha) + ((overlayColor & 0x0000FF00u) * alpha)) >> 8;
        return 0xFF000000u | (rb & 0x00FF00FFu) | (g & 0x0000FF00u);
    }

    private void RenderOverlays()
    {
        CanvasOverlay.Children.Clear();

        int width = (int)ViewportGrid.ActualWidth;
        int height = (int)ViewportGrid.ActualHeight;
        if (width <= 0 || height <= 0) return;

        double halfW = width / 2.0;
        double halfH = height / 2.0;

        // 1. STRUCTURE PINS WITH OFFICIAL ICONS (Capped to top 40 visible pins for high performance)
        if (_visibleStructures.Count > 0)
        {
            var visiblePins = _visibleStructures.Take(40);

            foreach (var s in visiblePins)
            {
                double sx = halfW + (s.X - _centerX) * _zoom;
                double sz = halfH + (s.Z - _centerZ) * _zoom;

                if (sx < -30 || sx > width + 30 || sz < -30 || sz > height + 30) continue;

                var marker = CreateStructurePin(s);
                Canvas.SetLeft(marker, sx - 13);
                Canvas.SetTop(marker, sz - 13);
                CanvasOverlay.Children.Add(marker);
            }
        }

        // 2. LEVELDB CONTAINERS (CHESTS / BARRELS)
        if (ChkLayerContainers?.IsChecked == true && _containers.Any())
        {
            foreach (var c in _containers.Where(c => c.DimensionId == _dimensionId))
            {
                double cx = halfW + (c.X - _centerX) * _zoom;
                double cz = halfH + (c.Z - _centerZ) * _zoom;

                if (cx < -30 || cx > width + 30 || cz < -30 || cz > height + 30) continue;

                var chestPin = CreateContainerPin(c);
                Canvas.SetLeft(chestPin, cx - 11);
                Canvas.SetTop(chestPin, cz - 11);
                CanvasOverlay.Children.Add(chestPin);
            }
        }

        // 3. WORLD SPAWN PIN (0, 0 or specific spawn in Overworld)
        if (ChkLayerPlayer?.IsChecked == true && _dimensionId == 0)
        {
            double spawnPx = halfW + (_spawnX - _centerX) * _zoom;
            double spawnPz = halfH + (_spawnZ - _centerZ) * _zoom;

            if (spawnPx >= -30 && spawnPx <= width + 30 && spawnPz >= -30 && spawnPz <= height + 30)
            {
                var spawnPin = CreateSpawnPin();
                Canvas.SetLeft(spawnPin, spawnPx - 13);
                Canvas.SetTop(spawnPin, spawnPz - 13);
                CanvasOverlay.Children.Add(spawnPin);
            }
        }

        // 4. PLAYER PIN
        if (ChkLayerPlayer?.IsChecked == true && _dimensionId == _playerDimensionId)
        {
            double pPx = halfW + (_playerX - _centerX) * _zoom;
            double pPz = halfH + (_playerZ - _centerZ) * _zoom;

            if (pPx >= -40 && pPx <= width + 40 && pPz >= -40 && pPz <= height + 40)
            {
                var playerPin = CreatePlayerPin();
                Canvas.SetLeft(playerPin, pPx - 16);
                Canvas.SetTop(playerPin, pPz - 16);
                CanvasOverlay.Children.Add(playerPin);
            }
        }
    }

    private FrameworkElement CreateStructurePin(StructureDefinition s)
    {
        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 26,
            Height = 26,
            Cursor = Cursors.Hand,
            ToolTip = $"{s.Name}\n📍 Koordinat: {s.CoordinatesText}\n🌿 Bioma: {s.BiomeName}\n\nKlik untuk lihat detail lengkap atau teleportasi."
        };

        border.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            e.Handled = true;
            ShowPinDetail(s);
        };

        try
        {
            var uri = new Uri($"pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/{s.IconAsset}", UriKind.Absolute);
            var bmp = new BitmapImage(uri);
            var img = new Image
            {
                Source = bmp,
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            border.Child = img;
        }
        catch
        {
            var txt = new TextBlock
            {
                Text = s.IconEmoji,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = txt;
        }

        return border;
    }

    private FrameworkElement CreateContainerPin(BlockEntityContainer c)
    {
        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 22,
            Height = 22,
            Cursor = Cursors.Hand,
            ToolTip = $"{c.DisplayName}\nKoordinat: {c.CoordinatesText}\nTerisi: {c.FilledSlotsText}\n\nKlik untuk lihat detail."
        };

        border.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            e.Handled = true;
            ShowContainerDetail(c);
        };

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/treasure.png", UriKind.Absolute);
            var bmp = new BitmapImage(uri);
            var img = new Image
            {
                Source = bmp,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            border.Child = img;
        }
        catch
        {
            var txt = new TextBlock
            {
                Text = "📦",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = txt;
        }

        return border;
    }

    private FrameworkElement CreateSpawnPin()
    {
        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 26,
            Height = 26,
            Cursor = Cursors.Hand,
            ToolTip = $"Titik Spawn Dunia (Spawn Point)\nKoordinat: X: {_spawnX:F0}, Z: {_spawnZ:F0}\n\nKlik untuk info detail."
        };

        border.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            e.Handled = true;
            ShowSpawnDetail();
        };

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/spawn_point.png", UriKind.Absolute);
            var bmp = new BitmapImage(uri);
            var img = new Image
            {
                Source = bmp,
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            border.Child = img;
        }
        catch
        {
            var txt = new TextBlock
            {
                Text = "⭐",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Child = txt;
        }

        return border;
    }

    private FrameworkElement CreatePlayerPin()
    {
        var grid = new Grid
        {
            Width = 32,
            Height = 32,
            Cursor = Cursors.Hand,
            ToolTip = $"Posisi Pemain Aktif (~local_player)\nKoordinat: X: {_playerX:F0}, Z: {_playerZ:F0}\nDimensi: {ChunkbaseService.DimensionIdToString(_playerDimensionId)}\n\nKlik untuk info detail."
        };

        grid.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            e.Handled = true;
            ShowPlayerDetail();
        };

        // Outer pulsing ring
        var ring = new Ellipse
        {
            Width = 32,
            Height = 32,
            Fill = new SolidColorBrush(Color.FromArgb(60, 244, 63, 94)),
            Stroke = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
            StrokeThickness = 1.5
        };
        grid.Children.Add(ring);

        // Center dot
        var dot = new Border
        {
            Width = 14,
            Height = 14,
            Background = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
            BorderBrush = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(7),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(dot);

        return grid;
    }

    // =========================================================================
    // PIN DETAIL CARD & INFORMATION SYSTEM
    // =========================================================================

    public void ShowPinDetail(StructureDefinition s)
    {
        _selectedX = s.X;
        _selectedZ = s.Z;
        _selectedDimensionId = s.DimensionId;
        _selectedTitle = s.Name;

        TxtDetailTitle.Text = s.Name;
        TxtDetailDimension.Text = ChunkbaseService.DimensionIdToString(s.DimensionId).ToUpperInvariant();
        TxtDetailCoords.Text = $"X: {s.X:N0}, Z: {s.Z:N0}";

        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(s.X, s.Z);
        TxtDetailChunk.Text = $"Chunk [{cx}, {cz}] ({subX}, {subZ})";
        TxtDetailBiome.Text = s.BiomeName;

        double dist = ChunkbaseService.CalculateDistance(_playerX, _playerZ, s.X, s.Z);
        string bearing = ChunkbaseService.GetCompassDirection(_playerX, _playerZ, s.X, s.Z);
        TxtDetailDistance.Text = $"{dist:N0} blok ({bearing})";

        try
        {
            var uri = new Uri($"pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/{s.IconAsset}", UriKind.Absolute);
            ImgDetailIcon.Source = new BitmapImage(uri);
        }
        catch { }

        PnlInspectorHud.Visibility = Visibility.Collapsed;
        PnlPinDetailCard.Visibility = Visibility.Visible;
    }

    public void ShowContainerDetail(BlockEntityContainer c)
    {
        _selectedX = c.X;
        _selectedZ = c.Z;
        _selectedDimensionId = c.DimensionId;
        _selectedTitle = c.DisplayName;

        TxtDetailTitle.Text = c.DisplayName;
        TxtDetailDimension.Text = ChunkbaseService.DimensionIdToString(c.DimensionId).ToUpperInvariant();
        TxtDetailCoords.Text = $"X: {c.X:N0}, Y: {c.Y:N0}, Z: {c.Z:N0}";

        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(c.X, c.Z);
        TxtDetailChunk.Text = $"Chunk [{cx}, {cz}] ({subX}, {subZ})";
        TxtDetailBiome.Text = $"Terisi: {c.FilledSlotsText}";

        double dist = ChunkbaseService.CalculateDistance(_playerX, _playerZ, c.X, c.Z);
        string bearing = ChunkbaseService.GetCompassDirection(_playerX, _playerZ, c.X, c.Z);
        TxtDetailDistance.Text = $"{dist:N0} blok ({bearing})";

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/treasure.png", UriKind.Absolute);
            ImgDetailIcon.Source = new BitmapImage(uri);
        }
        catch { }

        PnlInspectorHud.Visibility = Visibility.Collapsed;
        PnlPinDetailCard.Visibility = Visibility.Visible;
    }

    public void ShowSpawnDetail()
    {
        _selectedX = _spawnX;
        _selectedZ = _spawnZ;
        _selectedDimensionId = 0;
        _selectedTitle = "Spawn Point";

        TxtDetailTitle.Text = "Titik Spawn Dunia";
        TxtDetailDimension.Text = "OVERWORLD";
        TxtDetailCoords.Text = $"X: {_spawnX:N0}, Z: {_spawnZ:N0}";

        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(_spawnX, _spawnZ);
        TxtDetailChunk.Text = $"Chunk [{cx}, {cz}] ({subX}, {subZ})";

        var biome = BiomeRegistry.SampleBiome(_worldSeed, 0, _spawnX, _spawnZ);
        TxtDetailBiome.Text = biome.Name;

        double dist = ChunkbaseService.CalculateDistance(_playerX, _playerZ, _spawnX, _spawnZ);
        string bearing = ChunkbaseService.GetCompassDirection(_playerX, _playerZ, _spawnX, _spawnZ);
        TxtDetailDistance.Text = $"{dist:N0} blok ({bearing})";

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/spawn_point.png", UriKind.Absolute);
            ImgDetailIcon.Source = new BitmapImage(uri);
        }
        catch { }

        PnlInspectorHud.Visibility = Visibility.Collapsed;
        PnlPinDetailCard.Visibility = Visibility.Visible;
    }

    public void ShowPlayerDetail()
    {
        _selectedX = _playerX;
        _selectedZ = _playerZ;
        _selectedDimensionId = _playerDimensionId;
        _selectedTitle = "Posisi Pemain";

        TxtDetailTitle.Text = "Pemain Aktif (~local_player)";
        TxtDetailDimension.Text = ChunkbaseService.DimensionIdToString(_playerDimensionId).ToUpperInvariant();
        TxtDetailCoords.Text = $"X: {_playerX:N0}, Z: {_playerZ:N0}";

        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(_playerX, _playerZ);
        TxtDetailChunk.Text = $"Chunk [{cx}, {cz}] ({subX}, {subZ})";

        var biome = BiomeRegistry.SampleBiome(_worldSeed, _playerDimensionId, _playerX, _playerZ);
        TxtDetailBiome.Text = biome.Name;
        TxtDetailDistance.Text = "Tepat di Posisi Pemain";

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/village.png", UriKind.Absolute);
            ImgDetailIcon.Source = new BitmapImage(uri);
        }
        catch { }

        PnlInspectorHud.Visibility = Visibility.Collapsed;
        PnlPinDetailCard.Visibility = Visibility.Visible;
    }

    public void ShowPointDetail(double bx, double bz)
    {
        _selectedX = bx;
        _selectedZ = bz;
        _selectedDimensionId = _dimensionId;

        var biome = BiomeRegistry.SampleBiome(_worldSeed, _dimensionId, bx, bz);
        _selectedTitle = biome.Name;

        TxtDetailTitle.Text = $"{GetBiomeEmoji(biome)} {biome.Name}";
        TxtDetailDimension.Text = ChunkbaseService.DimensionIdToString(_dimensionId).ToUpperInvariant();
        TxtDetailCoords.Text = $"X: {Math.Round(bx):N0}, Z: {Math.Round(bz):N0}";

        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(bx, bz);
        bool isSlime = _dimensionId == 0 && ChunkbaseService.IsBedrockSlimeChunk(cx, cz);
        TxtDetailChunk.Text = $"Chunk [{cx}, {cz}] ({subX}, {subZ})" + (isSlime ? " 🟢 Slime" : "");
        TxtDetailBiome.Text = $"{biome.Category} • {biome.Id}";

        double dist = ChunkbaseService.CalculateDistance(_playerX, _playerZ, bx, bz);
        string bearing = ChunkbaseService.GetCompassDirection(_playerX, _playerZ, bx, bz);
        TxtDetailDistance.Text = $"{dist:N0} blok ({bearing})";

        try
        {
            var uri = new Uri("pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/biomes.png", UriKind.Absolute);
            ImgDetailIcon.Source = new BitmapImage(uri);
        }
        catch { }

        PnlInspectorHud.Visibility = Visibility.Collapsed;
        PnlPinDetailCard.Visibility = Visibility.Visible;
    }

    private void OnClosePinDetailCardClick(object sender, RoutedEventArgs e)
    {
        PnlPinDetailCard.Visibility = Visibility.Collapsed;
        PnlInspectorHud.Visibility = Visibility.Visible;
    }

    private void OnDetailTeleportClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new TeleportEventArgs(TeleportRequestedEvent, _selectedX, _selectedZ, _selectedDimensionId));
    }

    private void OnDetailCopyTpClick(object sender, RoutedEventArgs e)
    {
        string tpCmd = $"/tp @s {(int)Math.Round(_selectedX)} ~ {(int)Math.Round(_selectedZ)}";
        Clipboard.SetText(tpCmd);
        MessageBox.Show($"Perintah teleportasi disalin ke clipboard:\n\n{tpCmd}", "Disalin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // =========================================================================
    // MOUSE INTERACTION & NAVIGATION
    // =========================================================================

    private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _mouseDownPos = e.GetPosition(ViewportGrid);

            if (e.ClickCount == 2)
            {
                // Double click: Teleport or center
                var clickPos = e.GetPosition(ViewportGrid);
                var (bx, bz) = ScreenToBlockCoords(clickPos);
                RaiseEvent(new TeleportEventArgs(TeleportRequestedEvent, bx, bz, _dimensionId));
            }
            else
            {
                _lastMousePos = e.GetPosition(this);
                _isPanning = true;
                Mouse.Capture((IInputElement)sender);
            }
        }
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        var currentPos = e.GetPosition(this);
        _hoverMousePos = e.GetPosition(ViewportGrid);

        if (_isPanning && _lastMousePos.HasValue)
        {
            double dx = currentPos.X - _lastMousePos.Value.X;
            double dz = currentPos.Y - _lastMousePos.Value.Y;

            _centerX -= dx / _zoom;
            _centerZ -= dz / _zoom;

            _lastMousePos = currentPos;
            RequestRender();
        }

        UpdateInspectorHud(_hoverMousePos);
    }

    private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && _isPanning)
        {
            _isPanning = false;
            Mouse.Capture(null);

            var upPos = e.GetPosition(ViewportGrid);
            if (_mouseDownPos.HasValue)
            {
                double moveDist = (upPos - _mouseDownPos.Value).Length;
                if (moveDist < 4.0)
                {
                    // Single click on map background without drag -> Inspect point!
                    var (bx, bz) = ScreenToBlockCoords(upPos);
                    ShowPointDetail(bx, bz);
                }
            }

            RequestRender();
        }
    }

    private void OnViewportMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            Mouse.Capture(null);
            RequestRender();
        }
    }

    private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mousePos = e.GetPosition(ViewportGrid);
        var (targetBx, targetBz) = ScreenToBlockCoords(mousePos);

        double oldZoom = _zoom;
        double zoomFactor = e.Delta > 0 ? 1.25 : 0.8;
        double newZoom = Math.Clamp(_zoom * zoomFactor, 0.05, 4.0);

        if (Math.Abs(newZoom - oldZoom) > 1e-4)
        {
            _centerX = targetBx - (mousePos.X - ViewportGrid.ActualWidth / 2.0) / newZoom;
            _centerZ = targetBz - (mousePos.Y - ViewportGrid.ActualHeight / 2.0) / newZoom;
            Zoom = newZoom;
            RequestRender();
        }
    }

    private (double BlockX, double BlockZ) ScreenToBlockCoords(Point p)
    {
        double halfW = ViewportGrid.ActualWidth / 2.0;
        double halfH = ViewportGrid.ActualHeight / 2.0;
        double bx = _centerX + (p.X - halfW) / _zoom;
        double bz = _centerZ + (p.Y - halfH) / _zoom;
        return (bx, bz);
    }

    private void UpdateInspectorHud(Point p)
    {
        if (ViewportGrid.ActualWidth <= 0 || ViewportGrid.ActualHeight <= 0) return;

        var (bx, bz) = ScreenToBlockCoords(p);
        int intX = (int)Math.Round(bx);
        int intZ = (int)Math.Round(bz);

        // Biome
        var biome = BiomeRegistry.SampleBiome(_worldSeed, _dimensionId, bx, bz);
        TxtHudBiome.Text = $"{GetBiomeEmoji(biome)} {biome.Name}";

        // Chunk & Slime
        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(bx, bz);
        TxtHudCoords.Text = $"📍 X: {intX:N0}, Z: {intZ:N0} • Chunk: [{cx}, {cz}] ({subX}, {subZ})";

        bool isSlime = _dimensionId == 0 && ChunkbaseService.IsBedrockSlimeChunk(cx, cz);
        BadgeSlimeIndicator.Visibility = isSlime ? Visibility.Visible : Visibility.Collapsed;

        // Distance & Bearing from player
        double dist = ChunkbaseService.CalculateDistance(_playerX, _playerZ, bx, bz);
        string bearing = ChunkbaseService.GetCompassDirection(_playerX, _playerZ, bx, bz);
        TxtHudDistance.Text = $"🧭 Jarak: {dist:N0} blok • Arah: {bearing}";

        // Nearest structure
        var nearest = _visibleStructures
            .OrderBy(s => ChunkbaseService.CalculateDistance(bx, bz, s.X, s.Z))
            .FirstOrDefault();

        if (nearest != null)
        {
            double structDist = ChunkbaseService.CalculateDistance(bx, bz, nearest.X, nearest.Z);
            TxtHudStructure.Text = $"{nearest.IconEmoji} {nearest.Name} (~{structDist:N0}m)";
        }
        else
        {
            TxtHudStructure.Text = "✨ Klik ikon untuk info lengkap atau klik kanan untuk teleportasi";
        }
    }

    private static string GetBiomeEmoji(BiomeDefinition biome)
    {
        return biome.Category switch
        {
            BiomeCategory.Ocean => "🌊",
            BiomeCategory.Desert => "🏜️",
            BiomeCategory.Forest => "🌲",
            BiomeCategory.Taiga => "🌲",
            BiomeCategory.Jungle => "🌴",
            BiomeCategory.Savanna => "🌾",
            BiomeCategory.Badlands => "🏜️",
            BiomeCategory.Swamp => "🐸",
            BiomeCategory.Mountain => "⛰️",
            BiomeCategory.Snowy => "❄️",
            BiomeCategory.Nether => "🔥",
            BiomeCategory.TheEnd => "🛸",
            _ => "🌱"
        };
    }

    // =========================================================================
    // BUTTON & CONTEXT MENU HANDLERS
    // =========================================================================

    private void OnDimensionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (CmbDimension.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int dim))
        {
            _dimensionId = dim;
            RequestRender();
        }
    }

    private void OnLayerToggleChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        RequestRender();
    }

    private void OnToggleStructureFilterClick(object sender, RoutedEventArgs e)
    {
        if (PnlStructureFilterGrid != null)
        {
            PnlStructureFilterGrid.Visibility = PnlStructureFilterGrid.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void OnCloseStructureFilterClick(object sender, RoutedEventArgs e)
    {
        if (PnlStructureFilterGrid != null)
        {
            PnlStructureFilterGrid.Visibility = Visibility.Collapsed;
        }
    }

    private void OnStructureChipClicked(object sender, RoutedEventArgs e)
    {
        UpdateActiveFilterCountBadge();
        RequestRender();
    }

    private void OnSelectAllStructuresClick(object sender, RoutedEventArgs e)
    {
        foreach (var f in _structureFilters)
        {
            f.IsChecked = true;
        }
        UpdateActiveFilterCountBadge();
        RequestRender();
    }

    private void OnDeselectAllStructuresClick(object sender, RoutedEventArgs e)
    {
        foreach (var f in _structureFilters)
        {
            f.IsChecked = false;
        }
        UpdateActiveFilterCountBadge();
        RequestRender();
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
    {
        Zoom *= 1.35;
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
    {
        Zoom *= 0.74;
    }

    private void OnCenterOnPlayerClick(object sender, RoutedEventArgs e)
    {
        _centerX = _playerX;
        _centerZ = _playerZ;
        _dimensionId = _playerDimensionId;
        if (CmbDimension != null && CmbDimension.SelectedIndex != _dimensionId)
        {
            CmbDimension.SelectedIndex = _dimensionId;
        }
        RequestRender();
    }

    private void OnCenterOnSpawnClick(object sender, RoutedEventArgs e)
    {
        _centerX = _spawnX;
        _centerZ = _spawnZ;
        _dimensionId = 0;
        if (CmbDimension != null && CmbDimension.SelectedIndex != 0)
        {
            CmbDimension.SelectedIndex = 0;
        }
        RequestRender();
    }

    private void OnContextMenuTeleportClick(object sender, RoutedEventArgs e)
    {
        var (bx, bz) = ScreenToBlockCoords(_hoverMousePos);
        RaiseEvent(new TeleportEventArgs(TeleportRequestedEvent, bx, bz, _dimensionId));
    }

    private void OnContextMenuCenterHereClick(object sender, RoutedEventArgs e)
    {
        var (bx, bz) = ScreenToBlockCoords(_hoverMousePos);
        _centerX = bx;
        _centerZ = bz;
        RequestRender();
    }

    private void OnContextMenuCopyCoordsClick(object sender, RoutedEventArgs e)
    {
        var (bx, bz) = ScreenToBlockCoords(_hoverMousePos);
        Clipboard.SetText($"{(int)Math.Round(bx)}, {(int)Math.Round(bz)}");
    }

    private void OnContextMenuCopyTpClick(object sender, RoutedEventArgs e)
    {
        var (bx, bz) = ScreenToBlockCoords(_hoverMousePos);
        string tpCmd = $"/tp @s {(int)Math.Round(bx)} ~ {(int)Math.Round(bz)}";
        Clipboard.SetText(tpCmd);
        MessageBox.Show($"Perintah teleportasi disalin ke clipboard:\n\n{tpCmd}", "Disalin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnContextMenuOpenChunkbaseClick(object sender, RoutedEventArgs e)
    {
        var (bx, bz) = ScreenToBlockCoords(_hoverMousePos);
        string url = ChunkbaseService.BuildSeedMapUrl(_worldSeed, "bedrock_1_21", ChunkbaseService.DimensionIdToString(_dimensionId), bx, bz);
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
