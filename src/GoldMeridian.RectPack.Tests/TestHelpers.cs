using System;

namespace GoldMeridian.RectPack.Tests;

public static class TestHelpers
{
    public static bool Overlaps(in RectXywh a, in RectXywh b)
    {
        return b.X < a.X + a.W && a.X < b.X + b.W && b.Y < a.Y + a.H && a.Y < b.Y + b.H;
    }

    public static bool Overlaps(in RectXywhf a, in RectXywhf b)
    {
        return b.X < a.X + a.W && a.X < b.X + b.W && b.Y < a.Y + a.H && a.Y < b.Y + b.H;
    }

    public static bool Overlaps(in PackedRect a, in PackedRect b)
    {
        return b.X < a.X + a.W && a.X < b.X + b.W && b.Y < a.Y + a.H && a.Y < b.Y + b.H;
    }

    public static bool AnyOverlaps(ReadOnlySpan<PackedRect> results)
    {
        for (var i = 0; i < results.Length; i++)
        {
            if (!results[i].WasPacked)
            {
                continue;
            }

            for (var j = i + 1; j < results.Length; j++)
            {
                if (!results[j].WasPacked)
                {
                    continue;
                }

                var a = results[i];
                var b = results[j];
                if (Overlaps(a, b))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void AssertNoOverlaps(ReadOnlySpan<PackedRect> results)
    {
        for (var i = 0; i < results.Length; i++)
        {
            if (!results[i].WasPacked)
            {
                continue;
            }

            for (var j = i + 1; j < results.Length; j++)
            {
                if (!results[j].WasPacked)
                {
                    continue;
                }

                var a = results[i];
                var b = results[j];

                Assert.That(Overlaps(a, b), Is.False, $"Overlapped rectangles (indices; i: {i}, j: {j}): ({a.X}, {a.Y}, {a.W}, {a.H}) vs ({b.X}, {b.Y}, {b.W}, {b.H})");
            }
        }
    }

    public static void AssertAllWithinBin(ReadOnlySpan<PackedRect> results, RectWh bin)
    {
        foreach (var r in results)
        {
            if (!r.WasPacked)
            {
                continue;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(r.X, Is.GreaterThanOrEqualTo(0));
                Assert.That(r.Y, Is.GreaterThanOrEqualTo(0));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(r.X + r.W, Is.LessThanOrEqualTo(bin.W));
                Assert.That(r.Y + r.H, Is.LessThanOrEqualTo(bin.H));
            }
        }
    }
    
    public static void AssertSizesMatchInput(ReadOnlySpan<RectWh> input, ReadOnlySpan<PackedRect> results)
    {
        for (var i = 0; i < input.Length; i++)
        {
            if (!results[i].WasPacked)
            {
                continue;
            }

            var r = results[i];
            var matchesNormal = r.W == input[i].W && r.H == input[i].H;
            var matchesFlipped = r.W == input[i].H && r.H == input[i].W;
            
            // Squares are the same, flipped or not, so always ensure there's
            // always at least one match.  Then do the more granular validation.
            Assert.That(matchesNormal || matchesFlipped);
            
            if (r.Flipped)
            {
                Assert.That(matchesFlipped);
            }
            else
            {
                Assert.That(matchesNormal);
            }
        }
    }

    public static RectWh[] RandomRects(int n, int minSide, int maxSide, int seed)
    {
        var random = new Random(seed);
        var result = new RectWh[n];

        for (var i = 0; i < n; i++)
        {
            result[i] = new RectWh(random.Next(minSide, maxSide + 1), random.Next(minSide, maxSide + 1));
        }

        return result;
    }
}
