using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoldMeridian.RectPack.Tests;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GoldMeridian.RectPack.NaiveTests;

// Test program more or less entirely adapted from RectpackSharp:
// https://github.com/ThomasMiz/RectpackSharp/blob/main/Tests/Program.cs

internal static class Program
{
    public static void Main()
    {
        RunTests();
    }

    private static readonly Stopwatch sw = new();
    private static readonly RectPacker<DefaultEmptySpaces> packer = RectPacker.CreateDefault();

    private static void RunTests()
    {
        for (var i = 0; i < 10; i++)
        {
            var r = new Random(i);
            var rectangles = CreateRectangles(r);
            Console.WriteLine($"Test {i}:");
            Console.WriteLine($"    Packing '{rectangles.Length}' rectangles...");

            var output = new PackedRect[rectangles.Length];

            sw.Restart();
            var bounds = packer.Pack(rectangles, output, maxBinSide: 4096);
            sw.Stop();

            Console.WriteLine($"    Packing took {sw.Elapsed.TotalMilliseconds}ms");

            var success = !TestHelpers.AnyOverlaps(output);
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    Packing passed");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("    Packing failed due to intersections");
            }

            Console.ResetColor();

            var imageName = $"image{i}.png";
            SaveAsImage(output, bounds, imageName);
        }

        Console.WriteLine("Running warmed-up tests...");

        var totalRuns = 0;
        var totalMs = 0.0;
        for (var i = 0; i < 10; i++)
        for (var j = 0; j < 10; j++)
        {
            totalRuns++;
            
            var r = new Random(i);
            var rectangles = CreateRectangles(r);
            var output = new PackedRect[rectangles.Length];
                
            sw.Restart();
            _ = packer.Pack(rectangles, output, maxBinSide: 4096);
            sw.Stop();
            
            totalMs += sw.ElapsedMilliseconds;
        }
        
        Console.WriteLine($"Average time: {totalMs / totalRuns}ms");
        Console.ReadKey();
    }

    private static RectWh[] CreateRectangles(Random r)
    {
        var list = new List<RectWh>();

        for (var i = r.Next(5); i < 12; i++)
        {
            list.Add(new RectWh(128 * r.Next(5, 9), 128 * r.Next(2, 5)));
        }

        for (var i = 0; i < 1024; i++)
        {
            list.Add(new RectWh(64, 64));
            list.Add(new RectWh(32, 64));
            list.Add(new RectWh(64, 32));
        }

        for (var i = 0; i < 196; i++)
        {
            list.Add(new RectWh(4 * r.Next(4, 11), 4 * r.Next(4, 11)));
        }

        return [.. list];
    }

    private static void SaveAsImage(PackedRect[] rectangles, RectWh bounds, string file)
    {
        using var image = new Image<Rgba32>(bounds.W, bounds.H);
        image.Mutate(x => x.BackgroundColor(Color.Black));

        for (var i = 0; i < rectangles.Length; i++)
        {
            var r = rectangles[i];
            var color = FromHue(i / 64f % 1);
            for (var x = 0; x < r.W; x++)
            for (var y = 0; y < r.H; y++)
            {
                image[x + r.X, y + r.Y] = color;
            }
        }

        image.SaveAsPng(file);
    }

    private static Rgba32 FromHue(float hue)
    {
        hue *= 360.0f;

        var h = hue / 60.0f;
        var x = (1.0f - Math.Abs((h % 2.0f) - 1.0f));

        float r, g, b;
        if (h is >= 0.0f and < 1.0f)
        {
            r = 1;
            g = x;
            b = 0.0f;
        }
        else if (h is >= 1.0f and < 2.0f)
        {
            r = x;
            g = 1;
            b = 0.0f;
        }
        else if (h is >= 2.0f and < 3.0f)
        {
            r = 0.0f;
            g = 1;
            b = x;
        }
        else if (h is >= 3.0f and < 4.0f)
        {
            r = 0.0f;
            g = x;
            b = 1;
        }
        else if (h is >= 4.0f and < 5.0f)
        {
            r = x;
            g = 0.0f;
            b = 1;
        }
        else if (h is >= 5.0f and < 6.0f)
        {
            r = 1;
            g = 0.0f;
            b = x;
        }
        else
        {
            r = 0.0f;
            g = 0.0f;
            b = 0.0f;
        }

        return new Rgba32(r, g, b);
    }
}
