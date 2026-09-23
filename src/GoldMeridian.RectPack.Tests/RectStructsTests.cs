using System.Collections.Generic;

namespace GoldMeridian.RectPack.Tests;

[TestFixture]
public static class RectStructsTests
{
    private static IEnumerable<TestCaseData> Rectangles
    {
        get
        {
            yield return new TestCaseData(0, 0);
            yield return new TestCaseData(0, 10);
            yield return new TestCaseData(10, 0);
            yield return new TestCaseData(10, 10);
            yield return new TestCaseData(1, 1);
            yield return new TestCaseData(2, 2);
            yield return new TestCaseData(10, 4);
            yield return new TestCaseData(4, 10);
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectWh_AreaAndPerimeter(int w, int h)
    {
        var r = new RectWh(w, h);

        var area = w * h;
        var perimeter = 2 * (w + h);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Area, Is.EqualTo(area));
            Assert.That(r.Perimeter, Is.EqualTo(perimeter));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectWh_MaxSideMinSide(int w, int h)
    {
        var r = new RectWh(w, h);
        var (max, min) = w > h ? (w, h) : (h, w);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.MaxSide, Is.EqualTo(max));
            Assert.That(r.MinSide, Is.EqualTo(min));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectWh_Flipped(int w, int h)
    {
        var r = new RectWh(w, h);
        var flipped = r.Flipped();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.W, Is.EqualTo(w));
            Assert.That(r.H, Is.EqualTo(h));
            Assert.That(flipped.W, Is.EqualTo(h));
            Assert.That(flipped.H, Is.EqualTo(w));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectWh_ExpandWith(int w, int h)
    {
        var aabb = new RectWh(6, 6);
        var placed = new RectXywhf(5, 5, w, h, flipped: false);

        aabb = aabb.ExpandWith(in placed);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(aabb.W, Is.EqualTo(int.Max(6, 5 + w)));
            Assert.That(aabb.H, Is.EqualTo(int.Max(6, 5 + h)));
        }
    }

    [Test]
    public static void RectWh_ExpandWithNeverShrinks()
    {
        var aabb = new RectWh(100, 100);
        var placed = new RectXywhf(0, 0, 1, 1, flipped: false);

        aabb = aabb.ExpandWith(in placed);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(aabb.W, Is.EqualTo(100));
            Assert.That(aabb.H, Is.EqualTo(100));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectXywhf_GetWh(int w, int h)
    {
        var r = new RectXywhf(0, 0, w, h, flipped: false);
        var wh = r.GetWh();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wh.W, Is.EqualTo(w));
            Assert.That(wh.H, Is.EqualTo(h));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectXywhf_Flipped(int w, int h)
    {
        var r = new RectXywhf(0, 0, w, h, flipped: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.W, Is.EqualTo(h));
            Assert.That(r.H, Is.EqualTo(w));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectXywhf_Unflipped(int w, int h)
    {
        var r = new RectXywhf(0, 0, w, h, flipped: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.W, Is.EqualTo(w));
            Assert.That(r.H, Is.EqualTo(h));
        }
    }

    [TestCaseSource(nameof(Rectangles))]
    public static void RectXywhf_FromRectXywh(int w, int h)
    {
        var src = new RectXywh(1, 2, w, h);
        var r = new RectXywhf(src);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.W, Is.EqualTo(w));
            Assert.That(r.H, Is.EqualTo(h));
            Assert.That(r.Flipped, Is.False);
        }
    }
}
