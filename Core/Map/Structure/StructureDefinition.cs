using System;

namespace BedrockInventoryEditor.Core.Map.Structure;

public enum StructureType
{
    Biomes,
    SpawnPoint,
    SlimeChunk,
    Village,
    AncientCity,
    Dungeon,
    Stronghold,
    Mansion,
    Monument,
    Outpost,
    Mineshaft,
    RuinedPortal,
    JungleTemple,
    DesertTemple,
    WitchHut,
    Treasure,
    Shipwreck,
    Igloo,
    OceanRuins,
    Fossil,
    Cave,
    Ravine,
    LavaPool,
    Geode,
    Apple,
    OreVeins,
    DesertWell,
    TrailRuins,
    TrialChamber,
    NetherFortress,
    BastionRemnant,
    EndCity,
    PlayerLocation,
    Container
}

public sealed class StructureDefinition
{
    public StructureType Type { get; init; } = StructureType.Village;
    public string Name { get; init; } = "Village";
    public string IconAsset { get; init; } = "village.png";
    public string IconEmoji { get; init; } = "🏰";
    public int X { get; init; }
    public int Z { get; init; }
    public int DimensionId { get; init; } = 0;
    public string BiomeName { get; init; } = "";
    public string Description { get; init; } = "";
    public uint ColorArgb { get; init; } = 0xFFF59E0B;

    public string CoordinatesText => $"X: {X}, Z: {Z}";
}
