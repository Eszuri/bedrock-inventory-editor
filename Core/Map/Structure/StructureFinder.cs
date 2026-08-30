using System;
using System.Collections.Generic;
using BedrockInventoryEditor.Core.Map.Biome;

namespace BedrockInventoryEditor.Core.Map.Structure;

/// <summary>
/// Deterministic seed-based structure finder for Minecraft Bedrock Edition.
/// </summary>
public static class StructureFinder
{
    private record StructureConfig(
        StructureType Type, 
        string Name, 
        string IconAsset,
        string Emoji, 
        int SpacingChunks, 
        int SeparationChunks, 
        ulong Salt, 
        int DimensionId,
        Func<BiomeDefinition, bool> BiomePredicate,
        uint ColorArgb
    );

    private static readonly List<StructureConfig> OverworldConfigs =
    [
        new(
            StructureType.Village, 
            "Village", 
            "village.png",
            "🏰", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 10387312UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Plains || b.Category == BiomeCategory.Desert || b.Category == BiomeCategory.Savanna || b.Category == BiomeCategory.Taiga || b.Id == "snowy_plains" || b.Id == "meadow",
            0xFFF59E0B
        ),
        new(
            StructureType.AncientCity, 
            "Ancient City", 
            "ancient_city.png",
            "🏛️", 
            SpacingChunks: 24, 
            SeparationChunks: 8, 
            Salt: 20083232UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Mountain || b.Category == BiomeCategory.Snowy,
            0xFF06B6D4
        ),
        new(
            StructureType.TrialChamber, 
            "Trial Chamber", 
            "trial_chamber.png",
            "🗝️", 
            SpacingChunks: 34, 
            SeparationChunks: 12, 
            Salt: 94251324UL, 
            DimensionId: 0, 
            b => b.Category != BiomeCategory.Ocean,
            0xFFF97316
        ),
        new(
            StructureType.Mansion, 
            "Woodland Mansion", 
            "mansion.png",
            "🌲", 
            SpacingChunks: 80, 
            SeparationChunks: 20, 
            Salt: 10387313UL, 
            DimensionId: 0, 
            b => b.Id == "dark_forest",
            0xFF84CC16
        ),
        new(
            StructureType.Monument, 
            "Ocean Monument", 
            "monument.png",
            "🔱", 
            SpacingChunks: 32, 
            SeparationChunks: 5, 
            Salt: 10387313UL, 
            DimensionId: 0, 
            b => b.Id.Contains("deep_ocean") || b.Id.Contains("deep_cold_ocean") || b.Id.Contains("deep_frozen_ocean"),
            0xFF38BDF8
        ),
        new(
            StructureType.Outpost, 
            "Pillager Outpost", 
            "outpost.png",
            "🏹", 
            SpacingChunks: 36, 
            SeparationChunks: 10, 
            Salt: 165745296UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Plains || b.Category == BiomeCategory.Desert || b.Category == BiomeCategory.Savanna || b.Category == BiomeCategory.Taiga || b.Id == "snowy_plains",
            0xFFE11D48
        ),
        new(
            StructureType.DesertTemple, 
            "Desert Temple", 
            "desert_temple.png",
            "🗿", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 14357617UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Desert,
            0xFFEAB308
        ),
        new(
            StructureType.JungleTemple, 
            "Jungle Temple", 
            "jungle_temple.png",
            "🌴", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 14357619UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Jungle,
            0xFF10B981
        ),
        new(
            StructureType.WitchHut, 
            "Witch Hut", 
            "witch_hut.png",
            "🧙", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 14357620UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Swamp,
            0xFF8B5CF6
        ),
        new(
            StructureType.Igloo, 
            "Igloo", 
            "igloo.png",
            "❄️", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 14357618UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Snowy,
            0xFFE0E7FF
        ),
        new(
            StructureType.Shipwreck, 
            "Shipwreck", 
            "shipwreck.png",
            "⚓", 
            SpacingChunks: 24, 
            SeparationChunks: 4, 
            Salt: 165745295UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Ocean,
            0xFF0284C7
        ),
        new(
            StructureType.OceanRuins, 
            "Ocean Ruins", 
            "ocean_ruins.png",
            "🏛️", 
            SpacingChunks: 20, 
            SeparationChunks: 8, 
            Salt: 14357621UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Ocean,
            0xFF0D9488
        ),
        new(
            StructureType.Treasure, 
            "Buried Treasure", 
            "treasure.png",
            "💰", 
            SpacingChunks: 16, 
            SeparationChunks: 8, 
            Salt: 10387320UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Ocean || b.Category == BiomeCategory.Plains,
            0xFFF59E0B
        ),
        new(
            StructureType.Mineshaft, 
            "Mineshaft", 
            "mineshaft.png",
            "⛏️", 
            SpacingChunks: 16, 
            SeparationChunks: 6, 
            Salt: 1234567UL, 
            DimensionId: 0, 
            _ => true,
            0xFF78716C
        ),
        new(
            StructureType.Dungeon, 
            "Dungeon (Spawner)", 
            "dungeon.png",
            "🕸️", 
            SpacingChunks: 18, 
            SeparationChunks: 6, 
            Salt: 8462019UL, 
            DimensionId: 0, 
            _ => true,
            0xFF64748B
        ),
        new(
            StructureType.RuinedPortal, 
            "Ruined Portal", 
            "ruined_portal.png",
            "🔮", 
            SpacingChunks: 40, 
            SeparationChunks: 15, 
            Salt: 40552231UL, 
            DimensionId: 0, 
            _ => true,
            0xFF9333EA
        ),
        new(
            StructureType.Geode, 
            "Amethyst Geode", 
            "geode.png",
            "💎", 
            SpacingChunks: 24, 
            SeparationChunks: 8, 
            Salt: 98765432UL, 
            DimensionId: 0, 
            _ => true,
            0xFFA855F7
        ),
        new(
            StructureType.TrailRuins, 
            "Trail Ruins", 
            "trail_ruins.png",
            "🏺", 
            SpacingChunks: 34, 
            SeparationChunks: 10, 
            Salt: 83469123UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Taiga || b.Category == BiomeCategory.Forest,
            0xFFD97706
        ),
        new(
            StructureType.DesertWell, 
            "Desert Well", 
            "desert_well.png",
            "💧", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 7123984UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Desert,
            0xFF38BDF8
        ),
        new(
            StructureType.Fossil, 
            "Fossil", 
            "fossil.png",
            "🦴", 
            SpacingChunks: 36, 
            SeparationChunks: 12, 
            Salt: 55443322UL, 
            DimensionId: 0, 
            b => b.Category == BiomeCategory.Desert || b.Category == BiomeCategory.Swamp,
            0xFFF1F5F9
        ),
        new(
            StructureType.LavaPool, 
            "Lava Pool", 
            "lava_pool.png",
            "🔥", 
            SpacingChunks: 28, 
            SeparationChunks: 8, 
            Salt: 19283746UL, 
            DimensionId: 0, 
            _ => true,
            0xFFEF4444
        ),
        new(
            StructureType.Cave, 
            "Cave Entrance", 
            "cave.png",
            "🕳️", 
            SpacingChunks: 24, 
            SeparationChunks: 6, 
            Salt: 65748392UL, 
            DimensionId: 0, 
            _ => true,
            0xFF334155
        ),
        new(
            StructureType.Ravine, 
            "Ravine", 
            "ravine.png",
            "⛰️", 
            SpacingChunks: 30, 
            SeparationChunks: 8, 
            Salt: 77889900UL, 
            DimensionId: 0, 
            _ => true,
            0xFF475569
        ),
        new(
            StructureType.OreVeins, 
            "Large Ore Vein", 
            "ore_veins.png",
            "⛏️", 
            SpacingChunks: 32, 
            SeparationChunks: 8, 
            Salt: 33445566UL, 
            DimensionId: 0, 
            _ => true,
            0xFFF59E0B
        ),
        new(
            StructureType.Apple, 
            "Bonus Chest / Apple", 
            "apple.png",
            "🍎", 
            SpacingChunks: 48, 
            SeparationChunks: 16, 
            Salt: 11223344UL, 
            DimensionId: 0, 
            _ => true,
            0xFFE11D48
        )
    ];

