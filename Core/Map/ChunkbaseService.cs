using System;
using System.Globalization;

namespace BedrockInventoryEditor.Core.Map;

/// <summary>
/// Service helper for Chunkbase Seed Map integration, coordinate conversion, and Bedrock map utilities.
/// </summary>
public static class ChunkbaseService
{
    public const string ChunkbaseBaseUrl = "https://www.chunkbase.com/apps/seed-map";

    /// <summary>
    /// Builds a direct Chunkbase Seed Map URL with specified seed, platform, dimension, and coordinates.
    /// Format: https://www.chunkbase.com/apps/seed-map#seed={seed}&amp;platform={platform}&amp;dimension={dimension}&amp;x={x}&amp;z={z}
    /// </summary>
    public static string BuildSeedMapUrl(
        long seed, 
        string platform = "bedrock_1_21", 
        string dimension = "overworld", 
        double x = 0, 
        double z = 0)
    {
        var normalizedPlatform = NormalizePlatform(platform);
        var normalizedDim = NormalizeDimension(dimension);
        int intX = (int)Math.Round(x);
        int intZ = (int)Math.Round(z);

        return $"{ChunkbaseBaseUrl}#seed={seed}&platform={normalizedPlatform}&dimension={normalizedDim}&x={intX}&z={intZ}";
    }

    /// <summary>
    /// Normalizes platform string to match Chunkbase platform identifier.
    /// </summary>
    public static string NormalizePlatform(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform)) return "bedrock_1_21";

        var clean = platform.Trim().ToLowerInvariant();
        if (clean.Contains("java"))
        {
            return "java_1_21";
        }
        if (clean.Contains("1.21") || clean.Contains("1_21") || clean.Contains("latest") || clean.Contains("26"))
        {
            return "bedrock_1_21";
        }
        if (clean.Contains("1.20") || clean.Contains("1_20"))
        {
            return "bedrock_1_20";
        }
        if (clean.Contains("1.19") || clean.Contains("1_19"))
        {
            return "bedrock_1_19";
        }
        if (clean.Contains("1.18") || clean.Contains("1_18"))
        {
            return "bedrock_1_18";
        }
        if (clean.Contains("1.17") || clean.Contains("1_17"))
        {
            return "bedrock_1_17";
        }
        if (clean.Contains("1.16") || clean.Contains("1_16"))
        {
            return "bedrock_1_16";
        }
        if (clean.Contains("java"))
        {
            return "java_1_21";
        }

        return "bedrock_1_21";
    }

    /// <summary>
    /// Normalizes dimension string to Chunkbase dimension identifier: 'overworld', 'nether', or 'end'.
    /// </summary>
    public static string NormalizeDimension(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension)) return "overworld";

        var clean = dimension.Trim().ToLowerInvariant();
        if (clean.Contains("nether") || clean == "1") return "nether";
        if (clean.Contains("end") || clean == "2") return "end";
        return "overworld";
    }

    /// <summary>
    /// Converts dimension integer ID (0: Overworld, 1: Nether, 2: The End) to Chunkbase string.
    /// </summary>
    public static string DimensionIdToString(int dimensionId)
    {
        return dimensionId switch
        {
            1 => "nether",
            2 => "end",
            _ => "overworld"
        };
    }

    /// <summary>
    /// Converts dimension string back to Bedrock dimension integer ID.
    /// </summary>
    public static int StringToDimensionId(string dimension)
    {
        var norm = NormalizeDimension(dimension);
        return norm switch
        {
            "nether" => 1,
            "end" => 2,
            _ => 0
        };
    }

    /// <summary>
    /// Calculates distance between two coordinates in blocks.
    /// </summary>
    public static double CalculateDistance(double x1, double z1, double x2, double z2)
    {
        double dx = x2 - x1;
        double dz = z2 - z1;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// Gets compass cardinal direction string from point 1 to point 2 (e.g., "Utara (N)", "Timur Laut (NE)").
    /// </summary>
    public static string GetCompassDirection(double fromX, double fromZ, double toX, double toZ)
    {
        double dx = toX - fromX;
        double dz = toZ - fromZ;

        if (Math.Abs(dx) < 1e-3 && Math.Abs(dz) < 1e-3)
        {
            return "Tepat di Lokasi";
        }

        // In Minecraft: +X is East, -X is West, +Z is South, -Z is North
        // Angle in degrees from North (0 deg) clockwise
        double angle = Math.Atan2(dx, -dz) * (180.0 / Math.PI);
        if (angle < 0) angle += 360.0;

        return angle switch
        {
            >= 337.5 or < 22.5 => "Utara (N)",
            >= 22.5 and < 67.5 => "Timur Laut (NE)",
            >= 67.5 and < 112.5 => "Timur (E)",
            >= 112.5 and < 157.5 => "Tenggara (SE)",
            >= 157.5 and < 202.5 => "Selatan (S)",
            >= 202.5 and < 247.5 => "Barat Daya (SW)",
            >= 247.5 and < 292.5 => "Barat (W)",
            >= 292.5 and < 337.5 => "Barat Laut (NW)",
            _ => "Utara (N)"
        };
    }

    /// <summary>
    /// Converts Overworld coordinates to Nether coordinates (divided by 8).
    /// </summary>
    public static (double X, double Z) OverworldToNether(double overworldX, double overworldZ)
    {
        return (overworldX / 8.0, overworldZ / 8.0);
    }

    /// <summary>
    /// Converts Nether coordinates to Overworld coordinates (multiplied by 8).
    /// </summary>
    public static (double X, double Z) NetherToOverworld(double netherX, double netherZ)
    {
        return (netherX * 8.0, netherZ * 8.0);
    }

    /// <summary>
    /// Converts block coordinates to chunk coordinates (ChunkX, ChunkZ).
    /// </summary>
    public static (int ChunkX, int ChunkZ, int SubX, int SubZ) BlockToChunkCoords(double x, double z)
    {
        int bx = (int)Math.Floor(x);
        int bz = (int)Math.Floor(z);
        int cx = bx >> 4;
        int cz = bz >> 4;
        int subX = bx & 15;
        int subZ = bz & 15;
        return (cx, cz, subX, subZ);
    }

    /// <summary>
    /// Checks whether a specific chunk in Minecraft Bedrock Edition is a Slime Chunk.
    /// In Bedrock Edition, slime chunks are seed-independent and determined by a 32-bit PRNG hash of chunk coordinates.
    /// </summary>
    public static bool IsBedrockSlimeChunk(int chunkX, int chunkZ)
    {
        uint ux = (uint)chunkX;
        uint uz = (uint)chunkZ;
        uint hash = (ux * 0x1f1f1f1fu) ^ uz;
        return (hash % 10) == 0;
    }
}
