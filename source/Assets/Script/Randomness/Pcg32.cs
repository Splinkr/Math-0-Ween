using System;

// PCG32 XSH-RR, adapted from Melissa O'Neill's pcg-c-basic (2014).
// Copyright 2014 Melissa O'Neill <oneill@pcg-random.org>
// Licensed under Apache-2.0; see LICENSE-PCG.txt in this directory.
// https://github.com/imneme/pcg-c-basic/blob/master/pcg_basic.c
internal interface IRandomSource
{
    int Range(int minInclusive, int maxExclusive);
    float Value { get; }
}

internal sealed class Pcg32 : IRandomSource
{
    private const ulong Multiplier = 6364136223846793005UL;
    private ulong state;
    private readonly ulong increment;

    internal Pcg32(ulong initialState, ulong sequence)
    {
        unchecked
        {
            increment = (sequence << 1) | 1UL;
            // Algebraic equivalent of PCG's two initialization steps. No application draws.
            state = (increment + initialState) * Multiplier + increment;
        }
    }

    internal uint NextUInt()
    {
        unchecked
        {
            ulong previous = state;
            state = previous * Multiplier + increment;
            uint shifted = (uint)(((previous >> 18) ^ previous) >> 27);
            int rotation = (int)(previous >> 59);
            return (shifted >> rotation) | (shifted << ((-rotation) & 31));
        }
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive < minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        if (maxExclusive == minInclusive)
            return minInclusive;
        uint bound = (uint)((long)maxExclusive - minInclusive);
        uint threshold = unchecked(0U - bound) % bound;
        uint sample;
        do { sample = NextUInt(); } while (sample < threshold);
        return (int)(minInclusive + (long)(sample % bound));
    }

    internal static float ToUnitValue(uint sample) => (sample >> 8) / 16777215f;
    public float Value => ToUnitValue(NextUInt());
}
