#include "BedrockMapEngine.h"
#include "cubiomes/generator.h"
#include "cubiomes/finders.h"
#include "cubiomes/biomes.h"
#include "cubiomes/layers.h"

#include <random>
#include <cmath>
#include <cstring>
#include <algorithm>
#include <vector>

// ═══════════════════════════════════════════════════════════════════
// 1. BIOME LOOKUP & COLOR TABLE (100% OFFICIAL MOJANG PARITY)
// ═══════════════════════════════════════════════════════════════════

struct BiomeInfo {
    const char* name;
    const char* category;
    uint32_t color;
};

static inline BiomeInfo getBiomeInfo(int id) {
    switch (id) {
        case ocean: return {"Ocean", "Ocean", 0xFF000070};
        case plains: return {"Plains", "Plains", 0xFF8DB360};
        case desert: return {"Desert", "Desert", 0xFFFA9418};
        case mountains: return {"Windswept Hills", "Mountain", 0xFF606060};
        case forest: return {"Forest", "Forest", 0xFF056621};
        case taiga: return {"Taiga", "Taiga", 0xFF0B6659};
        case swamp: return {"Swamp", "Swamp", 0xFF07F9B2};
        case river: return {"River", "River", 0xFF0000FF};
        case nether_wastes: return {"Nether Wastes", "Nether", 0xFF572526};
        case the_end: return {"The End", "TheEnd", 0xFF8080FF};
        case frozen_ocean: return {"Frozen Ocean", "Ocean", 0xFF7070D6};
        case frozen_river: return {"Frozen River", "River", 0xFFA0A0FF};
        case snowy_tundra: return {"Snowy Plains", "Icy", 0xFFFFFFFF};
        case snowy_mountains: return {"Snowy Mountains", "Mountain", 0xFFA0A0A0};
        case mushroom_fields: return {"Mushroom Fields", "Mushroom", 0xFFFF00FF};
        case mushroom_field_shore: return {"Mushroom Field Shore", "Mushroom", 0xFFA000FF};
        case beach: return {"Beach", "Beach", 0xFFFADE55};
        case desert_hills: return {"Desert Hills", "Desert", 0xFFD25F12};
        case wooded_hills: return {"Wooded Hills", "Forest", 0xFF22551E};
        case taiga_hills: return {"Taiga Hills", "Taiga", 0xFF163933};
        case mountain_edge: return {"Mountain Edge", "Mountain", 0xFF72789A};
        case jungle: return {"Jungle", "Jungle", 0xFF537B09};
        case jungle_hills: return {"Jungle Hills", "Jungle", 0xFF2C4205};
        case jungle_edge: return {"Sparse Jungle", "Jungle", 0xFF628B17};
        case deep_ocean: return {"Deep Ocean", "Ocean", 0xFF000030};
        case stone_shore: return {"Stony Shore", "Beach", 0xFFA2A284};
        case snowy_beach: return {"Snowy Beach", "Beach", 0xFFFAF0C0};
        case birch_forest: return {"Birch Forest", "Forest", 0xFF307444};
        case birch_forest_hills: return {"Birch Forest Hills", "Forest", 0xFF1F5F32};
        case dark_forest: return {"Dark Forest", "Forest", 0xFF40511A};
        case snowy_taiga: return {"Snowy Taiga", "Taiga", 0xFF31554A};
        case snowy_taiga_hills: return {"Snowy Taiga Hills", "Taiga", 0xFF243F36};
        case giant_tree_taiga: return {"Old Growth Pine Taiga", "Taiga", 0xFF596651};
        case giant_tree_taiga_hills: return {"Giant Tree Taiga Hills", "Taiga", 0xFF454F3E};
        case wooded_mountains: return {"Windswept Forest", "Forest", 0xFF507050};
        case savanna: return {"Savanna", "Savanna", 0xFFBDB25F};
        case savanna_plateau: return {"Savanna Plateau", "Savanna", 0xFFA79D64};
        case badlands: return {"Badlands", "Mesa", 0xFFD94515};
        case wooded_badlands_plateau: return {"Wooded Badlands", "Mesa", 0xFFB09765};
        case badlands_plateau: return {"Badlands Plateau", "Mesa", 0xFFCA8C65};
        case small_end_islands: return {"Small End Islands", "TheEnd", 0xFF404080};
        case end_midlands: return {"End Midlands", "TheEnd", 0xFFC8C8FF};
        case end_highlands: return {"End Highlands", "TheEnd", 0xFFB8B8FF};
        case end_barrens: return {"End Barrens", "TheEnd", 0xFF606090};
        case warm_ocean: return {"Warm Ocean", "Ocean", 0xFF0000AC};
        case lukewarm_ocean: return {"Lukewarm Ocean", "Ocean", 0xFF000090};
        case cold_ocean: return {"Cold Ocean", "Ocean", 0xFF202070};
        case deep_warm_ocean: return {"Deep Warm Ocean", "Ocean", 0xFF000050};
        case deep_lukewarm_ocean: return {"Deep Lukewarm Ocean", "Ocean", 0xFF000040};
        case deep_cold_ocean: return {"Deep Cold Ocean", "Ocean", 0xFF202038};
        case deep_frozen_ocean: return {"Deep Frozen Ocean", "Ocean", 0xFF404090};
        case sunflower_plains: return {"Sunflower Plains", "Plains", 0xFFB5DB61};
        case desert_lakes: return {"Desert Lakes", "Desert", 0xFFFFBC40};
        case gravelly_mountains: return {"Windswept Gravelly Hills", "Mountain", 0xFF888888};
        case flower_forest: return {"Flower Forest", "Forest", 0xFF2D8E49};
        case taiga_mountains: return {"Taiga Mountains", "Taiga", 0xFF1E7060};
        case swamp_hills: return {"Swamp Hills", "Swamp", 0xFF2FFDA3};
        case ice_spikes: return {"Ice Spikes", "Icy", 0xFFB0E0E6};
        case modified_jungle: return {"Modified Jungle", "Jungle", 0xFF7CA01A};
        case modified_jungle_edge: return {"Modified Jungle Edge", "Jungle", 0xFF7F9E2A};
        case tall_birch_forest: return {"Old Growth Birch Forest", "Forest", 0xFF589A6C};
        case tall_birch_hills: return {"Tall Birch Hills", "Forest", 0xFF488A5C};
        case dark_forest_hills: return {"Dark Forest Hills", "Forest", 0xFF60712A};
        case snowy_taiga_mountains: return {"Snowy Taiga Mountains", "Taiga", 0xFF41655A};
        case giant_spruce_taiga: return {"Old Growth Spruce Taiga", "Taiga", 0xFF818E79};
        case giant_spruce_taiga_hills: return {"Giant Spruce Taiga Hills", "Taiga", 0xFF6D7A65};
        case modified_gravelly_mountains: return {"Modified Gravelly Mountains", "Mountain", 0xFF688868};
        case shattered_savanna: return {"Windswept Savanna", "Savanna", 0xFFE5DA87};
        case shattered_savanna_plateau: return {"Shattered Savanna Plateau", "Savanna", 0xFFCFC48C};
        case eroded_badlands: return {"Eroded Badlands", "Mesa", 0xFFFF6D3D};
        case modified_wooded_badlands_plateau: return {"Modified Wooded Badlands", "Mesa", 0xFFD8BF8D};
        case modified_badlands_plateau: return {"Modified Badlands Plateau", "Mesa", 0xFFF2B48D};
        case bamboo_jungle: return {"Bamboo Jungle", "Jungle", 0xFF768E14};
        case bamboo_jungle_hills: return {"Bamboo Jungle Hills", "Jungle", 0xFF5A720C};
        case soul_sand_valley: return {"Soul Sand Valley", "Nether", 0xFF5E493E};
        case crimson_forest: return {"Crimson Forest", "Nether", 0xFF991515};
        case warped_forest: return {"Warped Forest", "Nether", 0xFF147B78};
        case basalt_deltas: return {"Basalt Deltas", "Nether", 0xFF403636};
        case dripstone_caves: return {"Dripstone Caves", "Underground", 0xFF806850};
        case lush_caves: return {"Lush Caves", "Underground", 0xFF408030};
        case meadow: return {"Meadow", "Mountain", 0xFF60B060};
        case grove: return {"Grove", "Taiga", 0xFF487060};
        case snowy_slopes: return {"Snowy Slopes", "Mountain", 0xFFA0C0C0};
        case jagged_peaks: return {"Jagged Peaks", "Mountain", 0xFFD0E8E8};
        case frozen_peaks: return {"Frozen Peaks", "Mountain", 0xFFC0D8E8};
        case stony_peaks: return {"Stony Peaks", "Mountain", 0xFF889898};
        case deep_dark: return {"Deep Dark", "Underground", 0xFF031A26};
        case mangrove_swamp: return {"Mangrove Swamp", "Swamp", 0xFF678839};
        case cherry_grove: return {"Cherry Grove", "Mountain", 0xFFFFB5D5};
        case pale_garden: return {"Pale Garden", "Forest", 0xFF7D8C85};
        default: return {"Plains", "Plains", 0xFF8DB360};
    }
}