    private static readonly List<StructureConfig> NetherConfigs =
    [
        new(
            StructureType.NetherFortress, 
            "Nether Fortress", 
            "nether_fortress.png",
            "🏰", 
            SpacingChunks: 27, 
            SeparationChunks: 4, 
            Salt: 30084232UL, 
            DimensionId: 1, 
            _ => true,
            0xFFEF4444
        ),
        new(
            StructureType.BastionRemnant, 
            "Bastion Remnant", 
            "bastion.png",
            "🐗", 
            SpacingChunks: 27, 
            SeparationChunks: 4, 
            Salt: 30084233UL, 
            DimensionId: 1, 
            b => b.Id != "basalt_deltas",
            0xFFF59E0B
        ),
        new(
            StructureType.RuinedPortal, 
            "Ruined Portal (Nether)", 
            "ruined_portal.png",
            "🔮", 
            SpacingChunks: 25, 
            SeparationChunks: 10, 
            Salt: 40552231UL, 
            DimensionId: 1, 
            _ => true,
            0xFF9333EA
        )
    ];

    private static readonly List<StructureConfig> EndConfigs =
    [
        new(
            StructureType.EndCity, 
            "End City", 
            "end_city.png",
            "🛸", 
            SpacingChunks: 20, 
            SeparationChunks: 11, 
            Salt: 10387313UL, 
            DimensionId: 2, 
            b => b.Id == "end_highlands" || b.Id == "end_midlands",
            0xFFA855F7
        )
    ];

