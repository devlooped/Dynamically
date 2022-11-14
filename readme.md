![Icon](https://raw.github.com/Xamarin/Merq/main/icon/32.png) Merq
================

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Dynamically.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Dynamically)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Dynamically.svg?color=green)](https://www.nuget.org/packages/Devlooped.Dynamically)
[![License](https://img.shields.io/github/license/devlooped/Dynamically.svg?color=blue)](https://github.com/devlooped/Dynamically/blob/main/license.txt)

<!-- #main -->
Create record types from dynamic data with compatible structural shape.

## Usage

Create records for your data types:

```csharp
public record Point(int X, int Y);

public record Line(Point Start, Point End);

public record Drawing(Line[] Lines);
```

This package allows you to create instances of those records from a data 
object with compatible shape, such as:

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
from a different assembly, but that has the same structure. This allows fast in-memory 
object mapping without any serialization or extra allocations.

The factory works too for Newtonsoft.Json deserialized objects, for example:

```csharp
// elsewhere, you got an in-memory Json.NET object model, perhaps with:
dynamic data = JsonConvert.DeserializeObject(json);

// Subsequently, you can turn it into your strongly-typed records:
Drawing drawing = Dynamically.Create<Drawing>(data);
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

## Limitations

This package is not meant to be a full-fledged object mapper. For that, you can 
use [AutoMapper](https://automapper.org/), for example, which is much more flexible 
and has excelent [performance characteristics](https://github.com/kzu/MappingBenchmark).
This package does provide very fast in-memory object mapping that is far faster and 
cheaper than going through any sort of serialization. 

As mentioned, the provided factories do not provide backwards-compatibility: if 
you add a property or constructor argument to the record, the factory will fail 
for payloads without it.

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