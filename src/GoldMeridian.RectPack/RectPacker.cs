using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GoldMeridian.RectPack;

public struct PackedRect
{
    public int X;
    public int Y;
    public int W;
    public int H;
    public bool Flipped;
    public bool WasPacked;
    public int SourceIndex;
}

public sealed class RectPacker<TAllocator>(TAllocator? allocator = null)
    where TAllocator : class, IEmptySpaceAllocator, new()
{
    private readonly EmptySpaces<TAllocator> root = new(allocator ?? new TAllocator());

    private RectSubject[] subjects = [];
    private RectWh[] wh = [];
    private readonly int[][] orders = new int[5][];

    // find_best_packing
    public RectWh Pack(
        ReadOnlySpan<RectWh> input,
        Span<PackedRect> output,
        int maxBinSide,
        int discardStep = -4,
        FlippingOption flippingMode = FlippingOption.Enabled
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length);

        var n = input.Length;
        {
            EnsureCapacity(n);
        }

        for (var i = 0; i < n; i++)
        {
            wh[i] = input[i];
            subjects[i] = new RectSubject { Wh = wh[i], OriginalIndex = i };

            orders[0][i] = i;
        }

        for (var o = 1; o < 5; o++)
        {
            Array.Copy(orders[0], orders[o], n);
        }

        orders[0].AsSpan(0, n).Sort(new AreaDescComparer(wh));
        orders[1].AsSpan(0, n).Sort(new PerimeterDescComparer(wh));
        orders[2].AsSpan(0, n).Sort(new MaxSideDescComparer(wh));
        orders[3].AsSpan(0, n).Sort(new WidthDescComparer(wh));
        orders[4].AsSpan(0, n).Sort(new HeightDescComparer(wh));

        return PackCore(n, maxBinSide, discardStep, flippingMode, output);
    }

    // find_best_packing_dont_sort
    public RectWh PackInOrder(
        ReadOnlySpan<RectWh> input,
        Span<PackedRect> output,
        int maxBinSide,
        int discardStep = -4,
        FlippingOption flippingMode = FlippingOption.Enabled
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length);

        var n = input.Length;
        {
            EnsureCapacity(n);
        }

        for (var i = 0; i < n; i++)
        {
            subjects[i] = new RectSubject { Wh = input[i], OriginalIndex = i };
            orders[0][i] = i;
        }

        var maxBin = new RectWh(maxBinSide, maxBinSide);
        root.FlippingMode = flippingMode;

        var order = orders[0].AsSpan(0, n);
        var success = BestBinFinder<TAllocator>.TryPackForOrderingBest(root, subjects, order, in maxBin, discardStep, out var bestBin, out _);
        if (!success)
        {
            bestBin = maxBin;
        }

        return FinalizePlacement(order, in bestBin, output);
    }

    // best_bin_finder.h find_best_packing_impl
    private RectWh PackCore(
        int n,
        int maxBinSide,
        int discardStep,
        FlippingOption flippingMode,
        Span<PackedRect> output
    )
    {
        var maxBin = new RectWh(maxBinSide, maxBinSide);
        root.FlippingMode = flippingMode;

        var bestBin = maxBin;
        var bestOrder = default(int[]?);
        var bestOrderLength = n;
        var bestTotalInserted = -1;
        var anySucceeded = false;

        for (var o = 0; o < 5; o++)
        {
            var order = orders[o].AsSpan(0, n);
            var success = BestBinFinder<TAllocator>.TryPackForOrderingBest(
                root,
                subjects,
                order,
                in maxBin,
                discardStep,
                out var resultBin,
                out var totalArea
            );

            if (success)
            {
                if (!anySucceeded || resultBin.Area <= bestBin.Area)
                {
                    bestBin = resultBin;
                    bestOrder = orders[o];
                    bestOrderLength = n;
                    anySucceeded = true;
                }
            }
            else if (!anySucceeded)
            {
                if (totalArea > bestTotalInserted)
                {
                    bestTotalInserted = totalArea;
                    bestOrder = orders[o];
                    bestOrderLength = n;
                }
            }
        }

        return FinalizePlacement(bestOrder.AsSpan(0, bestOrderLength), in bestBin, output);
    }

    private RectWh FinalizePlacement(ReadOnlySpan<int> order, in RectWh bin, Span<PackedRect> output)
    {
        root.Reset(in bin);

        foreach (var k in order)
        {
            ref readonly var subject = ref subjects[k];

            if (root.TryInsert(in subject.Wh, out var placed))
            {
                output[subject.OriginalIndex] = new PackedRect
                {
                    X = placed.X,
                    Y = placed.Y,
                    W = placed.W,
                    H = placed.H,
                    Flipped = placed.Flipped,
                    WasPacked = true,
                    SourceIndex = subject.OriginalIndex,
                };
            }
            else
            {
                output[subject.OriginalIndex] = new PackedRect
                {
                    WasPacked = false,
                    SourceIndex = subject.OriginalIndex,
                };
            }
        }

        return root.RectsAabb;
    }

    private void EnsureCapacity(int n)
    {
        if (subjects.Length >= n)
        {
            return;
        }

        var newCap = int.Max(n, subjects.Length * 2);
        subjects = new RectSubject[newCap];
        wh = new RectWh[newCap];

        for (var i = 0; i < 5; i++)
        {
            orders[i] = new int[newCap];
        }
    }
}

public static class RectPacker
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectPacker<DefaultEmptySpaces> CreateDefault()
    {
        return new RectPacker<DefaultEmptySpaces>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectPacker<StaticEmptySpaces> CreateFixedCapacity(int capacity)
    {
        return new RectPacker<StaticEmptySpaces>(new StaticEmptySpaces(capacity));
    }
}

internal readonly struct AreaDescComparer(RectWh[] wh) : IComparer<int>
{
    public int Compare(int a, int b)
    {
        return wh[b].Area.CompareTo(wh[a].Area);
    }
}

internal readonly struct PerimeterDescComparer(RectWh[] wh) : IComparer<int>
{
    public int Compare(int a, int b)
    {
        return wh[b].Perimeter.CompareTo(wh[a].Perimeter);
    }
}

internal readonly struct MaxSideDescComparer(RectWh[] wh) : IComparer<int>
{
    public int Compare(int a, int b)
    {
        return wh[b].MaxSide.CompareTo(wh[a].MaxSide);
    }
}

internal readonly struct WidthDescComparer(RectWh[] wh) : IComparer<int>
{
    public int Compare(int a, int b)
    {
        return wh[b].W.CompareTo(wh[a].W);
    }
}

internal readonly struct HeightDescComparer(RectWh[] wh) : IComparer<int>
{
    public int Compare(int a, int b)
    {
        return wh[b].H.CompareTo(wh[a].H);
    }
}
