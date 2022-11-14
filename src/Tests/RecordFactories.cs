extern alias Library1;
extern alias Library2;

using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public static class DynamicExtensions
{
    public static dynamic With(this ExpandoObject expando, Action<dynamic> action)
    { 
        action(expando);
        return expando;
    }
}

public record RecordFactories(ITestOutputHelper Output)
{
    [Fact]
    public void CanMapFromAnonymousType()
    {
        var data = new
        {
            Lines = new[]
            {
                new
                {
                    Start = new { X = 50, Y = 0 },
                    End = new { X = 0, Y = 100 },
                },
                new
                {
                    Start = new { X = 50, Y = 0 },
                    End = new { X = 0, Y = 100 },
                }
            }
        };

        // The dynamic works here because the anonymous type from above lives in the 
        // same assembly as our BufferFactory for our local type. This is not the case 
        // if we try to pass our anonymous type to a library that has its own BufferFactory, 
        // which is why we pass our own buffer to the Library1/2 instead.
        Drawing buffer = Drawing.Create(data);

        Assert.Equal(50, buffer.Lines[0].Start.X);
        Assert.Equal(100, buffer.Lines[1].End.Y);

        var b1 = Dynamically.Create<Library1.Library.Drawing>(buffer);

        Assert.Equal(50, b1.Lines[0].Start.X);
        Assert.Equal(100, b1.Lines[1].End.Y);

        var b2 = Dynamically.Create<Library2.Library.Drawing>(b1);
        
        Assert.Equal(50, b2.Lines[0].Start.X);
        Assert.Equal(100, b2.Lines[1].End.Y);
    }

    [Fact]
    public void CanMapFromLibraryType()
    {
        var data = new Library1::Library.Drawing(new[]
        {
            new Library1::Library.Line(
                new Library1::Library.Point(0, 0),
                new Library1::Library.Point(100, 0)),
            new Library1::Library.Line(
                new Library1::Library.Point(0, 0),
                new Library1::Library.Point(0, 100))
        });

        var buffer = Dynamically.Create<Library2.Library.Drawing>(data);

        //Assert.Equal(100, buffer.Lines[0].End.X);
        //Assert.Equal(100, buffer.Lines[1].End.Y);
    }

    [Fact]
    public void CanMapFromDynamicJson()
    {
        var data = new
        {
            Lines = new[]
            {
                new
                {
                    Start = new { X = 50, Y = 0 },
                    End = new { X = 0, Y = 100 },
                },
                new
                {
                    Start = new { X = 50, Y = 0 },
                    End = new { X = 0, Y = 100 },
                }
            }
        };

        var json = JsonConvert.SerializeObject(data);
        dynamic obj = JsonConvert.DeserializeObject(json);

        // The dynamic works here because the anonymous type from above lives in the 
        // same assembly as our BufferFactory for our local type. This is not the case 
        // if we try to pass our anonymous type to a library that has its own BufferFactory, 
        // which is why we pass our own buffer to the Library1/2 instead.
        Drawing buffer = Drawing.Create(obj);

        Assert.Equal(50, buffer.Lines[0].Start.X);
        Assert.Equal(100, buffer.Lines[1].End.Y);

        var b1 = Dynamically.Create<Library1.Library.Drawing>(obj);

        Assert.Equal(50, b1.Lines[0].Start.X);
        Assert.Equal(100, b1.Lines[1].End.Y);

        var b2 = Dynamically.Create<Library2.Library.Drawing>(obj);

        Assert.Equal(50, b2.Lines[0].Start.X);
        Assert.Equal(100, b2.Lines[1].End.Y);
    }

}

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public partial record Drawing(IReadOnlyList<Line> Lines);