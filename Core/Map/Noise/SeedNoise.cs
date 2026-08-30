using System;
using System.Runtime.CompilerServices;

namespace BedrockInventoryEditor.Core.Map.Noise;

/// <summary>
/// Fast 2D Simplex and Fractal Brownian Motion (FBM) noise generator parameterized by a 64-bit seed.
/// </summary>
public sealed class SeedNoise
{
    private readonly byte[] _perm = new byte[512];
    private readonly byte[] _permGradIndex = new byte[512];
    private readonly long _seed;

    // Gradients for 2D Simplex noise
    private static readonly double[] GradX = [1, -1, 1, -1, 1, -1, 1, -1, 0, 0, 0, 0];
    private static readonly double[] GradZ = [1, 1, -1, -1, 0, 0, 0, 0, 1, -1, 1, -1];

    private const double F2 = 0.5 * (1.73205080756887729 - 1.0); // 0.5 * (sqrt(3) - 1)
    private const double G2 = (3.0 - 1.73205080756887729) / 6.0; // (3 - sqrt(3)) / 6

    public SeedNoise(long seed)
    {
        _seed = seed;
        InitPermutation(seed);
    }

    private void InitPermutation(long seed)
    {
        var p = new byte[256];
        for (int i = 0; i < 256; i++) p[i] = (byte)i;

        // Deterministic Knuth / 64-bit LCG shuffle
        ulong s = (ulong)seed ^ 0x5DEECE66DUL;
        for (int i = 255; i > 0; i--)
        {
            s = (s * 6364136223846793005UL + 1442695040888963407UL);
            int j = (int)((s >> 32) % (ulong)(i + 1));
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++)
        {
            _perm[i] = p[i & 255];
            _permGradIndex[i] = (byte)(_perm[i] % 12);
        }
    }

    /// <summary>
    /// Evaluates 2D Simplex noise in range [-1.0, 1.0].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Evaluate2D(double x, double z)
    {
        double s = (x + z) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(z + s);

        double t = (i + j) * G2;
        double x0 = x - (i - t);
        double z0 = z - (j - t);

        int i1, j1;
        if (x0 > z0) { i1 = 1; j1 = 0; }
        else { i1 = 0; j1 = 1; }

        double x1 = x0 - i1 + G2;
        double z1 = z0 - j1 + G2;
        double x2 = x0 - 1.0 + 2.0 * G2;
        double z2 = z0 - 1.0 + 2.0 * G2;

        int ii = i & 255;
        int jj = j & 255;

        int gi0 = _permGradIndex[ii + _perm[jj]];
        int gi1 = _permGradIndex[ii + i1 + _perm[jj + j1]];
        int gi2 = _permGradIndex[ii + 1 + _perm[jj + 1]];

        double n0, n1, n2;

        double t0 = 0.5 - x0 * x0 - z0 * z0;
        if (t0 < 0) n0 = 0.0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * (GradX[gi0] * x0 + GradZ[gi0] * z0);
        }

        double t1 = 0.5 - x1 * x1 - z1 * z1;
        if (t1 < 0) n1 = 0.0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * (GradX[gi1] * x1 + GradZ[gi1] * z1);
        }

        double t2 = 0.5 - x2 * x2 - z2 * z2;
        if (t2 < 0) n2 = 0.0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * (GradX[gi2] * x2 + GradZ[gi2] * z2);
        }

        return 70.0 * (n0 + n1 + n2);
    }

    /// <summary>
    /// Evaluates multi-octave Fractal Brownian Motion (FBM) noise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double EvaluateFbm(double x, double z, double frequency = 0.002, int octaves = 2, double persistence = 0.5, double lacunarity = 2.0)
    {
        double total = Evaluate2D(x * frequency, z * frequency);
        if (octaves <= 1) return total;

        double amplitude = persistence;
        double maxAmp = 1.0 + persistence;
        double freq = frequency * lacunarity;

        total += Evaluate2D(x * freq, z * freq) * amplitude;

        for (int i = 2; i < octaves; i++)
        {
            amplitude *= persistence;
            freq *= lacunarity;
            total += Evaluate2D(x * freq, z * freq) * amplitude;
            maxAmp += amplitude;
        }

        return total / maxAmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(double x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }
}
