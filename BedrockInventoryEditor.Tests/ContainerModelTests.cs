using BedrockInventoryEditor.Core.Models;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class ContainerModelTests
{
    [Theory]
    [InlineData("Chest", false, 27, 3, 9)]
    [InlineData("Chest", true, 54, 6, 9)]
    [InlineData("Barrel", false, 27, 3, 9)]
    [InlineData("ShulkerBox", false, 27, 3, 9)]
    [InlineData("Dispenser", false, 9, 3, 3)]
    [InlineData("Dropper", false, 9, 3, 3)]
    [InlineData("Crafter", false, 9, 3, 3)]
    [InlineData("ChiseledBookshelf", false, 6, 2, 3)]
    [InlineData("Hopper", false, 5, 1, 5)]
    [InlineData("BrewingStand", false, 5, 1, 5)]
    [InlineData("Campfire", false, 4, 2, 2)]
    [InlineData("SoulCampfire", false, 4, 2, 2)]
    [InlineData("Furnace", false, 3, 1, 3)]
    [InlineData("BlastFurnace", false, 3, 1, 3)]
    [InlineData("Smoker", false, 3, 1, 3)]
    [InlineData("Lectern", false, 1, 1, 1)]
    [InlineData("Jukebox", false, 1, 1, 1)]
    [InlineData("CopperChest", false, 27, 3, 9)]
    [InlineData("CopperChest", true, 54, 6, 9)]
    [InlineData("DecoratedPot", false, 1, 1, 1)]
    [InlineData("Shelf", false, 6, 2, 3)]
    [InlineData("WoodShelf", false, 6, 2, 3)]
    public void ContainerDimensions_AreCorrect(string typeId, bool isDouble, int expectedSlots, int expectedRows, int expectedCols)
    {
        var (slots, rows, cols) = BlockEntityContainer.GetContainerDimensions(typeId, isDouble);
        Assert.Equal(expectedSlots, slots);
        Assert.Equal(expectedRows, rows);
        Assert.Equal(expectedCols, cols);
    }

    [Fact]
    public void BlockEntityContainer_PropertiesAndFormatting()
    {
        var container = new BlockEntityContainer
        {
            TypeId = "Chest",
            X = 10,
            Y = 64,
            Z = -50,
            DistanceToPlayer = 4.3,
            TotalSlots = 27
        };

        Assert.Equal("Peti (Chest)", container.DisplayName);
        Assert.Equal("X: 10, Y: 64, Z: -50", container.CoordinatesText);
        Assert.Equal("4.3 blok", container.DistanceText);
        Assert.Equal("0 / 27 Slot", container.FilledSlotsText);

        // Add 1 item
        container.Slots.Add(new ItemStack(0, SlotLocation.Container) { Id = "minecraft:diamond", Count = 1 });
        container.NotifySlotsChanged();
        Assert.Equal("1 / 27 Slot", container.FilledSlotsText);
    }
}
