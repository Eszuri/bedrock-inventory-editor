using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using BedrockInventoryEditor.Core.Map.Noise;

namespace BedrockInventoryEditor.Core.Map.Biome;

/// <summary>
/// Registry of all Minecraft Bedrock biomes and high-performance procedural climate/multi-noise sampler.
/// </summary>
public static class BiomeRegistry
{
    // ==========================================
    // OVERWORLD BIOMES
    // ==========================================
    public static readonly BiomeDefinition Plains = new() { Id = "plains", Name = "Plains", Category = BiomeCategory.Plains, ColorArgb = 0xFF8DB360, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition SunflowerPlains = new() { Id = "sunflower_plains", Name = "Sunflower Plains", Category = BiomeCategory.Plains, ColorArgb = 0xFFB5DB88, Temperature = 0.8, Downfall = 0.4 };
    public static readonly BiomeDefinition Desert = new() { Id = "desert", Name = "Desert", Category = BiomeCategory.Desert, ColorArgb = 0xFFFA9418, Temperature = 2.0, Downfall = 0.0 };
    public static readonly BiomeDefinition Forest = new() { Id = "forest", Name = "Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF056621, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition FlowerForest = new() { Id = "flower_forest", Name = "Flower Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF2D8E49, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition BirchForest = new() { Id = "birch_forest", Name = "Birch Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF307444, Temperature = 0.6, Downfall = 0.6 };
    public static readonly BiomeDefinition DarkForest = new() { Id = "dark_forest", Name = "Dark Forest", Category = BiomeCategory.Forest, ColorArgb = 0xFF40511A, Temperature = 0.7, Downfall = 0.8 };
    public static readonly BiomeDefinition Taiga = new() { Id = "taiga", Name = "Taiga", Category = BiomeCategory.Taiga, ColorArgb = 0xFF0B6659, Temperature = 0.25, Downfall = 0.8 };
    public static readonly BiomeDefinition OldGrowthPineTaiga = new() { Id = "old_growth_pine_taiga", Name = "Old Growth Pine Taiga", Category = BiomeCategory.Taiga, ColorArgb = 0xFF596651, Temperature = 0.3, Downfall = 0.8 };
    public static readonly BiomeDefinition SnowyTaiga = new() { Id = "snowy_taiga", Name = "Snowy Taiga", Category = BiomeCategory.Snowy, ColorArgb = 0xFF31554A, Temperature = -0.5, Downfall = 0.4 };
    public static readonly BiomeDefinition Jungle = new() { Id = "jungle", Name = "Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF537B09, Temperature = 0.95, Downfall = 0.9 };
    public static readonly BiomeDefinition SparseJungle = new() { Id = "sparse_jungle", Name = "Sparse Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF628B17, Temperature = 0.95, Downfall = 0.8 };
    public static readonly BiomeDefinition BambooJungle = new() { Id = "bamboo_jungle", Name = "Bamboo Jungle", Category = BiomeCategory.Jungle, ColorArgb = 0xFF768E14, Temperature = 0.95, Downfall = 0.9 };
    public static readonly BiomeDefinition Savanna = new() { Id = "savanna", Name = "Savanna", Category = BiomeCategory.Savanna, ColorArgb = 0xFFBDB25F, Temperature = 1.1, Downfall = 0.0 };
    public static readonly BiomeDefinition SavannaPlateau = new() { Id = "savanna_plateau", Name = "Savanna Plateau", Category = BiomeCategory.Savanna, ColorArgb = 0xFFA79D64, Temperature = 1.0, Downfall = 0.0 };
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
    public static readonly BiomeDefinition SnowyPlains = new() { Id = "snowy_plains", Name = "Snowy Plains", Category = BiomeCategory.Snowy, ColorArgb = 0xFFFFFFFF, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition IceSpikes = new() { Id = "ice_spikes", Name = "Ice Spikes", Category = BiomeCategory.Snowy, ColorArgb = 0xFFB4DCDC, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition MushroomFields = new() { Id = "mushroom_fields", Name = "Mushroom Fields", Category = BiomeCategory.Plains, ColorArgb = 0xFFFF00FF, Temperature = 0.9, Downfall = 1.0 };
    
    // Oceans
    public static readonly BiomeDefinition WarmOcean = new() { Id = "warm_ocean", Name = "Warm Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF0066FF, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition LukewarmOcean = new() { Id = "lukewarm_ocean", Name = "Lukewarm Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF0055D4, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition Ocean = new() { Id = "ocean", Name = "Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF003EAA, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepOcean = new() { Id = "deep_ocean", Name = "Deep Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF002277, Temperature = 0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition ColdOcean = new() { Id = "cold_ocean", Name = "Cold Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF203070, Temperature = 0.0, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepColdOcean = new() { Id = "deep_cold_ocean", Name = "Deep Cold Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF101B4D, Temperature = 0.0, Downfall = 0.5 };
    public static readonly BiomeDefinition FrozenOcean = new() { Id = "frozen_ocean", Name = "Frozen Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF7090BA, Temperature = -0.5, Downfall = 0.5 };
    public static readonly BiomeDefinition DeepFrozenOcean = new() { Id = "deep_frozen_ocean", Name = "Deep Frozen Ocean", Category = BiomeCategory.Ocean, ColorArgb = 0xFF405580, Temperature = -0.5, Downfall = 0.5 };

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

    // Noise Generator Cache by Seed
    private static readonly ConcurrentDictionary<long, NoiseOctaveSuite> NoiseCache = new();

    private sealed class NoiseOctaveSuite
    {
        public SeedNoise Continentalness { get; }
        public SeedNoise Temperature { get; }
        public SeedNoise Humidity { get; }
        public SeedNoise Erosion { get; }
        public SeedNoise Weirdness { get; }

        public NoiseOctaveSuite(long seed)
        {
            Continentalness = new SeedNoise(seed);
            Temperature = new SeedNoise(seed ^ unchecked((long)0x9E3779B97F4A7C15UL));
            Humidity = new SeedNoise(seed ^ unchecked((long)0xBF58476D1CE4E5B9UL));
            Erosion = new SeedNoise(seed ^ unchecked((long)0x94D049BB133111EBUL));
            Weirdness = new SeedNoise(seed ^ 0x2545F4914F6CDD1DL);
        }
    }

    private static NoiseOctaveSuite GetSuite(long seed)
    {
        return NoiseCache.GetOrAdd(seed, s => new NoiseOctaveSuite(s));
    }

    /// <summary>
    /// Samples the biome at the given block coordinates (X, Z) and dimension.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static BiomeDefinition SampleBiome(long seed, int dimensionId, double blockX, double blockZ)
    {
        return dimensionId switch
        {
            1 => SampleNetherBiome(seed, blockX, blockZ),
            2 => SampleEndBiome(seed, blockX, blockZ),
            _ => SampleOverworldBiome(seed, blockX, blockZ)
        };
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static BiomeDefinition SampleOverworldBiome(long seed, double x, double z)
    {
        var suite = GetSuite(seed);

        // Low frequency climate noise (scale ~ 1000 - 2000 blocks)
        double cont = suite.Continentalness.EvaluateFbm(x, z, frequency: 0.0006, octaves: 2);
        double temp = suite.Temperature.EvaluateFbm(x, z, frequency: 0.0008, octaves: 2);
        double hum = suite.Humidity.EvaluateFbm(x, z, frequency: 0.0008, octaves: 2);
        double eros = suite.Erosion.EvaluateFbm(x, z, frequency: 0.001, octaves: 2);
        double weird = suite.Weirdness.EvaluateFbm(x, z, frequency: 0.0012, octaves: 2);

        // 1. Rare Mushroom Island check (Deep ocean + high weirdness)
        if (cont < -0.35 && weird > 0.65)
        {
            return MushroomFields;
        }

        // 2. Oceans
        if (cont < -0.15)
        {
            bool isDeep = cont < -0.32;
            if (temp > 0.3) return isDeep ? DeepOcean : (temp > 0.6 ? WarmOcean : LukewarmOcean);
            if (temp < -0.35) return isDeep ? DeepFrozenOcean : FrozenOcean;
            if (temp < -0.1) return isDeep ? DeepColdOcean : ColdOcean;
            return isDeep ? DeepOcean : Ocean;
        }

        // 3. Mountain Peaks & Highlands (High erosion + peaks)
        if (eros > 0.45)
        {
            if (temp < -0.3) return weird > 0.2 ? FrozenPeaks : JaggedPeaks;
            if (temp > 0.4) return StonyPeaks;
            if (weird > 0.35 && temp >= -0.1 && temp <= 0.4) return CherryGrove;
            if (temp < 0.0) return SnowySlopes;
            return Meadow;
        }

        // 4. Badlands (High temp, dry, specific weirdness)
        if (temp > 0.55 && hum < -0.2)
        {
            if (weird > 0.4) return ErodedBadlands;
            if (hum > -0.35) return WoodedBadlands;
            return Badlands;
        }

        // 5. Desert & Savanna (Hot climates)
        if (temp > 0.35)
        {
            if (hum < -0.25) return Desert;
            if (hum < 0.15) return weird > 0.3 ? SavannaPlateau : Savanna;
            // Hot + wet = Jungle
            if (hum > 0.5) return weird > 0.3 ? BambooJungle : Jungle;
            return SparseJungle;
        }

        // 6. Frozen & Snowy biomes (Cold climates)
        if (temp < -0.35)
        {
            if (weird > 0.55) return IceSpikes;
            if (hum > 0.1) return SnowyTaiga;
            if (eros > 0.2) return Grove;
            return SnowyPlains;
        }

        // 7. Taiga (Cool climates)
        if (temp < -0.1)
        {
            if (weird > 0.35) return OldGrowthPineTaiga;
            if (eros > 0.25) return WindsweptHills;
            return Taiga;
        }

        // 8. Temperate biomes (Forests, Swamps, Plains)
        if (hum > 0.35)
        {
            if (temp > 0.2 && eros < -0.2) return MangroveSwamp;
            if (eros < -0.15) return Swamp;
            if (weird > 0.4) return DarkForest;
            if (weird < -0.3) return FlowerForest;
            return Forest;
        }

        if (hum > 0.0)
        {
            if (weird > 0.25) return BirchForest;
            if (weird < -0.35) return SunflowerPlains;
            return Forest;
        }

        // Default: Plains
        return Plains;
    }

    private static BiomeDefinition SampleNetherBiome(long seed, double x, double z)
    {
        var suite = GetSuite(seed);
        double temp = suite.Temperature.EvaluateFbm(x, z, frequency: 0.002, octaves: 2);
        double hum = suite.Humidity.EvaluateFbm(x, z, frequency: 0.002, octaves: 2);

        if (temp > 0.3)
        {
            return hum > 0.0 ? CrimsonForest : BasaltDeltas;
        }
        if (temp < -0.3)
        {
            return hum > 0.0 ? WarpedForest : SoulSandValley;
        }
        return NetherWastes;
    }

    private static BiomeDefinition SampleEndBiome(long seed, double x, double z)
    {
        double distSq = x * x + z * z;
        // Central End Island (< 1000 blocks radius)
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
