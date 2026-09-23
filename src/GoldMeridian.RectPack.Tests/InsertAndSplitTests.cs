namespace GoldMeridian.RectPack.Tests;

[TestFixture]
public static class InsertAndSplitTests
{
    [Test]
    public static void TooWide_Fails()
    {
        var result = CreatedSplits.InsertAndSplit(new RectWh(20, 5), new RectXywh(0, 0, 10, 10));
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public static void TooTall_Fails()
    {
        var result = CreatedSplits.InsertAndSplit(new RectWh(5, 20), new RectXywh(0, 0, 10, 10));
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public static void ExactFit_ProducesNoSplits()
    {
        var result = CreatedSplits.InsertAndSplit(new RectWh(10, 10), new RectXywh(0, 0, 10, 10));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(0));
        }
    }

    [Test]
    public static void ExactWidthMatch_ProducesOneVerticalSplit()
    {
        var result = CreatedSplits.InsertAndSplit(new RectWh(10, 4), new RectXywh(0, 0, 10, 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        var split = result[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(split.X, Is.EqualTo(0));
            Assert.That(split.Y, Is.EqualTo(4));
            Assert.That(split.W, Is.EqualTo(10));
            Assert.That(split.H, Is.EqualTo(6));
        }
    }

    [Test]
    public static void ExactHeightMatch_ProducesOneHorizontalSplit()
    {
        var result = CreatedSplits.InsertAndSplit(new RectWh(4, 10), new RectXywh(0, 0, 10, 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        var split = result[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(split.X, Is.EqualTo(4));
            Assert.That(split.Y, Is.EqualTo(0));
            Assert.That(split.W, Is.EqualTo(6));
            Assert.That(split.H, Is.EqualTo(10));
        }
    }

    [Test]
    public static void StrictlySmaller_ProducesTwoSplits_TotalAreaConserved()
    {
        var space = new RectXywh(0, 0, 20, 10);
        var image = new RectWh(6, 4);

        var result = CreatedSplits.InsertAndSplit(image, space);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(2));
        }

        var splitArea = result[0].Area + (long)result[1].Area;
        Assert.That(splitArea, Is.EqualTo(space.Area - image.Area));
    }

    [Test]
    public static void StrictlySmaller_SplitsDoNotOverlapEachOtherOrTheImage()
    {
        var space = new RectXywh(100, 200, 37, 81); // arbitrary offset + odd sizes
        var image = new RectWh(11, 23);

        var result = CreatedSplits.InsertAndSplit(image, space);
        Assert.That(result.Count, Is.EqualTo(2));

        var imageRect = new RectXywh(space.X, space.Y, image.W, image.H);
        var s0 = result[0];
        var s1 = result[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(TestHelpers.Overlaps(in imageRect, in s0), Is.False);
            Assert.That(TestHelpers.Overlaps(in imageRect, in s1), Is.False);
            Assert.That(TestHelpers.Overlaps(in s0, in s1), Is.False);
        }
    }

    [Test]
    public static void WiderRemainder_SplitsAlongVerticalAxis()
    {
        var space = new RectXywh(0, 0, 20, 10);
        var image = new RectWh(6, 6);

        var result = CreatedSplits.InsertAndSplit(image, space);

        var bigger = result[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(bigger.X, Is.EqualTo(6));
            Assert.That(bigger.Y, Is.EqualTo(0));
            Assert.That(bigger.W, Is.EqualTo(14));
            Assert.That(bigger.H, Is.EqualTo(10));
        }
    }

    [Test]
    public static void TallerRemainder_SplitsAlongHorizontalAxis()
    {
        var space = new RectXywh(0, 0, 10, 20);
        var image = new RectWh(6, 6);

        var result = CreatedSplits.InsertAndSplit(image, space);

        var bigger = result[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(bigger.X, Is.EqualTo(0));
            Assert.That(bigger.Y, Is.EqualTo(6));
            Assert.That(bigger.W, Is.EqualTo(10));
            Assert.That(bigger.H, Is.EqualTo(14));
        }
    }

    [Test]
    public static void CreatedSplits_BetterThan_PrefersFewerSplits()
    {
        var none = CreatedSplits.None();
        var one = CreatedSplits.One(new RectXywh(0, 0, 1, 1));
        var two = CreatedSplits.Two(new RectXywh(0, 0, 1, 1), new RectXywh(1, 1, 1, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(none.BetterThan(one), Is.True);
            Assert.That(none.BetterThan(two), Is.True);
            Assert.That(one.BetterThan(two), Is.True);
            Assert.That(two.BetterThan(one), Is.False);
        }
    }
}
