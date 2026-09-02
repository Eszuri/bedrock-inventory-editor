#pragma once
#include <cstdint>

#ifdef _WIN32
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C"
#endif

#pragma pack(push, 1)
struct NativeStructureResult {
    int32_t type;
    int32_t blockX;
    int32_t blockZ;
    int32_t dimensionId;
    uint32_t colorArgb;
    char name[64];
    char iconAsset[64];
    char biomeName[64];
};

struct NativeBiomeResult {
    int32_t biomeId;
    uint32_t colorArgb;
    char name[64];
    char category[32];
};
#pragma pack(pop)

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
);

EXPORT_API void SampleBiomeNative(
    int64_t seed,
    int32_t dimensionId,
    double blockX,
    double blockZ,
    float depth,
    NativeBiomeResult* outResult
);

EXPORT_API int32_t IsBedrockSlimeChunkNative(
    int32_t chunkX,
    int32_t chunkZ
);

EXPORT_API void GetBedrockSpawnPointNative(
    int64_t seed,
    double* outSpawnX,
    double* outSpawnZ
);

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
);
