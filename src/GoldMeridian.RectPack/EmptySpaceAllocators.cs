using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GoldMeridian.RectPack;

public interface IEmptySpaceAllocator
{
    int Count { get; }

    void Reset();

    bool Add(in RectXywh r);

    void RemoveAt(int i);

    RectXywh Get(int i);
}

public readonly struct DefaultEmptySpaces(int startingCapacity) : IEmptySpaceAllocator
{
    private readonly List<RectXywh> spaces = new(startingCapacity);

    public int Count => spaces.Count;

    public DefaultEmptySpaces() : this(64) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        spaces.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in RectXywh r)
    {
        spaces.Add(r);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int i)
    {
        var last = spaces.Count - 1;
        {
            spaces[i] = spaces[last];
        }
        spaces.RemoveAt(last);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectXywh Get(int i)
    {
        return spaces[i];
    }
}

public struct StaticEmptySpaces(int capacity) : IEmptySpaceAllocator
{
    public int Count { get; private set; }

    private readonly RectXywh[] spaces = new RectXywh[capacity];

    public StaticEmptySpaces() : this(capacity: 128) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in RectXywh r)
    {
        if (Count >= spaces.Length)
        {
            return false;
        }

        spaces[Count++] = r;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int i)
    {
        spaces[i] = spaces[Count - 1];
        Count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RectXywh Get(int i)
    {
        return spaces[i];
    }
}
