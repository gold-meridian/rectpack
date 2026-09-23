using System;
using System.Linq;

namespace GoldMeridian.RectPack.Tests;

[TestFixture]
public static class PackingCorrectnessTests
{
    [Test]
    public static void EmptyInput_ProducesZeroSizedBin()
    {
        var packer = RectPacker.CreateDefault();
        var bin = packer.Pack([], [], maxBinSide: 100);

        Assert.That(bin.Area, Is.EqualTo(0));
    }

    [Test]
    public static void SingleRect_PacksAtOrigin()
    {
        var packer = RectPacker.CreateDefault();
        var input = new[] { new RectWh(37, 51) };
        var output = new PackedRect[1];

        packer.Pack(input, output, maxBinSide: 1000);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(output[0].WasPacked, Is.True);
            Assert.That(output[0].X, Is.EqualTo(0));
            Assert.That(output[0].Y, Is.EqualTo(0));
        }
    }

    [Test]
    public static void OutputShorterThanInput_Throws()
    {
        var packer = RectPacker.CreateDefault();
        var input = new[] { new RectWh(1, 1), new RectWh(1, 1) };
        var output = new PackedRect[1];

        Assert.Throws<ArgumentOutOfRangeException>(() => packer.Pack(input, output, maxBinSide: 100));
    }

    [Test]
    public static void RectLargerThanMaxBin_ReportsNotPacked()
    {
        var packer = RectPacker.CreateDefault();
        var input = new[] { new RectWh(5, 5), new RectWh(10_000, 10_000) };
        var output = new PackedRect[2];

        packer.Pack(input, output, maxBinSide: 100);

        Assert.That(output[1].WasPacked, Is.False);
    }

    [Test]
    public static void ResultOrder_MatchesInputOrder_NotPackingOrder()
    {
        var packer = RectPacker.CreateDefault();
        // For the sake of the test, we want the buffer to have rectangles that
        // are disruptive in their ordering.  A naive packer may not know to
        // reorder these for packing efficiency.
        var input = new[] { new RectWh(1, 1), new RectWh(90, 90), new RectWh(2, 2) };
        var output = new PackedRect[3];

        packer.Pack(input, output, maxBinSide: 500);

        // TODO: Verify that the middle rectangle has been moved in packing
        //       order?  That's sort of an implementation detail...
        for (var i = 0; i < input.Length; i++)
        {
            Assert.That(output[i].SourceIndex, Is.EqualTo(i));
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    [TestCase(20)]
    [TestCase(100)]
    [TestCase(500)]
    [TestCase(2000)]
    public static void RandomRects_NoOverlap_AllContained_SizesPreserved(int n)
    {
        var input = TestHelpers.RandomRects(n, 4, 128, seed: n * 31 + 1);
        var packer = RectPacker.CreateDefault();
        var output = new PackedRect[n];

        var bin = packer.Pack(input, output, maxBinSide: 8192, discardStep: -4);

        Assert.That(output.All(r => r.WasPacked), Is.True);
        TestHelpers.AssertNoOverlaps(output);
        TestHelpers.AssertAllWithinBin(output, bin);
        TestHelpers.AssertSizesMatchInput(input, output);
    }

    [Test]
    public static void Deterministic_SameInput_SameResult()
    {
        var input = TestHelpers.RandomRects(300, 4, 96, seed: 12345);

        var packerA = RectPacker.CreateDefault();
        var outputA = new PackedRect[input.Length];
        var binA = packerA.Pack(input, outputA, maxBinSide: 4096, discardStep: -4);

        var packerB = RectPacker.CreateDefault();
        var outputB = new PackedRect[input.Length];
        var binB = packerB.Pack(input, outputB, maxBinSide: 4096, discardStep: -4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(binB.W, Is.EqualTo(binA.W));
            Assert.That(binB.H, Is.EqualTo(binA.H));
        }

        for (var i = 0; i < input.Length; i++)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outputB[i].X, Is.EqualTo(outputA[i].X));
                Assert.That(outputB[i].Y, Is.EqualTo(outputA[i].Y));
                Assert.That(outputB[i].Flipped, Is.EqualTo(outputA[i].Flipped));
            }
        }
    }

    [Test]
    public static void ReusedPackerInstance_ProducesSameResultAsFreshInstance()
    {
        var input = TestHelpers.RandomRects(150, 4, 96, seed: 999);

        var reused = RectPacker.CreateDefault();
        reused.Pack(TestHelpers.RandomRects(10, 1, 10, seed: 1), new PackedRect[10], maxBinSide: 100);
        reused.Pack(TestHelpers.RandomRects(500, 1, 50, seed: 2), new PackedRect[500], maxBinSide: 4096);

        var reusedOutput = new PackedRect[input.Length];
        var reusedBin = reused.Pack(input, reusedOutput, maxBinSide: 4096, discardStep: -4);

        var fresh = RectPacker.CreateDefault();
        var freshOutput = new PackedRect[input.Length];
        var freshBin = fresh.Pack(input, freshOutput, maxBinSide: 4096, discardStep: -4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reusedBin.W, Is.EqualTo(freshBin.W));
            Assert.That(reusedBin.H, Is.EqualTo(freshBin.H));
        }

        for (var i = 0; i < input.Length; i++)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(reusedOutput[i].X, Is.EqualTo(freshOutput[i].X));
                Assert.That(reusedOutput[i].Y, Is.EqualTo(freshOutput[i].Y));
            }
        }
    }

    [Test]
    public static void PackInOrder_RespectsGivenOrder_AndPacksValidly()
    {
        var input = TestHelpers.RandomRects(200, 4, 64, seed: 77);
        var packer = RectPacker.CreateDefault();
        var output = new PackedRect[input.Length];

        var bin = packer.PackInOrder(input, output, maxBinSide: 4096, discardStep: -4);

        Assert.That(output.All(r => r.WasPacked), Is.True);
        TestHelpers.AssertNoOverlaps(output);
        TestHelpers.AssertAllWithinBin(output, bin);
    }

    [Test]
    public static void FixedCapacityProvider_PacksValidly()
    {
        var input = TestHelpers.RandomRects(300, 4, 64, seed: 55);
        var packer = RectPacker.CreateFixedCapacity(capacity: 4096);
        var output = new PackedRect[input.Length];

        var bin = packer.Pack(input, output, maxBinSide: 4096, discardStep: -4);

        Assert.That(output.All(r => r.WasPacked), Is.True);
        TestHelpers.AssertNoOverlaps(output);
        TestHelpers.AssertAllWithinBin(output, bin);
    }

    [Test]
    public static void SmallerDiscardStep_NeverProducesLargerBinThanLargerDiscardStep()
    {
        var input = TestHelpers.RandomRects(400, 4, 80, seed: 321);

        var thorough = RectPacker.CreateDefault();
        var thoroughOutput = new PackedRect[input.Length];
        var thoroughBin = thorough.Pack(input, thoroughOutput, maxBinSide: 4096, discardStep: 0);

        var fast = RectPacker.CreateDefault();
        var fastOutput = new PackedRect[input.Length];
        var fastBin = fast.Pack(input, fastOutput, maxBinSide: 4096, discardStep: 64);

        // A more thorough (smaller discardStep) search should never do worse
        // than a coarser one.
        Assert.That(thoroughBin.Area, Is.LessThanOrEqualTo(fastBin.Area));
    }

    [Test]
    public static void LargeMaxBinSide_DoesNotOverflowAndStillPacksTightly()
    {
        // evil ass regression test in case of overflows
        var input = TestHelpers.RandomRects(1500, 4, 128, seed: 8500);
        var packer = RectPacker.CreateDefault();
        var output = new PackedRect[input.Length];

        var bin = packer.Pack(input, output, maxBinSide: 65536, discardStep: -4);

        Assert.That(output.All(r => r.WasPacked), Is.True);
        TestHelpers.AssertNoOverlaps(output);
        TestHelpers.AssertAllWithinBin(output, bin);

        var density = input.Sum(x => x.Area) / (float)bin.Area;

        // Expect generally high density.  If this ever fails, then it means the
        // overflow bug regressed and needs to be fixed again.
        Assert.That(density, Is.GreaterThan(0.5f));
    }

    [Test]
    public static void UniformSquares_PackIntoExactGrid()
    {
        // 16 identical 10x10 squares should pack perfectly into a 40x40 bin (or
        // another bin of equivalent area with sides that are a multiple of the
        // base width and height blah blah blah this is basic rectangle stuff).
        var input = new RectWh[16];
        for (var i = 0; i < input.Length; i++)
        {
            input[i] = new RectWh(10, 10);
        }

        var packer = RectPacker.CreateDefault();
        var output = new PackedRect[input.Length];

        var bin = packer.Pack(input, output, maxBinSide: 1000, discardStep: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bin.Area, Is.EqualTo(1600)); // 100% density pls
            Assert.That(output.All(r => r.WasPacked), Is.True);
        }

        TestHelpers.AssertNoOverlaps(output);
    }
}
