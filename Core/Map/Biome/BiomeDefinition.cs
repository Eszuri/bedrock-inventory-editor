using System;

namespace BedrockInventoryEditor.Core.Map.Biome;

public enum BiomeCategory
{
    Ocean,
    Plains,
    Desert,
    Forest,
    Taiga,
    Jungle,
    Savanna,
    Badlands,
    Swamp,
    Mountain,
    Snowy,
    Caves,
    Nether,
    TheEnd
}

public sealed class BiomeDefinition
{
    public string Id { get; init; } = "plains";
    public string Name { get; init; } = "Plains";
    public BiomeCategory Category { get; init; } = BiomeCategory.Plains;
    public uint ColorArgb { get; init; } = 0xFF8DB360; // 0xAARRGGBB
    public double Temperature { get; init; } = 0.8;
    public double Downfall { get; init; } = 0.4;
    public bool HasStructures { get; init; } = true;

    public byte R => (byte)((ColorArgb >> 16) & 0xFF);
    public byte G => (byte)((ColorArgb >> 8) & 0xFF);
    public byte B => (byte)(ColorArgb & 0xFF);
    public byte A => (byte)((ColorArgb >> 24) & 0xFF);

    public override string ToString() => $"{Name} ({Id})";
}
