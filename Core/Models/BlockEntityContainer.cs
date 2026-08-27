using System;
using System.Collections.ObjectModel;
using System.Linq;
using BedrockInventoryEditor.Core.Nbt;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public partial class BlockEntityContainer : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(ContainerCategory))]
    private string _typeId = string.Empty;

    [ObservableProperty]
    private string _blockId = "minecraft:chest";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _customName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoordinatesText))]
    private int _x;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoordinatesText))]
    private int _y;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoordinatesText))]
    private int _z;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimensionName))]
    private int _dimensionId = 0; // 0 = Overworld, 1 = Nether, 2 = The End

    [ObservableProperty]
    private int _chunkX;

    [ObservableProperty]
    private int _chunkZ;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText))]
    private double _distanceToPlayer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilledSlotsText))]
    private int _totalSlots = 27;

    [ObservableProperty]
    private int _gridRows = 3;

    [ObservableProperty]
    private int _gridColumns = 9;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private bool _isDoubleChest = false;

    [ObservableProperty]
    private int? _pairX;

    [ObservableProperty]
    private int? _pairZ;

    [ObservableProperty]
    private byte _pairLead = 0;

    [ObservableProperty]
    private int _pairChunkX;

    [ObservableProperty]
    private int _pairChunkZ;

    public NbtCompound PrimaryNbt { get; set; } = new();
    public NbtCompound? SecondaryNbt { get; set; }

    public ObservableCollection<ItemStack> Slots { get; } = [];

    public string DisplayName => !string.IsNullOrWhiteSpace(CustomName)
        ? CustomName
        : GetFriendlyContainerName(TypeId, IsDoubleChest);

    public string ContainerCategory => GetContainerCategory(TypeId);

    public string DimensionName => DimensionId switch
    {
        1 => "Nether",
        2 => "The End",
        _ => "Overworld"
    };

    public string CoordinatesText => $"X: {X}, Y: {Y}, Z: {Z}";

    public string DistanceText => $"{DistanceToPlayer:F1} blok";

    public string FilledSlotsText => $"{Slots.Count(s => !s.IsEmpty)} / {TotalSlots} Slot";

    public static string GetFriendlyContainerName(string typeId, bool isDouble = false)
    {
        if (isDouble) return "Peti Ganda (Double Chest)";

        return typeId switch
        {
            "Chest" => "Peti (Chest)",
            "ShulkerBox" => "Shulker Box",
            "Barrel" => "Tong (Barrel)",
            "Furnace" => "Tungku (Furnace)",
            "BlastFurnace" => "Blast Furnace",
            "Smoker" => "Smoker",
            "Campfire" => "Api Unggun (Campfire)",
            "SoulCampfire" => "Soul Campfire",
            "BrewingStand" => "Brewing Stand",
            "Dispenser" => "Dispenser",
            "Dropper" => "Dropper",
            "Hopper" => "Hopper",
            "ChiseledBookshelf" => "Chiseled Bookshelf",
            "Crafter" => "Crafter",
            "Lectern" => "Lectern",
            "Jukebox" => "Jukebox",
            _ => typeId
        };
    }

    public static string GetContainerCategory(string typeId)
    {
        return typeId switch
        {
            "Chest" or "Barrel" => "Peti & Tong",
            "ShulkerBox" => "Shulker Box",
            "Furnace" or "BlastFurnace" or "Smoker" or "Campfire" or "SoulCampfire" => "Pemasak & Tungku",
            "BrewingStand" => "Ramuan / Brewing",
            "Dispenser" or "Dropper" or "Hopper" or "Crafter" => "Redstone & Mekanik",
            "ChiseledBookshelf" or "Lectern" or "Jukebox" => "Dekorasi & Buku",
            _ => "Lainnya"
        };
    }

    public static string GetDefaultBlockId(string typeId)
    {
        return typeId switch
        {
            "Chest" => "minecraft:chest",
            "ShulkerBox" => "minecraft:shulker_box",
            "Barrel" => "minecraft:barrel",
            "Furnace" => "minecraft:furnace",
            "BlastFurnace" => "minecraft:blast_furnace",
            "Smoker" => "minecraft:smoker",
            "Campfire" => "minecraft:campfire",
            "SoulCampfire" => "minecraft:soul_campfire",
            "BrewingStand" => "minecraft:brewing_stand",
            "Dispenser" => "minecraft:dispenser",
            "Dropper" => "minecraft:dropper",
            "Hopper" => "minecraft:hopper",
            "ChiseledBookshelf" => "minecraft:chiseled_bookshelf",
            "Crafter" => "minecraft:crafter",
            "Lectern" => "minecraft:lectern",
            "Jukebox" => "minecraft:jukebox",
            _ => "minecraft:chest"
        };
    }

    public static (int slots, int rows, int cols) GetContainerDimensions(string typeId, bool isDouble = false)
    {
        if (isDouble) return (54, 6, 9);

        return typeId switch
        {
            "Chest" or "Barrel" or "ShulkerBox" => (27, 3, 9),
            "Dispenser" or "Dropper" or "Crafter" => (9, 3, 3),
            "ChiseledBookshelf" => (6, 2, 3),
            "Hopper" or "BrewingStand" => (5, 1, 5),
            "Campfire" or "SoulCampfire" => (4, 2, 2),
            "Furnace" or "BlastFurnace" or "Smoker" => (3, 1, 3),
            "Lectern" or "Jukebox" => (1, 1, 1),
            _ => (0, 0, 0)
        };
    }

    public void NotifySlotsChanged()
    {
        OnPropertyChanged(nameof(FilledSlotsText));
    }
}
