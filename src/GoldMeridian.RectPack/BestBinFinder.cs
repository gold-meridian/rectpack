using System;

namespace GoldMeridian.RectPack;

internal enum BinDimension
{
    Both,
    Width,
    Height,
}

internal struct RectSubject
{
    public RectWh Wh;
    public int OriginalIndex;
}

internal static class BestBinFinder<TAllocator>
    where TAllocator : class, IEmptySpaceAllocator, new()
{
    // Un-templated adaption of best_packing_for_ordering_impl
    public static bool TryPackForOrdering(
        EmptySpaces<TAllocator> root,
        RectSubject[] subjects,
        ReadOnlySpan<int> order,
        in RectWh startingBin,
        int discardStep,
        BinDimension triedDimension,
        out RectWh resultBin,
        out int totalAreaOnFailure
    )
    {
        var candidateBin = startingBin;
        var triesBeforeDiscarding = 0;

        if (discardStep <= 0)
        {
            triesBeforeDiscarding = -discardStep;
            discardStep = 1;
        }

        int startingStep;
        switch (triedDimension)
        {
            case BinDimension.Both:
                candidateBin.W /= 2;
                candidateBin.H /= 2;
                startingStep = candidateBin.W / 2;
                break;

            case BinDimension.Width:
                candidateBin.W /= 2;
                startingStep = candidateBin.W / 2;
                break;

            case BinDimension.Height:
                candidateBin.H /= 2;
                startingStep = candidateBin.H / 2;
                break;

            default:
                goto case BinDimension.Height;
        }

        for (var step = startingStep;; step = int.Max(1, step / 2))
        {
            root.Reset(candidateBin);

            var totalInsertedArea = 0;
            var allInserted = true;

            foreach (var k in order)
            {
                ref readonly var subject = ref subjects[k];

                if (root.TryInsert(subject.Wh, out var placed))
                {
                    totalInsertedArea += placed.Area;
                }
                else
                {
                    allInserted = false;
                    break;
                }
            }

            if (allInserted)
            {
                if (step <= discardStep)
                {
                    if (triesBeforeDiscarding > 0)
                    {
                        triesBeforeDiscarding--;
                    }
                    else
                    {
                        resultBin = candidateBin;
                        totalAreaOnFailure = 0;
                        return true;
                    }
                }

                switch (triedDimension)
                {
                    case BinDimension.Both:
                        candidateBin.W -= step;
                        candidateBin.H -= step;
                        break;

                    case BinDimension.Width:
                        candidateBin.W -= step;
                        break;

                    case BinDimension.Height:
                        candidateBin.H -= step;
                        break;

                    default:
                        goto case BinDimension.Height;
                }
            }
            else
            {
                switch (triedDimension)
                {
                    case BinDimension.Both:
                        candidateBin.W += step;
                        candidateBin.H += step;

                        if (candidateBin.Area > startingBin.Area)
                        {
                            resultBin = default(RectWh);
                            totalAreaOnFailure = totalInsertedArea;
                            return false;
                        }

                        break;

                    case BinDimension.Width:
                        candidateBin.W += step;

                        if (candidateBin.W > startingBin.W)
                        {
                            resultBin = default(RectWh);
                            totalAreaOnFailure = totalInsertedArea;
                            return false;
                        }

                        break;

                    default:
                        candidateBin.H += step;

                        if (candidateBin.H > startingBin.H)
                        {
                            resultBin = default(RectWh);
                            totalAreaOnFailure = totalInsertedArea;
                            return false;
                        }

                        break;
                }
            }
        }
    }

    // Un-templated adaption of best_packing_for_ordering
    public static bool TryPackForOrderingBest(
        EmptySpaces<TAllocator> root,
        RectSubject[] subjects,
        ReadOnlySpan<int> order,
        in RectWh startingBin,
        int discardStep,
        out RectWh bestBin,
        out int totalAreaOnFailure
    )
    {
        if (!TryPackForOrdering(root, subjects, order, in startingBin, discardStep, BinDimension.Both, out bestBin, out totalAreaOnFailure))
        {
            return false;
        }

        if (TryPackForOrdering(root, subjects, order, in bestBin, discardStep, BinDimension.Width, out var widthBin, out _))
        {
            bestBin = widthBin;
        }

        if (TryPackForOrdering(root, subjects, order, in bestBin, discardStep, BinDimension.Height, out var heightBin, out _))
        {
            bestBin = heightBin;
        }

        totalAreaOnFailure = 0;
        return true;
    }
}