// ═══════════════════════════════════════════════════════════════════
// 2. GENERATOR CACHE & FAST SAMPLER (ZERO GC, VORONOI EXACT PRECISION)
// ═══════════════════════════════════════════════════════════════════

static thread_local Generator t_generator;
static thread_local bool t_generatorInitialized = false;
static thread_local int64_t t_currentSeed = 0;
static thread_local int32_t t_currentDim = 999;

static inline void ensureGenerator(int64_t seed, int32_t dimId) {
    int cubiomesDim = (dimId == 1) ? DIM_NETHER : ((dimId == 2) ? DIM_END : DIM_OVERWORLD);
    if (!t_generatorInitialized) {
        setupGenerator(&t_generator, MC_1_21, 0);
        applySeed(&t_generator, cubiomesDim, (uint64_t)seed);
        t_generatorInitialized = true;
        t_currentSeed = seed;
        t_currentDim = cubiomesDim;
    } else if (t_currentSeed != seed || t_currentDim != cubiomesDim) {
        applySeed(&t_generator, cubiomesDim, (uint64_t)seed);
        t_currentSeed = seed;
        t_currentDim = cubiomesDim;
    }
}

static inline int sampleBiomeFast(int dim, int blockX, int blockY, int blockZ) {
    if (dim == DIM_OVERWORLD) {
        int x4, y4, z4;
        voronoiAccess3D(t_generator.sha, blockX, blockY, blockZ, &x4, &y4, &z4);
        return sampleBiomeNoise(&t_generator.bn, NULL, x4, y4, z4, NULL, 0);
    } else {
        return getBiomeAt(&t_generator, 1, blockX, blockY, blockZ);
    }
}

