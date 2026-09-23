using System.Runtime.CompilerServices;

namespace GoldMeridian.RectPack;

public struct CreatedSplits
{
    public readonly bool Success => Count > -1;

    public readonly RectXywh this[int i] => i == 0 ? Space0 : Space1;
    
    public int Count;
    public RectXywh Space0;
    public RectXywh Space1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool BetterThan(in CreatedSplits other)
    {
        return Count < other.Count;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CreatedSplits Failed()
    {
        return new CreatedSplits { Count = -1 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CreatedSplits None()
    {
        return new CreatedSplits { Count = 0 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CreatedSplits One(in RectXywh a)
    {
        return new CreatedSplits { Count = 1, Space0 = a };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CreatedSplits Two(in RectXywh a, in RectXywh b)
    {
        return new CreatedSplits { Count = 2, Space0 = a, Space1 = b };
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CreatedSplits InsertAndSplit(in RectWh im, in RectXywh sp)
    {
        var freeW = sp.W - im.W;
        var freeH = sp.H - im.H;

        if (freeW < 0 || freeH < 0)
        {
            return Failed();
        }

        if (freeW == 0 && freeH == 0)
        {
            return None();
        }

        if (freeW > 0 && freeH == 0)
        {
            var r = sp;
            {
                r.X += im.W;
                r.W -= im.W;
            }
            return One(r);
        }

        if (freeW == 0 && freeH > 0)
        {
            var r = sp;
            {
                r.Y += im.H;
                r.H -= im.H;
            }
            return One(r);
        }

        if (freeW > freeH)
        {
            var biggerSplit = new RectXywh(sp.X + im.W, sp.Y, freeW, sp.H);
            var lesserSplit = new RectXywh(sp.X, sp.Y + im.H, im.W, freeH);
            return Two(biggerSplit, lesserSplit);
        }

        var bigger = new RectXywh(sp.X, sp.Y + im.H, sp.W, freeH);
        var lesser = new RectXywh(sp.X + im.W, sp.Y, freeW, im.H);
        return Two(bigger, lesser);
    }
}
