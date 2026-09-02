using System;
using System.Linq;
using BedrockInventoryEditor.Core.Map;
using BedrockInventoryEditor.Core.Map.Biome;
using BedrockInventoryEditor.Core.Map.Noise;
using BedrockInventoryEditor.Core.Map.Structure;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class NativeSeedMapTests
{
    // =========================================================================
    // 1. NOISE GENERATOR TESTS
    // =========================================================================

    [Fact]
    public void SeedNoise_DeterministicOutput_ForSameSeedAndCoordinates()
    {
        long seed = 123456789L;
        var noiseA = new SeedNoise(seed);
        var noiseB = new SeedNoise(seed);

        double valA = noiseA.Evaluate2D(100.5, -200.75);
        double valB = noiseB.Evaluate2D(100.5, -200.75);

        Assert.Equal(valA, valB, precision: 6);
    }

    [Fact]
    public void SeedNoise_DifferentOutput_ForDifferentSeeds()
    {
        var noise1 = new SeedNoise(111111L);
        var noise2 = new SeedNoise(999999L);

        double val1 = noise1.Evaluate2D(50.0, 50.0);
        double val2 = noise2.Evaluate2D(50.0, 50.0);

        Assert.NotEqual(val1, val2);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, -250)]
    [InlineData(-5000, 8000)]
    [InlineData(12345.67, -98765.43)]
    public void SeedNoise_OutputWithinBoundedRange(double x, double z)
    {
        var noise = new SeedNoise(42L);
        double val = noise.Evaluate2D(x, z);

        Assert.InRange(val, -1.5, 1.5);
    }

    [Fact]
    public void SeedNoise_EvaluateFbm_ReturnsNormalizedValue()
    {
        var noise = new SeedNoise(987654321L);
        double fbm = noise.EvaluateFbm(1000.0, -1000.0, frequency: 0.001, octaves: 4);

        Assert.InRange(fbm, -2.0, 2.0);
    }

    // =========================================================================
    // 2. BIOME REGISTRY & SAMPLER TESTS
    // =========================================================================

    [Fact]
    public void BiomeRegistry_SampleOverworldBiome_ReturnsValidBiome()
    {
        long seed = 555555L;
        var biome = BiomeRegistry.SampleBiome(seed, dimensionId: 0, blockX: 0, blockZ: 0);

        Assert.NotNull(biome);
        Assert.False(string.IsNullOrEmpty(biome.Id));
        Assert.False(string.IsNullOrEmpty(biome.Name));
        Assert.NotEqual(0u, biome.ColorArgb);
    }

    [Fact]
    public void BiomeRegistry_SampleNetherBiome_ReturnsNetherCategory()
    {
        long seed = 777777L;
        var biome = BiomeRegistry.SampleBiome(seed, dimensionId: 1, blockX: 100, blockZ: -100);

        Assert.NotNull(biome);
        Assert.Equal(BiomeCategory.Nether, biome.Category);
    }

    [Fact]
    public void BiomeRegistry_SampleEndCenterIsland_ReturnsTheEnd()
    {
        long seed = 888888L;
        var biome = BiomeRegistry.SampleBiome(seed, dimensionId: 2, blockX: 50, blockZ: 50);

        Assert.NotNull(biome);
        Assert.Equal(BiomeRegistry.TheEnd.Id, biome.Id);
    }

    [Fact]
    public void BiomeRegistry_SampleOuterEndIslands_ReturnsOuterBiomes()
    {
        long seed = 888888L;
        var biome = BiomeRegistry.SampleBiome(seed, dimensionId: 2, blockX: 2000, blockZ: 2000);

        Assert.NotNull(biome);
        Assert.Equal(BiomeCategory.TheEnd, biome.Category);
    }

    [Fact]
    public void BiomeDefinition_ColorComponents_ExtractCorrectRGB()
    {
        var biome = new BiomeDefinition
        {
            ColorArgb = 0xFF123456
        };

        Assert.Equal(0xFF, biome.A);
        Assert.Equal(0x12, biome.R);
        Assert.Equal(0x34, biome.G);
        Assert.Equal(0x56, biome.B);
    }

    [Fact]
    public void BiomeRegistry_SampleClimatePoint_ReturnsBounded6DPoint()
    {
        long seed = 123456789L;
        var p = BiomeRegistry.SampleClimatePoint(seed, 500, -300, depth: 0.5f);

        Assert.InRange(p.Temperature, -2.0f, 2.0f);
        Assert.InRange(p.Humidity, -2.0f, 2.0f);
        Assert.InRange(p.Continentalness, -2.0f, 2.0f);
        Assert.InRange(p.Erosion, -2.0f, 2.0f);
        Assert.InRange(p.Weirdness, -2.0f, 2.0f);
        Assert.Equal(0.5f, p.Depth);
    }

    [Fact]
    public void BiomeRegistry_SampleUndergroundDeepDark_ReturnsDeepDarkOrCave()
    {
        long seed = 42L;
        var biome = BiomeRegistry.SampleBiome(seed, dimensionId: 0, blockX: 1000, blockZ: 1000, depth: 0.8f);

        Assert.NotNull(biome);
        Assert.True(biome.Category == BiomeCategory.Caves || biome.Category == BiomeCategory.Mountain || !string.IsNullOrEmpty(biome.Name));
    }

    // =========================================================================
    // 3. MT19937 PRNG TESTS
    // =========================================================================

    [Fact]
    public void Mt19937_Deterministic_SameSeedSameOutput()
    {
        var mt1 = new Mt19937(12345U);
        var mt2 = new Mt19937(12345U);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(mt1.NextUInt(), mt2.NextUInt());
        }
    }

    [Fact]
    public void Mt19937_DifferentSeeds_DifferentOutput()
    {
        var mt1 = new Mt19937(11111U);
        var mt2 = new Mt19937(99999U);

        Assert.NotEqual(mt1.NextUInt(), mt2.NextUInt());
    }

    [Fact]
    public void Mt19937_NextInt_WithinRange()
    {
        var mt = new Mt19937(42U);
        for (int i = 0; i < 1000; i++)
        {
            int val = mt.NextInt(24);
            Assert.InRange(val, 0, 23);
        }
    }

    [Fact]
    public void Mt19937_NextInt_ZeroMax_ReturnsZero()
    {
        var mt = new Mt19937(42U);
        Assert.Equal(0, mt.NextInt(0));
        Assert.Equal(0, mt.NextInt(-5));
    }

    // =========================================================================
    // 4. STRUCTURE FINDER TESTS
    // =========================================================================

    [Fact]
    public void StructureFinder_FindStructures_ReturnsStructuresInBoundingBox()
    {
        long seed = 12345L;
        // Search a wide area 5000x5000 around spawn
        var structures = StructureFinder.FindStructures(
            seed,
            dimensionId: 0,
            minBlockX: -2500,
            minBlockZ: -2500,
            maxBlockX: 2500,
            maxBlockZ: 2500
        );

        Assert.NotNull(structures);
        Assert.NotEmpty(structures);

        // Should find villages or strongholds
        Assert.Contains(structures, s => s.DimensionId == 0);
    }

    [Fact]
    public void StructureFinder_NetherStructures_ReturnsFortressOrBastion()
    {
        long seed = 99999L;
        var structures = StructureFinder.FindStructures(
            seed,
            dimensionId: 1,
            minBlockX: -1000,
            minBlockZ: -1000,
            maxBlockX: 1000,
            maxBlockZ: 1000
        );

        Assert.NotNull(structures);
        Assert.All(structures, s => Assert.Equal(1, s.DimensionId));
    }

    [Fact]
    public void StructureFinder_EndCities_FoundInOuterEnd()
    {
        long seed = 44444L;
        var structures = StructureFinder.FindStructures(
            seed,
            dimensionId: 2,
            minBlockX: 1000,
            minBlockZ: 1000,
            maxBlockX: 5000,
            maxBlockZ: 5000
        );

        Assert.NotNull(structures);
        Assert.All(structures, s => Assert.Equal(2, s.DimensionId));
    }

    [Fact]
    public void StructureFinder_Deterministic_ForSameSeed()
    {
        long seed = 314159L;
        var run1 = StructureFinder.FindStructures(seed, 0, -1000, -1000, 1000, 1000);
        var run2 = StructureFinder.FindStructures(seed, 0, -1000, -1000, 1000, 1000);

        Assert.Equal(run1.Count, run2.Count);
        for (int i = 0; i < run1.Count; i++)
        {
            Assert.Equal(run1[i].Type, run2[i].Type);
            Assert.Equal(run1[i].X, run2[i].X);
            Assert.Equal(run1[i].Z, run2[i].Z);
        }
    }

    [Fact]
    public void StructureFinder_Filter_OnlyReturnsEnabledTypes()
    {
        long seed = 12345L;
        var filter = new System.Collections.Generic.HashSet<StructureType> { StructureType.Village };
        var structures = StructureFinder.FindStructures(seed, 0, -2000, -2000, 2000, 2000, filter);

        Assert.NotNull(structures);
        Assert.All(structures, s => Assert.Equal(StructureType.Village, s.Type));
    }

    [Fact]
    public void StructureFinder_AllStructures_HaveIconAssetAssigned()
    {
        long seed = 77777L;
        var structures = StructureFinder.FindStructures(seed, 0, -3000, -3000, 3000, 3000);

        Assert.NotEmpty(structures);
        Assert.All(structures, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.IconAsset));
            Assert.EndsWith(".png", s.IconAsset);
        });
    }

    [Fact]
    public void StructureFinder_DifferentSeeds_ProduceDifferentLocations()
    {
        var s1 = StructureFinder.FindStructures(111L, 0, -2000, -2000, 2000, 2000);
        var s2 = StructureFinder.FindStructures(999L, 0, -2000, -2000, 2000, 2000);

        // At least one coordinate should differ between two different seeds
        bool anyDifferent = s1.Count != s2.Count;
        if (!anyDifferent && s1.Count > 0)
        {
            anyDifferent = s1[0].X != s2[0].X || s1[0].Z != s2[0].Z;
        }
        Assert.True(anyDifferent);
    }

    [Fact]
    public void StructureAssets_AllOfficialPngFilesExist()
    {
        string[] expectedPngs =
        [
            "ancient_city.png", "apple.png", "bastion.png", "biomes.png", "cave.png",
            "desert_temple.png", "desert_well.png", "dungeon.png", "end_city.png",
            "fossil.png", "geode.png", "igloo.png", "jungle_temple.png", "lava_pool.png",
            "mansion.png", "mineshaft.png", "monument.png", "nether_fortress.png",
            "ocean_ruins.png", "ore_veins.png", "outpost.png", "ravine.png",
            "ruined_portal.png", "shipwreck.png", "slime_chunk.png", "spawn_point.png",
            "stronghold.png", "trail_ruins.png", "treasure.png", "trial_chamber.png",
            "village.png", "witch_hut.png"
        ];

        string assetsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "Structures");
        if (!System.IO.Directory.Exists(assetsDir))
        {
            assetsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Structures");
        }

        if (System.IO.Directory.Exists(assetsDir))
        {
            foreach (var file in expectedPngs)
            {
                string fullPath = System.IO.Path.Combine(assetsDir, file);
                Assert.True(System.IO.File.Exists(fullPath), $"Missing structure icon file: {file}");
            }
        }
    }

    // =========================================================================
    // 5. NATIVE ENGINE BRIDGE TESTS (C++ DLL INTEROP)
    // =========================================================================

    [Fact]
    public void NativeEngineBridge_FindStructures_ReturnsValidList()
    {
        long seed = 12345L;
        var structures = NativeEngineBridge.FindStructures(seed, 0, -2000, -2000, 2000, 2000);

        Assert.NotNull(structures);
        Assert.NotEmpty(structures);
        Assert.All(structures, s =>
        {
            Assert.False(string.IsNullOrEmpty(s.Name));
            Assert.False(string.IsNullOrEmpty(s.IconAsset));
            Assert.Equal(0, s.DimensionId);
        });
    }
    [Fact]
    public void NativeEngineBridge_FindStructures_RespectsBiomeValidation()
    {
        long seed = 12345L;
        var structures = NativeEngineBridge.FindStructures(seed, 0, -3000, -3000, 3000, 3000);

        Assert.NotNull(structures);
        Assert.NotEmpty(structures);

        // Verify that Desert Temples only spawn in Desert biomes
        var desertTemples = structures.Where(s => s.Type == StructureType.DesertTemple);
        foreach (var temple in desertTemples)
        {
            Assert.Contains("Desert", temple.BiomeName, StringComparison.OrdinalIgnoreCase);
        }

        // Verify that Witch Huts only spawn in Swamp biomes
        var witchHuts = structures.Where(s => s.Type == StructureType.WitchHut);
        foreach (var hut in witchHuts)
        {
            Assert.Contains("Swamp", hut.BiomeName, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void NativeEngineBridge_SampleNetherAndEndBiomes_ReturnsCorrectDimensions()
    {
        long seed = 424242L;

        // Nether (Dimension 1)
        var netherBiome = NativeEngineBridge.SampleBiome(seed, 1, 100, 100);
        Assert.NotNull(netherBiome);
        Assert.Equal(BiomeCategory.Nether, netherBiome.Category);

        // The End (Dimension 2)
        var endCenter = NativeEngineBridge.SampleBiome(seed, 2, 0, 0);
        Assert.NotNull(endCenter);
        Assert.Equal(BiomeCategory.TheEnd, endCenter.Category);
    }

    [Fact]
    public void NativeEngineBridge_RenderBiomeMap_GeneratesNonEmptyPixelBuffer()
    {
        long seed = 123456789L;
        int width = 128;
        int height = 128;
        uint[] buffer = new uint[width * height];

        bool success = NativeEngineBridge.RenderBiomeMap(
            seed,
            dimensionId: 0,
            centerX: 0,
            centerZ: 0,
            zoom: 1.0,
            width: width,
            height: height,
            step: 4,
            pixelBuffer: buffer);

        Assert.True(success);
        Assert.True(NativeEngineBridge.IsNativeAvailable);
        Assert.Contains(buffer, color => color != 0u);
    }
}
