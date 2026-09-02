namespace BedrockInventoryEditor.Core.Map.Structure;

/// <summary>
/// Standard 32-bit Mersenne Twister (MT19937) PRNG implementation.
/// Bedrock Edition uses std::mt19937 for structure placement and slime chunk generation.
/// </summary>
public sealed class Mt19937
{
    private const int N = 624;
    private const int M = 397;
    private const uint MatrixA = 0x9908B0DFU;
    private const uint UpperMask = 0x80000000U;
    private const uint LowerMask = 0x7FFFFFFFU;

    private readonly uint[] _mt = new uint[N];
    private int _mti = N + 1;

    public Mt19937(uint seed)
    {
        Init(seed);
    }

    public void Init(uint seed)
    {
        _mt[0] = seed;
        for (_mti = 1; _mti < N; _mti++)
        {
            _mt[_mti] = 1812433253U * (_mt[_mti - 1] ^ (_mt[_mti - 1] >> 30)) + (uint)_mti;
        }
    }

    public uint NextUInt()
    {
        uint y;
        uint[] mag01 = [0x0U, MatrixA];

        if (_mti >= N)
        {
            int kk;
            for (kk = 0; kk < N - M; kk++)
            {
                y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1U];
            }
            for (; kk < N - 1; kk++)
            {
                y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1U];
            }
            y = (_mt[N - 1] & UpperMask) | (_mt[0] & LowerMask);
            _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1U];
            _mti = 0;
        }

        y = _mt[_mti++];

        // Tempering
        y ^= (y >> 11);
        y ^= (y << 7) & 0x9D2C5680U;
        y ^= (y << 15) & 0xEFC60000U;
        y ^= (y >> 18);

        return y;
    }

    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) return 0;
        return (int)(NextUInt() % (uint)maxExclusive);
    }
}
