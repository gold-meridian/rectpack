using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GoldMeridian.RectPack;

public interface IEmptySpaceAllocator
{
    int Count { get; }

    void Reset();

    bool Add(in RectXywh r);

    void RemoveAt(int i);

    ref readonly RectXywh Get(int i);
}

public sealed class DefaultEmptySpaces : IEmptySpaceAllocator
{
    private readonly List<RectXywh> spaces = new(capacity: 64);

    public int Count => spaces.Count;

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
    public ref readonly RectXywh Get(int i)
    {
        var span = CollectionsMarshal.AsSpan(spaces);
        return ref span[i];
    }
}

public sealed class StaticEmptySpaces(int capacity) : IEmptySpaceAllocator
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
    public ref readonly RectXywh Get(int i)
    {
        return ref spaces[i];
    }
}
