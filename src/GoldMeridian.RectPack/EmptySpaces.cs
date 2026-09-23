using System.Runtime.CompilerServices;

namespace GoldMeridian.RectPack;

public enum FlippingOption
{
    Disabled,
    Enabled,
}

public sealed class EmptySpaces<TAllocator>(TAllocator? providerSeed = null)
    where TAllocator : struct, IEmptySpaceAllocator
{
    public FlippingOption FlippingMode { get; set; } = FlippingOption.Enabled;

    public RectWh RectsAabb { get; private set; }

    public ref TAllocator Spaces => ref spaces;

    private TAllocator spaces = providerSeed ?? new TAllocator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(RectWh r)
    {
        RectsAabb = default(RectWh);
        Spaces.Reset();
        Spaces.Add(new RectXywh(0, 0, r.W, r.H));
    }

    [SkipLocalsInit]
    public bool TryInsert(RectWh imageRectangle, out RectXywhf result)
    {
        var flippingEnabled = FlippingMode == FlippingOption.Enabled;
        var flippedWh = new RectWh(imageRectangle.H, imageRectangle.W);

        for (var i = Spaces.Count - 1; i >= 0; i--)
        {
            var candidateSpace = Spaces.Get(i);

            var normal = CreatedSplits.InsertAndSplit(imageRectangle, in candidateSpace);

            CreatedSplits chosen;
            bool flip;
            if (flippingEnabled)
            {
                var flipped = CreatedSplits.InsertAndSplit(flippedWh, in candidateSpace);

                if (flipped.Success && (!normal.Success || flipped.BetterThan(normal)))
                {
                    chosen = flipped;
                    flip = true;
                }
                else if (normal.Success)
                {
                    chosen = normal;
                    flip = false;
                }
                else
                {
                    continue;
                }
            }
            else
            {
                if (!normal.Success)
                {
                    continue;
                }

                chosen = normal;
                flip = false;
            }

            return Accept(i, in candidateSpace, imageRectangle, in chosen, flip, out result);
        }

        result = default(RectXywhf);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Accept(
        int i,
        in RectXywh candidateSpace,
        RectWh imageRectangle,
        in CreatedSplits splits,
        bool flippingNecessary,
        out RectXywhf result
    )
    {
        Spaces.RemoveAt(i);

        for (var s = 0; s < splits.Count; s++)
        {
            if (Spaces.Add(splits[s]))
            {
                continue;
            }

            result = default(RectXywhf);
            return false;
        }

        result = new RectXywhf(candidateSpace.X, candidateSpace.Y, imageRectangle.W, imageRectangle.H, flippingNecessary);
        RectsAabb = RectsAabb.ExpandWith(result);
        return true;
    }
}
