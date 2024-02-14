![Icon](https://github.com/devlooped/Dynamically/raw/main/assets/img/32.png) Devlooped.Dynamically
================

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Dynamically.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Dynamically)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Dynamically.svg?color=green)](https://www.nuget.org/packages/Devlooped.Dynamically)
[![License](https://img.shields.io/github/license/devlooped/Dynamically.svg?color=blue)](https://github.com/devlooped/Dynamically/blob/main/license.txt)

<!-- #main -->
Instantiate record types from dynamic data with compatible structural shapes, 
in-memory with no reflection or serialization, via compile-time source generators.

## Usage

Create records for your data types:

```csharp
public record Point(int X, int Y);

public record Line(Point Start, Point End);

public record Drawing(Line[] Lines);
```

This project will generate a `Dynamically` class with a factory method to create instances 
of those records from a data object with a compatible shape, such as:

```csharp
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

Drawing drawing = Dynamically.Create<Drawing>(data);
```

In adition to a dynamic object (or an [ExpandoObject](https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=net-7.0), 
for example), you can also pass in objects from other strongly typed values that come 
from a different assembly as long as it has the same structure. This allows fast 
in-memory object mapping without any serialization or extra allocations.

The factory works too for Newtonsoft.Json deserialized objects, for example:

```csharp
// elsewhere, you got an in-memory Json.NET object model, perhaps with:
dynamic data = JsonConvert.DeserializeObject(json);

// Subsequently, you can turn it into your strongly-typed records:
Drawing drawing = Dynamically.Create<Drawing>(data);
```

You can also optionally customize the mapping for specific records by providing accessible 
static `Create` or `CreateMany` factory methods in your records, so you can 
selectively customize the mapping by providing them as needed in specific cases. 
For example:

```csharp
partial record Drawing
{
    // Customize creation of a single Drawing from a dynamic value
    public static Drawing Create(dynamic value);
    // Customize creation of a list of Drawings from a dynamic value
    public static List<Drawing> CreateMany(dynamic value);
}
```


## How It Works

This package analyzes (at compile-time) the shape of your records and creates a 
factory that create instances from a dynamic object. For this, it just accesses 
the properties of the dynamic object and passes them to the record constructor 
(or its properties). This means that the data must have (at least) the expected 
values for the conversion to succeed. 

The static `Dynamically.Create` generic method is also generated at compile time 
and contains a switch statement that dispatches to the correct factory based on 
the generic argument specified. 

In addition, if the records are partial, you also get static `Create` and 
`CreateMany` static methods on the record type itself, for added convenience, 
such as:

```csharp
partial record Drawing
{
    public static Drawing Create(dynamic value);
    public static List<Drawing> CreateMany(dynamic value);
}
```

> NOTE: these will only be provided if your record doesn't already have them.

### Example

For the above example with `Drawing/Line/Point` records, you'd get a generated 
`Dynamically` type like:

```csharp
static partial class Dynamically
{
    public static partial T Create<T>(dynamic data)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(Drawing) => (T)Drawing.Create(data),
            _ => throw new NotSupportedException(),
        };
    }
}
```

If the `Drawing` record was partial, the `Create` method would look like:

```csharp
    partial record Drawing
    {
        public static Drawing Create(dynamic value)
            => DrawingFactory.Create(value);
    }
```

With the `DrawingFactory` class being generated as:

```csharp
static partial class DrawingFactory
{
    public static Drawing Create(dynamic value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        try
        {
            return new Drawing(Line.CreateMany(value.Lines));
        }
        catch (RuntimeBinderException e)
        {
            var valueAsm = ((object)value).GetType().Assembly.GetName();
            var thisAsm = typeof(DrawingFactory).Assembly.GetName();
            throw new ArgumentException(
                $"Incompatible {nameof(Drawing)} value. Cannot convert value from '{valueAsm.Name}, Version={valueAsm.Version}' to '{thisAsm.Name}, Version={thisAsm.Version}'.",
                nameof(value), e);
        }
    }

    public static List<Drawing> CreateMany(dynamic value)
    {
        var result = new List<Drawing>();
        foreach (var item in value)
        {
            result.Add(Create(item));
        }
        return result;
    }
}
```

The `Line` factory method would look very similar, instantiating a many points, 
with perhaps `Point` being the most interesting:

```csharp
static partial class PointFactory
{
    public static Point Create(dynamic value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        try
        {
            return new Point((global::System.Int32)value.X, (global::System.Int32)value.Y);
        }
        catch (RuntimeBinderException e)
        {
            var valueAsm = ((object)value).GetType().Assembly.GetName();
            var thisAsm = typeof(PointFactory).Assembly.GetName();
            throw new ArgumentException(
                $"Incompatible {nameof(Point)} value. Cannot convert value from '{valueAsm.Name}, Version={valueAsm.Version}' to '{thisAsm.Name}, Version={thisAsm.Version}'.",
                nameof(value), e);
        }
    }

    public static List<Point> CreateMany(dynamic value)
    {
        var result = new List<Point>();
        foreach (var item in value)
        {
            result.Add(Create(item));
        }
        return result;
    }
}
```

As you can see, the factory methods are very simple and straightforward, and have 
great run-time performance characteristics since there is absolutely no reflection, 
and the built-in C# dynamic infrastructure takes care of doing the heavy lifting. 
The generated code is basically what you'd write manually to do the casting of the 
entire object hierarchy.

## Limitations

This package is not meant to be a full-fledged object mapper. For that, you can 
use [AutoMapper](https://automapper.org/), for example, which is much more flexible 
and has excelent [performance characteristics](https://github.com/kzu/MappingBenchmark).
This package does provide very fast in-memory object mapping that is far faster and 
cheaper than going through any sort of serialization. 

As mentioned, the provided factories do not provide backwards-compatibility: if 
you add a property or constructor argument to the record, the factory will fail 
for payloads without it.



<!-- #main -->

<!-- #ci -->
## Dogfooding

[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.io/vpre/Devlooped.Dynamically/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.io/index.json)
[![Build](https://github.com/devlooped/Dynamically/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/Dynamically/actions)

We also produce CI packages from branches and pull requests so you can dogfood builds as quickly as they are produced. 

The CI feed is `https://pkg.kzu.io/index.json`. 

The versioning scheme for packages is:

- PR builds: *42.42.42-pr*`[NUMBER]`
- Branch builds: *42.42.42-*`[BRANCH]`.`[COMMITS]`

<!-- #sponsors -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![Christian Findlay](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MelbourneDeveloper.png "Christian Findlay")](https://github.com/MelbourneDeveloper)
[![C. Augusto Proiete](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/augustoproiete.png "C. Augusto Proiete")](https://github.com/augustoproiete)
[![Kirill Osenkov](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KirillOsenkov.png "Kirill Osenkov")](https://github.com/KirillOsenkov)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![SandRock](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sandrock.png "SandRock")](https://github.com/sandrock)
[![Eric C](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eeseewy.png "Eric C")](https://github.com/eeseewy)
[![Andy Gocke](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agocke.png "Andy Gocke")](https://github.com/agocke)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
