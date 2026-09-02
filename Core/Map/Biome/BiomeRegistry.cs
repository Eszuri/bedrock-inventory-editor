using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BedrockInventoryEditor.Core.Map.Noise;

namespace BedrockInventoryEditor.Core.Map.Biome;

/// <summary>
/// 6-Parameter Climate Point in Minecraft 1.18+ Multi-Noise space.
/// </summary>
public readonly record struct ClimatePoint(float Temperature, float Humidity, float Continentalness, float Erosion, float Weirdness, float Depth);

/// <summary>
/// Target parameter entry mapping a 6D climate point to a specific Minecraft biome.
/// </summary>
public sealed class BiomeClimateEntry
{
    public BiomeDefinition Biome { get; }
    public float Temperature { get; }
    public float Humidity { get; }
    public float Continentalness { get; }
    public float Erosion { get; }
    public float Weirdness { get; }
    public float Depth { get; }

    public BiomeClimateEntry(BiomeDefinition biome, float t, float h, float c, float e, float w, float d = 0f)
    {
        Biome = biome;
        Temperature = t;
        Humidity = h;
        Continentalness = c;
        Erosion = e;
        Weirdness = w;
        Depth = d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SquaredDistance(float t, float h, float c, float e, float w, float d)
    {
        float dt = Temperature - t;
        float dh = Humidity - h;
        float dc = Continentalness - c;
        float de = Erosion - e;
        float dw = Weirdness - w;
        float dd = Depth - d;
        return 1.5f * dt * dt + 1.5f * dh * dh + 3.0f * dc * dc + 1.0f * de * de + 0.8f * dw * dw + 3.0f * dd * dd;
    }
}

/// <summary>
/// Registry of all Minecraft Bedrock biomes and high-performance 6-Parameter Multi-Noise Climate Sampler.
/// Matches Minecraft 1.18 - 1.21+ climate parameter tables.
/// </summary>
public static class BiomeRegistry
{
    // ==========================================
    // OVERWORLD SURFACE BIOMES
    // ==========================================
    public static readonly BiomeDefinition Plains = new() { Id = "plains", Name = "Plains", Category = BiomeCategory.Plains, ColorArgb = 0xFF8DB360, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition SunflowerPlains = new() { Id = "sunflower_plains", Name = "Sunflower Plains", Category = BiomeCategory.Plains, ColorArgb = 0xFFB5DB88, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition Desert = new() { Id = "desert", Name = "Desert", Category = BiomeCategory.Desert, ColorArgb = 0xFFFA9418, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition Forest = new() { Id = "forest", Name = "Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF056621, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition FlowerForest = new() { Id = "flower_forest", Name = "Flower Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF2D8E49, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition BirchForest = new() { Id = "birch_forest", Name = "Birch Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF307444, Temperature = 0.6, Downfall = 0.6 };
    public static readonly BiomeDefinition OldGrowthBirchForest = new() { Id = "old_growth_birch_forest", Name = "Old Growth Birch Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF48845A, Temperature = 0.6, Downfall = 0.6 };
    public static readonly BiomeDefinition DarkForest = new() { Id = "dark_forest", Name = "Dark Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF40511A, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition PaleGarden = new() { Id = "pale_garden", Name = "Pale Garden", Category = BiomeCategory.Forest, ColorArgb = 0xFF5B6966, Temperature = 0.1, Downfall = 0.8 };
    public static readonly BiomeDefinition Taiga = new() { Id = "taiga", Name = "Taiga", Category = BiomeCategory.Taiga, ColorArgb = 0xFF0B6659, Temperature = 0.25, Downfall = 0.8 };
    public static readonly BiomeDefinition OldGrowthPineTaiga = new() { Id = "old_growth_pine_taiga", Name = "Old Growth Pine Taiga", Category = BiomeCategory.Taiga, ColorArgb = 0xFF596651, Temperature = 0.3, Downfall = 0.8 };
    public static readonly BiomeDefinition OldGrowthSpruceTaiga = new() { Id = "old_growth_spruce_taiga", Name = "Old Growth Spruce Taiga", Category = BiomeCategory.Taiga, ColorArgb = 0xFF4A5844, Temperature = 0.25, Downfall = 0.8 };
    public static readonly BiomeDefinition SnowyTaiga = new() { Id = "snowy_taiga", Name = "Snowy Taiga", Category = BiomeCategory.Snowy, ColorArgb = 0xFF31554A, Temperature = -0.5, Downfall = 0.4 };
    public static readonly BiomeDefinition Jungle = new() { Id = "jungle", Name = "Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF537B09, Temperature = 0.95, Downfall = 0.9 };
    public static readonly BiomeDefinition SparseJungle = new() { Id = "sparse_jungle", Name = "Sparse Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF628B17, Temperature = 0.95, Downfall = 0.8 };
    public static readonly BiomeDefinition BambooJungle = new() { Id = "bamboo_jungle", Name = "Bamboo Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF768E14, Temperature = 0.95, Downfall = 0.9 };
    public static readonly BiomeDefinition Savanna = new() { Id = "savanna", Name = "Savanna", Category = BiomeCategory.Savanna, ColorArgb = 0xFFBDB25F, Temperature = 1.1, Downfall = 0.0 };
    public static readonly BiomeDefinition SavannaPlateau = new() { Id = "savanna_plateau", Name = "Savanna Plateau", Category = BiomeCategory.Savanna, ColorArgb = 0xFFA79D64, Temperature = 1.0, Downfall = 0.0 };
    public static readonly BiomeDefinition WindsweptSavanna = new() { Id = "windswept_savanna", Name = "Windswept Savanna", Category = BiomeCategory.Savanna, ColorArgb = 0xFFE5D57B, Temperature = 1.1, Downfall = 0.0 };
    public static readonly BiomeDefinition Badlands = new() { Id = "badlands", Name = "Badlands", Category = BiomeCategory.Badlands, ColorArgb = 0xFFD94515, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition WoodedBadlands = new() { Id = "wooded_badlands", Name = "Wooded Badlands", Category = BiomeCategory.Badlands, ColorArgb = 0xFFB09765, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition ErodedBadlands = new() { Id = "eroded_badlands", Name = "Eroded Badlands", Category = BiomeCategory.Badlands, ColorArgb = 0xFFFF6D3D, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition Swamp = new() { Id = "swamp", Name = "Swamp", Category = BiomeCategory.Swamp, ColorArgb = 0xFF2F6652, Temperature = 0.8, Downfall = 0.9 };
    public static readonly BiomeDefinition MangroveSwamp = new() { Id = "mangrove_swamp", Name = "Mangrove Swamp", Category = BiomeCategory.Swamp, ColorArgb = 0xFF673528, Temperature = 0.8, Downfall = 0.9 };
    public static readonly BiomeDefinition CherryGrove = new() { Id = "cherry_grove", Name = "Cherry Grove", Category = BiomeCategory.Mountain, ColorArgb = 0xFFFFB5D5, Temperature = 0.5, Downfall = 0.8 };
    public static readonly BiomeDefinition Meadow = new() { Id = "meadow", Name = "Meadow", Category = BiomeCategory.Mountain, ColorArgb = 0xFF6F9960, Temperature = 0.5, Downfall = 0.8 };
    public static readonly BiomeDefinition Grove = new() { Id = "grove", Name = "Grove", Category = BiomeCategory.Mountain, ColorArgb = 0xFF4B6659, Temperature = -0.2, Downfall = 0.8 };
    public static readonly BiomeDefinition SnowySlopes = new() { Id = "snowy_slopes", Name = "Snowy Slopes", Category = BiomeCategory.Snowy, ColorArgb = 0xFFF0F0F0, Temperature = -0.3, Downfall = 0.9 };
    public static readonly BiomeDefinition JaggedPeaks = new() { Id = "jagged_peaks", Name = "Jagged Peaks", Category = BiomeCategory.Mountain, ColorArgb = 0xFFE0E8EA, Temperature = -0.7, Downfall = 0.9 };
    public static readonly BiomeDefinition FrozenPeaks = new() { Id = "frozen_peaks", Name = "Frozen Peaks", Category = BiomeCategory.Mountain, ColorArgb = 0xFFC8E0E8, Temperature = -0.7, Downfall = 0.9 };
    public static readonly BiomeDefinition StonyPeaks = new() { Id = "stony_peaks", Name = "Stony Peaks", Category = BiomeCategory.Mountain, ColorArgb = 0xFF888888, Temperature = 1.0, Downfall = 0.3 };
    public static readonly BiomeDefinition WindsweptHills = new() { Id = "windswept_hills", Name = "Windswept Hills", Category = BiomeCategory.Mountain, ColorArgb = 0xFF606060, Temperature = 0.2, Downfall = 0.3 };
    public static readonly BiomeDefinition WindsweptForest = new() { Id = "windswept_forest", Name = "Windswept Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF587050, Temperature = 0.2, Downfall = 0.3 };
    public static readonly BiomeDefinition SnowyPlains = new() { Id = "snowy_plains", Name = "Snowy Plains", Category = BiomeCategory.Snowy, ColorArgb = 0xFFFFFFFF, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition IceSpikes = new() { Id = "ice_spikes", Name = "Ice Spikes", Category = BiomeCategory.Snowy, ColorArgb = 0xFFB4DCDC, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition MushroomFields = new() { Id = "mushroom_fields", Name = "Mushroom Fields", Category = BiomeCategory.Plains, ColorArgb = 0xFFFF00FF, Temperature = 0.9, Downfall = 1.0 };
    public static readonly BiomeDefinition Beach = new() { Id = "beach", Name = "Beach", Category = BiomeCategory.Plains, ColorArgb = 0xFFFADE55, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition SnowyBeach = new() { Id = "snowy_beach", Name = "Snowy Beach", Category = BiomeCategory.Snowy, ColorArgb = 0xFFFAF0E6, Temperature = 0.05, Downfall = 0.3 };
    public static readonly BiomeDefinition StonyShore = new() { Id = "stony_shore", Name = "Stony Shore", Category = BiomeCategory.Mountain, ColorArgb = 0xFFA0A0A0, Temperature = 0.2, Downfall = 0.3 };

    // Oceans
    public static readonly BiomeDefinition WarmOcean = new() { Id = "warm_ocean", Name = "Warm Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF0066FF, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition LukewarmOcean = new() { Id = "lukewarm_ocean", Name = "Lukewarm Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF0055D4, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepLukewarmOcean = new() { Id = "deep_lukewarm_ocean", Name = "Deep Lukewarm Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF003D99, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition Ocean = new() { Id = "ocean", Name = "Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF003EAA, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepOcean = new() { Id = "deep_ocean", Name = "Deep Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF002277, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition ColdOcean = new() { Id = "cold_ocean", Name = "Cold Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF203070, Temperature = 0.0, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepColdOcean = new() { Id = "deep_cold_ocean", Name = "Deep Cold Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF101B4D, Temperature = 0.0, Downfall = 0.5 };
    public static readonly BiomeDefinition FrozenOcean = new() { Id = "frozen_ocean", Name = "Frozen Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF7090BA, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepFrozenOcean = new() { Id = "deep_frozen_ocean", Name = "Deep Frozen Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF405580, Temperature = -0.5, Downfall = 0.5 };

    // Underground Caves (Depth > 0.2)
    public static readonly BiomeDefinition DeepDark = new() { Id = "deep_dark", Name = "Deep Dark", Category = BiomeCategory.Caves, ColorArgb = 0xFF0B1E28, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition LushCaves = new() { Id = "lush_caves", Name = "Lush Caves", Category = BiomeCategory.Caves, ColorArgb = 0xFF3B5E28, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DripstoneCaves = new() { Id = "dripstone_caves", Name = "Dripstone Caves", Category = BiomeCategory.Caves, ColorArgb = 0xFF8C7156, Temperature = 0.8, Downfall = 0.4 };

    // ==========================================
    // NETHER BIOMES
    // ==========================================
    public static readonly BiomeDefinition NetherWastes = new() { Id = "nether_wastes", Name = "Nether Wastes", Category = BiomeCategory.Nether, ColorArgb = 0xFFBF3B3B, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition CrimsonForest = new() { Id = "crimson_forest", Name = "Crimson Forest", Category = BiomeCategory.Nether, ColorArgb = 0xFF991515, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition WarpedForest = new() { Id = "warped_forest", Name = "Warped Forest", Category = BiomeCategory.Nether, ColorArgb = 0xFF147B78, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition SoulSandValley = new() { Id = "soul_sand_valley", Name = "Soul Sand Valley", Category = BiomeCategory.Nether, ColorArgb = 0xFF5E493E, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition BasaltDeltas = new() { Id = "basalt_deltas", Name = "Basalt Deltas", Category = BiomeCategory.Nether, ColorArgb = 0xFF403636, Temperature = 2.0, Downfall = 0.0 };

    // ==========================================
    // THE END BIOMES
    // ==========================================
    public static readonly BiomeDefinition TheEnd = new() { Id = "the_end", Name = "The End (Center Island)", Category = BiomeCategory.TheEnd, ColorArgb = 0xFF8080FF, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition EndHighlands = new() { Id = "end_highlands", Name = "End Highlands", Category = BiomeCategory.TheEnd, ColorArgb = 0xFFB8B8FF, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition EndMidlands = new() { Id = "end_midlands", Name = "End Midlands", Category = BiomeCategory.TheEnd, ColorArgb = 0xFFC8C8FF, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition SmallEndIslands = new() { Id = "small_end_islands", Name = "Small End Islands", Category = BiomeCategory.TheEnd, ColorArgb = 0xFF404080, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition EndBarrens = new() { Id = "end_barrens", Name = "End Barrens", Category = BiomeCategory.TheEnd, ColorArgb = 0xFF606090, Temperature = 0.5, Downfall = 0.5 };

    // ==========================================
    // 6-PARAMETER OVERWORLD CLIMATE TABLE (Minecraft 1.18 - 1.21+)
    // ==========================================
    private static readonly BiomeClimateEntry[] OverworldClimateEntries =
    [
        // 1. Mushroom Fields & Oceans (Continentalness < -0.11)
        new(MushroomFields, 0.0f, 0.0f, -1.15f, 0.0f, 0.0f),
        new(DeepFrozenOcean, -0.7f, 0.0f, -0.75f, 0.0f, 0.0f),
        new(FrozenOcean, -0.7f, 0.0f, -0.32f, 0.0f, 0.0f),
        new(DeepColdOcean, -0.3f, 0.0f, -0.75f, 0.0f, 0.0f),
        new(ColdOcean, -0.3f, 0.0f, -0.32f, 0.0f, 0.0f),
        new(DeepOcean, 0.0f, 0.0f, -0.75f, 0.0f, 0.0f),
        new(Ocean, 0.0f, 0.0f, -0.32f, 0.0f, 0.0f),
        new(DeepLukewarmOcean, 0.5f, 0.0f, -0.75f, 0.0f, 0.0f),
        new(LukewarmOcean, 0.5f, 0.0f, -0.32f, 0.0f, 0.0f),
        new(WarmOcean, 0.9f, 0.4f, -0.32f, 0.0f, 0.0f),

        // 2. Coasts / Beaches (Continentalness ~ -0.15)
        new(SnowyBeach, -0.6f, 0.0f, -0.15f, 0.0f, 0.0f),
        new(StonyShore, 0.0f, 0.0f, -0.15f, -0.6f, 0.0f),
        new(Beach, 0.2f, 0.0f, -0.15f, 0.1f, 0.0f),

        // 3. Hot / Arid Climates
        new(Desert, 0.8f, -0.7f, 0.2f, 0.0f, 0.0f),
        new(Badlands, 0.8f, -0.4f, 0.2f, 0.0f, 0.5f),
        new(WoodedBadlands, 0.8f, -0.2f, 0.4f, -0.4f, 0.5f),
        new(ErodedBadlands, 0.8f, -0.5f, 0.3f, -0.8f, 0.8f),
        new(Savanna, 0.6f, -0.3f, 0.1f, 0.2f, 0.0f),
        new(SavannaPlateau, 0.6f, -0.3f, 0.4f, -0.4f, 0.0f),
        new(WindsweptSavanna, 0.6f, -0.3f, 0.2f, -0.7f, 0.6f),

        // 4. Hot / Wet Climates
        new(Jungle, 0.8f, 0.7f, 0.2f, 0.0f, 0.0f),
        new(SparseJungle, 0.7f, 0.4f, 0.1f, 0.1f, 0.0f),
        new(BambooJungle, 0.8f, 0.8f, 0.3f, -0.2f, 0.6f),

        // 5. Temperate Climates
        new(Plains, 0.0f, -0.2f, 0.1f, 0.3f, 0.0f),
        new(SunflowerPlains, 0.0f, -0.2f, 0.1f, 0.3f, 0.6f),
        new(Forest, 0.0f, 0.3f, 0.1f, 0.1f, 0.0f),
        new(FlowerForest, 0.0f, 0.3f, 0.1f, 0.1f, 0.6f),
        new(BirchForest, 0.1f, 0.1f, 0.2f, 0.2f, 0.3f),
        new(OldGrowthBirchForest, 0.1f, 0.3f, 0.3f, 0.0f, 0.4f),
        new(DarkForest, 0.1f, 0.7f, 0.3f, 0.1f, 0.4f),
        new(PaleGarden, 0.1f, 0.7f, 0.3f, 0.1f, 0.55f),
        new(Swamp, 0.4f, 0.6f, 0.1f, 0.7f, -0.2f),
        new(MangroveSwamp, 0.7f, 0.8f, 0.1f, 0.7f, -0.2f),

        // 6. Cool / Taiga Climates
        new(Taiga, -0.3f, 0.3f, 0.2f, 0.1f, 0.0f),
        new(OldGrowthPineTaiga, -0.3f, 0.5f, 0.4f, 0.0f, 0.4f),
        new(OldGrowthSpruceTaiga, -0.3f, 0.6f, 0.5f, -0.2f, 0.5f),
        new(WindsweptHills, -0.2f, 0.0f, 0.2f, -0.6f, 0.2f),
        new(WindsweptForest, -0.2f, 0.3f, 0.2f, -0.6f, 0.5f),

        // 7. Cold / Snowy Climates
        new(SnowyPlains, -0.7f, -0.2f, 0.2f, 0.3f, 0.0f),
        new(IceSpikes, -0.7f, -0.2f, 0.3f, 0.3f, 0.8f),
        new(SnowyTaiga, -0.7f, 0.3f, 0.2f, 0.1f, 0.0f),

        // 8. Mountain Peaks & Highlands (Erosion < -0.3)
        new(Meadow, 0.0f, -0.1f, 0.3f, -0.5f, -0.3f),
        new(CherryGrove, 0.1f, 0.2f, 0.3f, -0.5f, 0.7f),
        new(Grove, -0.4f, 0.2f, 0.3f, -0.5f, -0.2f),
        new(SnowySlopes, -0.7f, 0.0f, 0.4f, -0.6f, -0.3f),
        new(JaggedPeaks, -0.7f, 0.0f, 0.5f, -0.9f, 0.0f),
        new(FrozenPeaks, -0.7f, 0.0f, 0.5f, -0.9f, 0.5f),
        new(StonyPeaks, 0.4f, -0.2f, 0.5f, -0.9f, 0.0f),

        // 9. Underground Caves (Depth > 0.2)
        new(DeepDark, 0.0f, 0.0f, 0.3f, -0.8f, 0.0f, 0.8f),
        new(LushCaves, 0.3f, 0.7f, 0.2f, 0.0f, 0.0f, 0.5f),
        new(DripstoneCaves, 0.2f, -0.3f, 0.2f, 0.0f, 0.0f, 0.5f)
    ];

    // ==========================================
    // 2-PARAMETER NETHER CLIMATE TABLE
    // ==========================================
    private static readonly (BiomeDefinition Biome, float Temperature, float Humidity)[] NetherClimateEntries =
    [
        (NetherWastes, 0.0f, 0.0f),
        (CrimsonForest, 0.4f, 0.0f),
        (WarpedForest, 0.0f, 0.5f),
        (SoulSandValley, -0.4f, 0.0f),
        (BasaltDeltas, -0.4f, -0.4f)
    ];

    // Noise Generator Cache by Seed
    private static readonly ConcurrentDictionary<long, NoiseOctaveSuite> NoiseCache = new();

    private sealed class NoiseOctaveSuite
    {
        public SeedNoise Continentalness { get; }
        public SeedNoise Temperature { get; }
        public SeedNoise Humidity { get; }
        public SeedNoise Erosion { get; }
        public SeedNoise Weirdness { get; }
        public SeedNoise WarpX { get; }
        public SeedNoise WarpZ { get; }

        public NoiseOctaveSuite(long seed)
        {
            Continentalness = new SeedNoise(seed);
            Temperature = new SeedNoise(seed ^ unchecked((long)0x9E3779B97F4A7C15UL));
            Humidity = new SeedNoise(seed ^ unchecked((long)0xBF58476D1CE4E5B9UL));
            Erosion = new SeedNoise(seed ^ unchecked((long)0x94D049BB133111EBUL));
            Weirdness = new SeedNoise(seed ^ 0x2545F4914F6CDD1DL);
            WarpX = new SeedNoise(seed ^ 0x5241617265646174L);
            WarpZ = new SeedNoise(seed ^ 0x6E6F697365777073L);
        }
    }

    private static NoiseOctaveSuite GetSuite(long seed)
    {
        return NoiseCache.GetOrAdd(seed, s => new NoiseOctaveSuite(s));
    }

    /// <summary>
    /// Samples the 6-parameter climate point at block (X, Z).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClimatePoint SampleClimatePoint(long seed, double x, double z, float depth = 0f)
    {
        var suite = GetSuite(seed);

        double wx = x + suite.WarpX.Evaluate2D(x * 0.0008, z * 0.0008) * 32.0;
        double wz = z + suite.WarpZ.Evaluate2D(x * 0.0008, z * 0.0008) * 32.0;

        float cont = (float)suite.Continentalness.EvaluateFbm(wx, wz, frequency: 0.00035, octaves: 3);
        float temp = (float)suite.Temperature.EvaluateFbm(wx, wz, frequency: 0.0005, octaves: 3);
        float hum = (float)suite.Humidity.EvaluateFbm(wx, wz, frequency: 0.0005, octaves: 3);
        float eros = (float)suite.Erosion.EvaluateFbm(wx, wz, frequency: 0.0007, octaves: 3);
        float weird = (float)suite.Weirdness.EvaluateFbm(wx, wz, frequency: 0.0008, octaves: 3);

        return new ClimatePoint(temp, hum, cont, eros, weird, depth);
    }

    /// <summary>
    /// Samples the biome at the given block coordinates (X, Z) and dimension.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BiomeDefinition SampleBiome(long seed, int dimensionId, double blockX, double blockZ, float depth = 0f)
    {
        return dimensionId switch
        {
            1 => SampleNetherBiome(seed, blockX, blockZ),
            2 => SampleEndBiome(seed, blockX, blockZ),
            _ => SampleOverworldBiome(seed, blockX, blockZ, depth)
        };
    }

    /// <summary>
    /// Overworld multi-noise nearest neighbor lookup in 6D parameter space.
    /// <summary>
    /// Finds the closest Overworld biome matching the specified 6D climate parameters.
    /// </summary>
    public static BiomeDefinition FindClosestOverworldBiome(float temperature, float humidity, float continentalness, float erosion, float weirdness, float depth = 0f)
    {
        float minDistanceSq = float.MaxValue;
        BiomeDefinition bestBiome = Plains;

        for (int i = 0; i < OverworldClimateEntries.Length; i++)
        {
            var entry = OverworldClimateEntries[i];
            float distSq = entry.SquaredDistance(temperature, humidity, continentalness, erosion, weirdness, depth);

            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                bestBiome = entry.Biome;
            }
        }

        return bestBiome;
    }

    /// <summary>
    /// Overworld multi-noise lookup with 6 parameters (T, H, C, E, W, D).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BiomeDefinition SampleOverworldBiome(long seed, double x, double z, float depth)
    {
        var p = SampleClimatePoint(seed, x, z, depth);
        return FindClosestOverworldBiome(p.Temperature, p.Humidity, p.Continentalness, p.Erosion, p.Weirdness, p.Depth);
    }

    /// <summary>
    /// Nether multi-noise lookup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BiomeDefinition SampleNetherBiome(long seed, double x, double z)
    {
        var suite = GetSuite(seed);
        float temp = (float)suite.Temperature.EvaluateFbm(x, z, frequency: 0.002, octaves: 2);
        float hum = (float)suite.Humidity.EvaluateFbm(x, z, frequency: 0.002, octaves: 2);

        float minDistanceSq = float.MaxValue;
        BiomeDefinition bestBiome = NetherWastes;

        for (int i = 0; i < NetherClimateEntries.Length; i++)
        {
            var entry = NetherClimateEntries[i];
            float dt = entry.Temperature - temp;
            float dh = entry.Humidity - hum;
            float distSq = dt * dt + dh * dh;

            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                bestBiome = entry.Biome;
            }
        }

        return bestBiome;
    }

    /// <summary>
    /// The End dimension island distribution sampler.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BiomeDefinition SampleEndBiome(long seed, double x, double z)
    {
        double distSq = x * x + z * z;
        // Central End Island (< 800 blocks radius)
        if (distSq < 800 * 800)
        {
            return TheEnd;
        }

        var suite = GetSuite(seed);
        double n = suite.Continentalness.EvaluateFbm(x, z, frequency: 0.003, octaves: 2);

        if (n > 0.25) return EndHighlands;
        if (n > 0.0) return EndMidlands;
        if (n > -0.25) return EndBarrens;
        return SmallEndIslands;
    }
}
