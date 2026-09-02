using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BedrockInventoryEditor.Core.Map.Biome;
using BedrockInventoryEditor.Core.Map.Structure;

namespace BedrockInventoryEditor.Core.Map;

/// <summary>
/// Direct P/Invoke Interop Bridge to the native C++ BedrockMapEngine.dll.
/// Provides C++ performance and exact std::mt19937 parity with fallback to C# managed engine.
/// </summary>
public static class NativeEngineBridge
{
    private const string DllName = "BedrockMapEngine.dll";

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct NativeStructureResult
    {
        public int Type;
        public int BlockX;
        public int BlockZ;
        public int DimensionId;
        public uint ColorArgb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string IconAsset;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string BiomeName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct NativeBiomeResult
    {
        public int BiomeId;
        public uint ColorArgb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Category;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FindStructuresNative")]
    private static extern int FindStructuresNative(
        long seed,
        int dimensionId,
        double minBlockX,
        double minBlockZ,
        double maxBlockX,
        double maxBlockZ,
        uint enabledMask,
        [Out] NativeStructureResult[] outResults,
        int maxResultsCapacity
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SampleBiomeNative")]
    private static extern void SampleBiomeNative(
        long seed,
        int dimensionId,
        double blockX,
        double blockZ,
        float depth,
        out NativeBiomeResult outResult
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsBedrockSlimeChunkNative")]
    private static extern int IsBedrockSlimeChunkNative(int chunkX, int chunkZ);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "RenderBiomeMapNative")]
    private static extern void RenderBiomeMapNative(
        long seed,
        int dimensionId,
        double centerX,
        double centerZ,
        double zoom,
        int width,
        int height,
        int step,
        [In, Out] uint[] outPixelBuffer
    );

    private static bool _nativeAvailable = true;

    /// <summary>
    /// Checks if the native C++ engine DLL is loaded and functioning.
    /// </summary>
    public static bool IsNativeAvailable
    {
        get
        {
            try
            {
                if (_nativeAvailable)
                {
                    IsBedrockSlimeChunkNative(0, 0);
                    return true;
                }
            }
            catch
            {
                _nativeAvailable = false;
            }
            return false;
        }
    }

    /// <summary>
    /// Calls the native C++ engine to find structures in a bounding box.
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
        if (IsNativeAvailable)
        {
            try
            {
                var buffer = new NativeStructureResult[256];
                int count = FindStructuresNative(seed, dimensionId, minBlockX, minBlockZ, maxBlockX, maxBlockZ, 0xFFFFFFFF, buffer, buffer.Length);

                var list = new List<StructureDefinition>(count);
                for (int i = 0; i < count; i++)
                {
                    var r = buffer[i];
                    var type = (StructureType)r.Type;

                    if (enabledTypes != null && !enabledTypes.Contains(type))
                    {
                        continue;
                    }

                    list.Add(new StructureDefinition
                    {
                        Type = type,
                        Name = r.Name ?? "Structure",
                        IconAsset = r.IconAsset ?? "village.png",
                        X = r.BlockX,
                        Z = r.BlockZ,
                        DimensionId = r.DimensionId,
                        BiomeName = r.BiomeName ?? "",
                        ColorArgb = r.ColorArgb,
                        Description = $"{r.Name} di bioma {r.BiomeName} ({r.BlockX}, {r.BlockZ})"
                    });
                }

                return list;
            }
            catch
            {
                _nativeAvailable = false;
            }
        }

        // Managed fallback
        return StructureFinder.FindStructures(seed, dimensionId, minBlockX, minBlockZ, maxBlockX, maxBlockZ, enabledTypes);
    }

    /// <summary>
    /// Calls the native C++ engine to sample a biome at (X, Z).
    /// </summary>
    public static BiomeDefinition SampleBiome(long seed, int dimensionId, double blockX, double blockZ, float depth = 0f)
    {
        if (IsNativeAvailable)
        {
            try
            {
                SampleBiomeNative(seed, dimensionId, blockX, blockZ, depth, out var r);
                return new BiomeDefinition
                {
                    Id = r.Name?.ToLowerInvariant().Replace(' ', '_') ?? "plains",
                    Name = r.Name ?? "Plains",
                    Category = Enum.TryParse<BiomeCategory>(r.Category, true, out var cat) ? cat : BiomeCategory.Plains,
                    ColorArgb = r.ColorArgb
                };
            }
            catch
            {
                _nativeAvailable = false;
            }
        }

        return BiomeRegistry.SampleBiome(seed, dimensionId, blockX, blockZ, depth);
    }

    /// <summary>
    /// Checks if a chunk is a Bedrock slime chunk using native C++ std::mt19937.
    /// </summary>
    public static bool IsSlimeChunk(int chunkX, int chunkZ)
    {
        if (IsNativeAvailable)
        {
            try
            {
                return IsBedrockSlimeChunkNative(chunkX, chunkZ) == 1;
            }
            catch
            {
                _nativeAvailable = false;
            }
        }

        return ChunkbaseService.IsBedrockSlimeChunk(chunkX, chunkZ);
    }

    /// <summary>
    /// Calls native C++ engine to render full biome map pixel buffer in a single high-speed pass.
    /// </summary>
    public static bool RenderBiomeMap(
        long seed,
        int dimensionId,
        double centerX,
        double centerZ,
        double zoom,
        int width,
        int height,
        int step,
        uint[] pixelBuffer)
    {
        if (IsNativeAvailable && pixelBuffer != null && pixelBuffer.Length >= width * height)
        {
            try
            {
                RenderBiomeMapNative(seed, dimensionId, centerX, centerZ, zoom, width, height, step, pixelBuffer);
                return true;
            }
            catch
            {
                _nativeAvailable = false;
            }
        }
        return false;
    }
}