    /// <summary>
    /// Finds all structures within the given bounding box in block coordinates with optional type filtering.
    /// </summary>
    public static List<StructureDefinition> FindStructures(
        long seed, 
        int dimensionId, 
        double minBlockX, 
        double minBlockZ, 
        double maxBlockX, 
        double maxBlockZ,
        HashSet<StructureType>? enabledTypes = null)
    {
        var results = new List<StructureDefinition>();

        var configs = dimensionId switch
        {
            1 => NetherConfigs,
            2 => EndConfigs,
            _ => OverworldConfigs
        };

        int minChunkX = (int)Math.Floor(minBlockX / 16.0);
        int maxChunkX = (int)Math.Ceiling(maxBlockX / 16.0);
        int minChunkZ = (int)Math.Floor(minBlockZ / 16.0);
        int maxChunkZ = (int)Math.Ceiling(maxBlockZ / 16.0);

        foreach (var cfg in configs)
        {
            if (enabledTypes != null && !enabledTypes.Contains(cfg.Type))
            {
                continue;
            }

            int spacing = cfg.SpacingChunks;
            int maxOffset = spacing - cfg.SeparationChunks;
            if (maxOffset <= 0) maxOffset = 1;

            int minRegionX = (int)Math.Floor((double)minChunkX / spacing);
            int maxRegionX = (int)Math.Floor((double)maxChunkX / spacing);
            int minRegionZ = (int)Math.Floor((double)minChunkZ / spacing);
            int maxRegionZ = (int)Math.Floor((double)maxChunkZ / spacing);

            for (int rx = minRegionX; rx <= maxRegionX; rx++)
            {
                for (int rz = minRegionZ; rz <= maxRegionZ; rz++)
                {
                    // Deterministic PRNG for structure placement in this region
                    ulong rSeed = (ulong)seed + (ulong)rx * 341873128712UL + (ulong)rz * 132897987541UL + cfg.Salt;
                    rSeed = (rSeed ^ 0x5DEECE66DUL) * 6364136223846793005UL + 1442695040888963407UL;

                    int offsetX = (int)((rSeed >> 16) % (ulong)maxOffset);
                    int offsetZ = (int)((rSeed >> 32) % (ulong)maxOffset);

                    int structChunkX = rx * spacing + offsetX;
                    int structChunkZ = rz * spacing + offsetZ;

                    int blockX = (structChunkX << 4) + 8;
                    int blockZ = (structChunkZ << 4) + 8;

                    // Bounds check
                    if (blockX < minBlockX || blockX > maxBlockX || blockZ < minBlockZ || blockZ > maxBlockZ)
                    {
                        continue;
                    }

                    // Biome validation
                    var biome = BiomeRegistry.SampleBiome(seed, dimensionId, blockX, blockZ);
                    if (cfg.BiomePredicate(biome))
                    {
                        results.Add(new StructureDefinition
                        {
                            Type = cfg.Type,
                            Name = cfg.Name,
                            IconAsset = cfg.IconAsset,
                            IconEmoji = cfg.Emoji,
                            X = blockX,
                            Z = blockZ,
                            DimensionId = dimensionId,
                            BiomeName = biome.Name,
                            ColorArgb = cfg.ColorArgb,
                            Description = $"{cfg.Name} di bioma {biome.Name} ({blockX}, {blockZ})"
                        });
                    }
                }
            }
        }

        // Add Strongholds for Overworld
        if (dimensionId == 0 && (enabledTypes == null || enabledTypes.Contains(StructureType.Stronghold)))
        {
            AddStrongholds(seed, minBlockX, minBlockZ, maxBlockX, maxBlockZ, results);
        }

        return results;
    }

    private static void AddStrongholds(long seed, double minX, double minZ, double maxX, double maxZ, List<StructureDefinition> results)
    {
        var rings = new (int count, double radius)[]
        {
            (3, 1800.0),
            (6, 4800.0),
            (10, 7800.0)
        };

        ulong rng = ((ulong)seed ^ 0x5DEECE66DUL);

        foreach (var (count, radius) in rings)
        {
            double angleStep = (2.0 * Math.PI) / count;
            rng = rng * 6364136223846793005UL + 1442695040888963407UL;
            double baseAngle = ((rng >> 32) % 1000) / 1000.0 * (2.0 * Math.PI);

            for (int i = 0; i < count; i++)
            {
                double angle = baseAngle + i * angleStep;
                rng = rng * 6364136223846793005UL + 1442695040888963407UL;
                double rVar = radius + ((rng >> 32) % 600) - 300.0;

                int bx = (int)(Math.Cos(angle) * rVar);
                int bz = (int)(Math.Sin(angle) * rVar);

                if (bx >= minX && bx <= maxX && bz >= minZ && bz <= maxZ)
                {
                    var biome = BiomeRegistry.SampleBiome(seed, 0, bx, bz);
                    results.Add(new StructureDefinition
                    {
                        Type = StructureType.Stronghold,
                        Name = "Stronghold",
                        IconAsset = "stronghold.png",
                        IconEmoji = "👁️",
                        X = bx,
                        Z = bz,
                        DimensionId = 0,
                        BiomeName = biome.Name,
                        ColorArgb = 0xFF10B981,
                        Description = $"Stronghold dengan portal End di bawah tanah ({bx}, {bz})"
                    });
                }
            }
        }
    }
}
