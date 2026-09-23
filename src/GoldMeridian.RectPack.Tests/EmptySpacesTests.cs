namespace GoldMeridian.RectPack.Tests;

[TestFixture]
public static class EmptySpacesTests
{
    [Test]
    public static void SingleRect_ExactFit_Succeeds()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces>();
        spaces.Reset(new RectWh(50, 50));

        var ok = spaces.TryInsert(new RectWh(50, 50), out var placed);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ok, Is.True);
            Assert.That(placed.X, Is.EqualTo(0));
            Assert.That(placed.Y, Is.EqualTo(0));
            Assert.That(placed.W, Is.EqualTo(50));
            Assert.That(placed.H, Is.EqualTo(50));
        }
    }

    [Test]
    public static void TooLargeForBin_Fails()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces>();
        spaces.Reset(new RectWh(10, 10));

        var ok = spaces.TryInsert(new RectWh(20, 5), out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public static void SecondInsert_UsesRemainderSpace_NoOverlap()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces>();
        spaces.Reset(new RectWh(20, 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(spaces.TryInsert(new RectWh(10, 10), out var first), Is.True);
            Assert.That(spaces.TryInsert(new RectWh(10, 10), out var second), Is.True);

            Assert.That(TestHelpers.Overlaps(first, second), Is.False);
        }
    }

    [Test]
    public static void Reset_ClearsPreviousState()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces>();

        // Make sure to fill up the entire bin first
        spaces.Reset(new RectWh(10, 10));
        spaces.TryInsert(new RectWh(10, 10), out _);

        Assert.That(spaces.TryInsert(new RectWh(1, 1), out _), Is.False);

        spaces.Reset(new RectWh(10, 10));
        Assert.That(spaces.TryInsert(new RectWh(10, 10), out _), Is.True);
    }

    [Test]
    public static void FlippingEnabled_RotatesToFit()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces> { FlippingMode = FlippingOption.Enabled };
        spaces.Reset(new RectWh(10, 20));

        var ok = spaces.TryInsert(new RectWh(20, 10), out var placed);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ok, Is.True);
            Assert.That(placed.Flipped, Is.True);
            Assert.That(placed.W, Is.EqualTo(10));
            Assert.That(placed.H, Is.EqualTo(20));
        }
    }

    [Test]
    public static void FlippingDisabled_DoesNotRotate_EvenIfItWouldFit()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces> { FlippingMode = FlippingOption.Disabled };
        spaces.Reset(new RectWh(10, 20));

        var ok = spaces.TryInsert(new RectWh(20, 10), out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public static void RectsAabb_GrowsToBoundAllPlacedRects()
    {
        var spaces = new EmptySpaces<DefaultEmptySpaces>();
        spaces.Reset(new RectWh(1000, 1000));

        spaces.TryInsert(new RectWh(30, 10), out _);
        spaces.TryInsert(new RectWh(10, 30), out _);

        var aabb = spaces.RectsAabb;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(aabb.W >= 30 || aabb.H >= 30, Is.True);
            Assert.That(aabb.Area, Is.GreaterThan(0));
            Assert.That(aabb.W, Is.LessThanOrEqualTo(1000));
            Assert.That(aabb.H, Is.LessThanOrEqualTo(1000));
        }
    }

    [Test]
    public static void FixedCapacityProvider_FailsGracefullyWhenSpacesExhausted()
    {
        var spaces = new EmptySpaces<StaticEmptySpaces>(new StaticEmptySpaces(1));
        spaces.Reset(new RectWh(100, 100));

        // Just ensure no exceptions occur when it tries (and fails) to add the
        // split rectangles back to the empty space container.
        Assert.DoesNotThrow(() => spaces.TryInsert(new RectWh(10, 10), out _));
    }
}
