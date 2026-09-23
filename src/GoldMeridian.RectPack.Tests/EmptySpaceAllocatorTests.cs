namespace GoldMeridian.RectPack.Tests;

[TestFixture]
public static class EmptySpaceAllocatorTests
{
    [Test]
    public static void DefaultEmptySpaces_SatisfiesBasicContract()
    {
        ExerciseBasicContract(new DefaultEmptySpaces());
    }

    [Test]
    public static void StaticEmptySpaces_SatisfiesBasicContract()
    {
        ExerciseBasicContract(new StaticEmptySpaces(16));
    }

    private static void ExerciseBasicContract<TAllocator>(TAllocator allocator)
        where TAllocator : struct, IEmptySpaceAllocator
    {
        allocator.Reset();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allocator.Count, Is.EqualTo(0));

            Assert.That(allocator.Add(new RectXywh(0, 0, 10, 10)), Is.True);
            Assert.That(allocator.Add(new RectXywh(10, 0, 5, 5)), Is.True);
            Assert.That(allocator.Add(new RectXywh(0, 10, 20, 20)), Is.True);
            Assert.That(allocator.Count, Is.EqualTo(3));

            Assert.That(allocator.Get(0).W, Is.EqualTo(10));
            Assert.That(allocator.Get(1).W, Is.EqualTo(5));
            Assert.That(allocator.Get(2).W, Is.EqualTo(20));

            allocator.RemoveAt(0);
            Assert.That(allocator.Count, Is.EqualTo(2));
            Assert.That(allocator.Get(0).W, Is.EqualTo(20));
            Assert.That(allocator.Get(1).W, Is.EqualTo(5));

            allocator.Reset();
            Assert.That(allocator.Count, Is.EqualTo(0));
        }
    }

    [Test]
    public static void DefaultEmptySpaces_GrowsBeyondInitialCapacity()
    {
        var provider = new DefaultEmptySpaces();
        provider.Reset();

        for (var i = 0; i < 1000; i++)
        {
            Assert.That(provider.Add(new RectXywh(i, 0, 1, 1)), Is.True);
        }

        Assert.That(provider.Count, Is.EqualTo(1000));
        Assert.That(provider.Get(999).X, Is.EqualTo(999));
    }

    [Test]
    public static void StaticEmptySpaces_RejectsAddBeyondCapacity()
    {
        var provider = new StaticEmptySpaces(3);
        provider.Reset();

        Assert.That(provider.Add(new RectXywh(0, 0, 1, 1)), Is.True);
        Assert.That(provider.Add(new RectXywh(0, 0, 1, 1)), Is.True);
        Assert.That(provider.Add(new RectXywh(0, 0, 1, 1)), Is.True);
        Assert.That(provider.Add(new RectXywh(0, 0, 1, 1)), Is.False);

        Assert.That(provider.Count, Is.EqualTo(3));
    }
}