// ═══════════════════════════════════════════════════════════════════
// 3. EXPORTED NATIVE API
// ═══════════════════════════════════════════════════════════════════

EXPORT_API void SampleBiomeNative(
    int64_t seed,
    int32_t dimensionId,
    double blockX,
    double blockZ,
    float depth,
    NativeBiomeResult* outResult
) {
    if (!outResult) return;

    ensureGenerator(seed, dimensionId);

    int cubiomesDim = (dimensionId == 1) ? DIM_NETHER : ((dimensionId == 2) ? DIM_END : DIM_OVERWORLD);
    int blockY = (depth > 0.05f) ? (int)(64.0f - depth * 128.0f) : 64;
    int bx = (int)std::floor(blockX);
    int bz = (int)std::floor(blockZ);

    int biomeId = sampleBiomeFast(cubiomesDim, bx, blockY, bz);

    BiomeInfo info = getBiomeInfo(biomeId);
    outResult->biomeId = biomeId;
    outResult->colorArgb = info.color;
    strncpy_s(outResult->name, sizeof(outResult->name), info.name, _TRUNCATE);
    strncpy_s(outResult->category, sizeof(outResult->category), info.category, _TRUNCATE);
}

EXPORT_API void RenderBiomeMapNative(
    int64_t seed,
    int32_t dimensionId,
    double centerX,
    double centerZ,
    double zoom,
    int32_t width,
    int32_t height,
    int32_t step,
    uint32_t* outPixelBuffer
) {
    if (!outPixelBuffer || width <= 0 || height <= 0 || step <= 0) return;

    ensureGenerator(seed, dimensionId);
    int cubiomesDim = (dimensionId == 1) ? DIM_NETHER : ((dimensionId == 2) ? DIM_END : DIM_OVERWORLD);

    double halfW = width / 2.0;
    double halfH = height / 2.0;
    double invZoom = 1.0 / zoom;
    int blockY = 64; // Sea level Y=64 for exact surface river & biome detection

    for (int y = 0; y < height; y += step) {
        double worldZ = centerZ + (y - halfH) * invZoom;
        int blockZ = (int)std::floor(worldZ);

        for (int x = 0; x < width; x += step) {
            double worldX = centerX + (x - halfW) * invZoom;
            int blockX = (int)std::floor(worldX);

            int biomeId = sampleBiomeFast(cubiomesDim, blockX, blockY, blockZ);
            BiomeInfo info = getBiomeInfo(biomeId);
            uint32_t color = info.color;

            int endY = std::min(y + step, height);
            int endX = std::min(x + step, width);
            for (int py = y; py < endY; py++) {
                int rowIdx = py * width;
                for (int px = x; px < endX; px++) {
                    outPixelBuffer[rowIdx + px] = color;
                }
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
// 4. AUTHENTIC STRUCTURE FINDER
// ═══════════════════════════════════════════════════════════════════

struct StructureTypeMapping {
    int appTypeId;
    int cubiomesType;
    int dim;
    const char* name;
    const char* icon;
    uint32_t color;
};

static const StructureTypeMapping StructureMapTable[] = {
    {3, Village, 0, "Village", "village.png", 0xFFF59E0B},
    {4, Ancient_City, 0, "Ancient City", "ancient_city.png", 0xFF06B6D4},
    {7, Mansion, 0, "Woodland Mansion", "mansion.png", 0xFF84CC16},
    {8, Monument, 0, "Ocean Monument", "monument.png", 0xFF38BDF8},
    {9, Outpost, 0, "Pillager Outpost", "outpost.png", 0xFFE11D48},
    {11, Ruined_Portal, 0, "Ruined Portal", "ruined_portal.png", 0xFF9333EA},
    {12, Jungle_Pyramid, 0, "Jungle Temple", "jungle_temple.png", 0xFF10B981},
    {13, Desert_Pyramid, 0, "Desert Temple", "desert_temple.png", 0xFFEAB308},
    {14, Swamp_Hut, 0, "Witch Hut", "witch_hut.png", 0xFF8B5CF6},
    {15, Treasure, 0, "Buried Treasure", "treasure.png", 0xFFF59E0B},
    {16, Shipwreck, 0, "Shipwreck", "shipwreck.png", 0xFF0284C7},
    {17, Igloo, 0, "Igloo", "igloo.png", 0xFFE0E7FF},
    {18, Ocean_Ruin, 0, "Ocean Ruins", "ocean_ruins.png", 0xFF0D9488},
    {27, Trail_Ruins, 0, "Trail Ruins", "trail_ruins.png", 0xFFD97706},
    {28, Trial_Chambers, 0, "Trial Chamber", "trial_chamber.png", 0xFFF97316},
    {29, Fortress, 1, "Nether Fortress", "nether_fortress.png", 0xFFDC2626},
    {30, Bastion, 1, "Bastion", "bastion.png", 0xFFF97316},
    {31, End_City, 2, "End City", "end_city.png", 0xFFA855F7}
};
static const size_t NumMappedStructures = sizeof(StructureMapTable) / sizeof(StructureMapTable[0]);

static inline int floorDiv(int a, int b) {
    return a >= 0 ? a / b : (a - b + 1) / b;
}

EXPORT_API int32_t FindStructuresNative(
    int64_t seed,
    int32_t dimensionId,
    double minBlockX,
    double minBlockZ,
    double maxBlockX,
    double maxBlockZ,
    uint32_t enabledMask,
    NativeStructureResult* outResults,
    int32_t maxResultsCapacity
) {
    if (!outResults || maxResultsCapacity <= 0) return 0;

    ensureGenerator(seed, dimensionId);

    int count = 0;
    int minBx = (int)std::floor(minBlockX);
    int minBz = (int)std::floor(minBlockZ);
    int maxBx = (int)std::ceil(maxBlockX);
    int maxBz = (int)std::ceil(maxBlockZ);

    for (size_t s = 0; s < NumMappedStructures && count < maxResultsCapacity; s++) {
        const auto& entry = StructureMapTable[s];
        if (entry.dim != dimensionId) continue;
        if (!(enabledMask & (1U << entry.appTypeId))) continue;

        StructureConfig sc;
        if (!getStructureConfig(entry.cubiomesType, MC_1_21, &sc)) continue;

        int regSizeBlocks = sc.regionSize * 16;
        int minRx = floorDiv(minBx, regSizeBlocks);
        int maxRx = floorDiv(maxBx, regSizeBlocks);
        int minRz = floorDiv(minBz, regSizeBlocks);
        int maxRz = floorDiv(maxBz, regSizeBlocks);

        for (int rx = minRx; rx <= maxRx && count < maxResultsCapacity; rx++) {
            for (int rz = minRz; rz <= maxRz && count < maxResultsCapacity; rz++) {
                Pos pos;
                if (!getStructurePos(entry.cubiomesType, MC_1_21, (uint64_t)seed, rx, rz, &pos)) continue;

                if (pos.x >= minBx && pos.x <= maxBx && pos.z >= minBz && pos.z <= maxBz) {
                    int biomeId = sampleBiomeFast(entry.dim, pos.x, 64, pos.z);
                    if (isViableFeatureBiome(MC_1_21, entry.cubiomesType, biomeId)) {
                        auto& res = outResults[count++];
                        res.type = entry.appTypeId;
                        res.blockX = pos.x;
                        res.blockZ = pos.z;
                        res.dimensionId = entry.dim;
                        res.colorArgb = entry.color;
                        strncpy_s(res.name, sizeof(res.name), entry.name, _TRUNCATE);
                        strncpy_s(res.iconAsset, sizeof(res.iconAsset), entry.icon, _TRUNCATE);
                        BiomeInfo bInfo = getBiomeInfo(biomeId);
                        strncpy_s(res.biomeName, sizeof(res.biomeName), bInfo.name, _TRUNCATE);
                    }
                }
            }
        }
    }

    return count;
}

// ═══════════════════════════════════════════════════════════════════
// 5. SLIME CHUNK & WORLD SPAWN
// ═══════════════════════════════════════════════════════════════════

EXPORT_API int32_t IsBedrockSlimeChunkNative(int32_t chunkX, int32_t chunkZ) {
    uint32_t seed = (uint32_t)chunkX * 0x1f1f1f1f ^ (uint32_t)chunkZ;
    std::mt19937 mt(seed);
    return (mt() % 10 == 0) ? 1 : 0;
}
