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
    public void Reset(in RectWh r)
    {
        RectsAabb = default(RectWh);
        Spaces.Reset();
        Spaces.Add(new RectXywh(0, 0, r.W, r.H));
    }

    public bool TryInsert(in RectWh imageRectangle, out RectXywhf result)
    {
        for (var i = Spaces.Count - 1; i >= 0; i--)
        {
            // !!! Make sure this remains a value copy rather than a reference!
            //     Accept() may mutate Spaces which will cause issues with
            //     candidateSpace as it'll propagate to the references to that
            //     come after the Spaces mutation.  This took me 30 minutes to
            //     find.
            var candidateSpace = Spaces.Get(i);

            if (FlippingMode == FlippingOption.Enabled)
            {
                var normal = CreatedSplits.InsertAndSplit(in imageRectangle, in candidateSpace);
                var flippedWh = new RectWh(imageRectangle.H, imageRectangle.W);
                var flipped = CreatedSplits.InsertAndSplit(in flippedWh, in candidateSpace);

                if (normal.Success && flipped.Success)
                {
                    if (flipped.BetterThan(normal))
                    {
                        return Accept(i, in candidateSpace, in imageRectangle, in flipped, flippingNecessary: true, out result);
                    }
                    
                    return Accept(i, in candidateSpace, in imageRectangle, in normal, flippingNecessary: false, out result);
                }

                if (normal.Success)
                {
                    return Accept(i, in candidateSpace, in imageRectangle, in normal, flippingNecessary: false, out result);
                }

                if (flipped.Success)
                {
                    return Accept(i, in candidateSpace, in imageRectangle, in flipped, flippingNecessary: true, out result);
                }
            }
            else
            {
                var normal = CreatedSplits.InsertAndSplit(in imageRectangle, in candidateSpace);

                if (normal.Success)
                {
                    return Accept(i, in candidateSpace, in imageRectangle, in normal, flippingNecessary: false, out result);
                }
            }
        }
        
        result = default(RectXywhf);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Accept(
        int i,
        in RectXywh candidateSpace,
        in RectWh imageRectangle,
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