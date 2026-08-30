using System;
using BedrockInventoryEditor.Core.Map;
using Xunit;

namespace BedrockInventoryEditor.Tests;

public class ChunkbaseServiceTests
{
    [Fact]
    public void BuildSeedMapUrl_GeneratesCorrectParameters()
    {
        long seed = 123456789012345;
        string url = ChunkbaseService.BuildSeedMapUrl(seed, "bedrock_1_21", "overworld", 100, -250);

        Assert.Contains("https://www.chunkbase.com/apps/seed-map#", url);
        Assert.Contains("seed=123456789012345", url);
        Assert.Contains("platform=bedrock_1_21", url);
        Assert.Contains("dimension=overworld", url);
        Assert.Contains("x=100", url);
        Assert.Contains("z=-250", url);
    }

    [Theory]
    [InlineData("1.21", "bedrock_1_21")]
    [InlineData("Bedrock 1.20", "bedrock_1_20")]
    [InlineData("bedrock_1_19", "bedrock_1_19")]
    [InlineData("Java 1.21", "java_1_21")]
    [InlineData("", "bedrock_1_21")]
    public void NormalizePlatform_MapsVersionsCorrectly(string input, string expected)
    {
        var result = ChunkbaseService.NormalizePlatform(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Overworld", "overworld")]
    [InlineData("Nether", "nether")]
    [InlineData("The End", "end")]
    [InlineData("1", "nether")]
    [InlineData("2", "end")]
    [InlineData("0", "overworld")]
    public void NormalizeDimension_MapsDimensionsCorrectly(string input, string expected)
    {
        var result = ChunkbaseService.NormalizeDimension(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void OverworldToNether_DividesBy8()
    {
        var (nx, nz) = ChunkbaseService.OverworldToNether(800, -160);
        Assert.Equal(100.0, nx);
        Assert.Equal(-20.0, nz);
    }

    [Fact]
    public void NetherToOverworld_MultipliesBy8()
    {
        var (ox, oz) = ChunkbaseService.NetherToOverworld(100, -20);
        Assert.Equal(800.0, ox);
        Assert.Equal(-160.0, oz);
    }

    [Fact]
    public void CalculateDistance_ReturnsCorrectEuclideanDistance()
    {
        double dist = ChunkbaseService.CalculateDistance(0, 0, 300, 400);
        Assert.Equal(500.0, dist, 0.001);
    }

    [Fact]
    public void GetCompassDirection_ReturnsExpectedDirection()
    {
        Assert.Equal("Utara (N)", ChunkbaseService.GetCompassDirection(0, 0, 0, -100));
        Assert.Equal("Selatan (S)", ChunkbaseService.GetCompassDirection(0, 0, 0, 100));
        Assert.Equal("Timur (E)", ChunkbaseService.GetCompassDirection(0, 0, 100, 0));
        Assert.Equal("Barat (W)", ChunkbaseService.GetCompassDirection(0, 0, -100, 0));
        Assert.Equal("Timur Laut (NE)", ChunkbaseService.GetCompassDirection(0, 0, 100, -100));
        Assert.Equal("Tepat di Lokasi", ChunkbaseService.GetCompassDirection(10, 20, 10, 20));
    }

    [Fact]
    public void BlockToChunkCoords_CalculatesCorrectly()
    {
        var (cx, cz, subX, subZ) = ChunkbaseService.BlockToChunkCoords(35, -17);
        Assert.Equal(2, cx);
        Assert.Equal(-2, cz);
        Assert.Equal(3, subX);
        Assert.Equal(15, subZ);
    }

    [Theory]
    [InlineData(0, "overworld")]
    [InlineData(1, "nether")]
    [InlineData(2, "end")]
    [InlineData(99, "overworld")]
    public void DimensionIdToString_ConvertsCorrectly(int dimId, string expected)
    {
        Assert.Equal(expected, ChunkbaseService.DimensionIdToString(dimId));
    }

    [Theory]
    [InlineData("overworld", 0)]
    [InlineData("nether", 1)]
    [InlineData("end", 2)]
    [InlineData("unknown", 0)]
    public void StringToDimensionId_ConvertsCorrectly(string dim, int expected)
    {
        Assert.Equal(expected, ChunkbaseService.StringToDimensionId(dim));
    }

    [Fact]
    public void GetCompassDirection_CoversAllOctants()
    {
        Assert.Equal("Utara (N)", ChunkbaseService.GetCompassDirection(0, 0, 0, -500));
        Assert.Equal("Timur Laut (NE)", ChunkbaseService.GetCompassDirection(0, 0, 500, -500));
        Assert.Equal("Timur (E)", ChunkbaseService.GetCompassDirection(0, 0, 500, 0));
        Assert.Equal("Tenggara (SE)", ChunkbaseService.GetCompassDirection(0, 0, 500, 500));
        Assert.Equal("Selatan (S)", ChunkbaseService.GetCompassDirection(0, 0, 0, 500));
        Assert.Equal("Barat Daya (SW)", ChunkbaseService.GetCompassDirection(0, 0, -500, 500));
        Assert.Equal("Barat (W)", ChunkbaseService.GetCompassDirection(0, 0, -500, 0));
        Assert.Equal("Barat Laut (NW)", ChunkbaseService.GetCompassDirection(0, 0, -500, -500));
    }

    [Fact]
    public void BuildSeedMapUrl_HandlesNegativeSeedAndCoords()
    {
        long seed = -987654321098765;
        string url = ChunkbaseService.BuildSeedMapUrl(seed, "bedrock_1_20", "nether", -512.4, -1024.9);

        Assert.Contains("seed=-987654321098765", url);
        Assert.Contains("platform=bedrock_1_20", url);
        Assert.Contains("dimension=nether", url);
        Assert.Contains("x=-512", url);
        Assert.Contains("z=-1025", url);
    }
}
