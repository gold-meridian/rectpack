using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GoldMeridian.RectPack;

public struct RectWh(int w, int h) : IEquatable<RectWh>
{
    public readonly int MaxSide => H > W ? H : W;

    public readonly int MinSide => H < W ? H : W;

    public readonly long Area => (long)W * H;

    public readonly int Perimeter => 2 * (W + H);

    public int W = w;
    public int H = h;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RectWh Flipped()
    {
        return new RectWh(H, W);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RectWh ExpandWith(in RectXywhf r)
    {
        return new RectWh(int.Max(W, r.X + r.W), int.Max(H, r.Y + r.H));
    }

    public readonly bool Equals(RectWh other)
    {
        return W == other.W && H == other.H;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is RectWh other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(W, H);
    }

    public readonly override string ToString()
    {
        return $"{W}x{H}";
    }
}

public readonly struct RectXywh(int x, int y, int w, int h)
{
    public int Area => W * H;

    public int Perimeter => 2 * (W + H);

    public readonly int X = x;
    public readonly int Y = y;
    public readonly int W = w;
    public readonly int H = h;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectWh GetWh()
    {
        return new RectWh(W, H);
    }
}

public readonly struct RectXywhf(int x, int y, int w, int h, bool flipped)
{
    public int Area => W * H;

    public int Perimeter => 2 * (W + H);
    
    public readonly int X = x;
    public readonly int Y = y;
    public readonly int W = flipped ? h : w;
    public readonly int H = flipped ? w : h;
    public readonly bool Flipped = flipped;

    public RectXywhf(in RectXywh r) : this(r.X, r.Y, r.W, r.H, flipped: false) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectWh GetWh()
    {
        return new RectWh(W, H);
    }
}
