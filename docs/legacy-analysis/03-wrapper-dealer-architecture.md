# WrapperDealer Plugin Architecture

This document analyzes the plugin architecture that enables dynamic resource wrapper discovery and instantiation.

## Source Files

- `legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs` (133 lines)
- `legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs` (40 lines)
- Example: `legacy_references/Sims4Tools/s4pi Wrappers/StblResource/StblResource.cs` (430 lines)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         WrapperDealer                                │
│  (Static class - discovers and instantiates wrappers)               │
├─────────────────────────────────────────────────────────────────────┤
│  typeMap: List<KeyValuePair<string, Type>>                          │
│    - "0x220557DA" → StblResource                                    │
│    - "0x034AEECB" → CASPartResource                                 │
│    - "*" → DefaultResource (fallback)                               │
│    - ... (100+ mappings)                                            │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ discovers via reflection
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│ s4pi.Stbl     │   │ s4pi.Catalog  │   │ s4pi.Default  │
│ Resource.dll  │   │ Resource.dll  │   │ Resource.dll  │
├───────────────┤   ├───────────────┤   ├───────────────┤
│StblResource   │   │CCOLResource   │   │DefaultResource│
│Handler        │   │COBJResource   │   │Handler        │
│ extends       │   │Handler        │   │ registers "*" │
│AResourceHandler│   │...            │   └───────────────┘
└───────────────┘   └───────────────┘
```

## WrapperDealer Static Class

### Purpose

WrapperDealer is the central factory for resource wrappers. It:
1. Discovers all wrapper assemblies at startup
2. Builds a mapping from ResourceType strings to wrapper Types
3. Instantiates appropriate wrappers for resources

### Static Initialization (WrapperDealer.cs:87-115)

```csharp
static WrapperDealer()
{
    // Find all DLLs in the same folder as WrapperDealer.dll
    string folder = Path.GetDirectoryName(typeof(WrapperDealer).Assembly.Location);
    typeMap = new List<KeyValuePair<string, Type>>();

    foreach (string path in Directory.GetFiles(folder, "*.dll"))
    {
        try
        {
            Assembly dotNetDll = Assembly.LoadFile(path);
            Type[] types = dotNetDll.GetTypes();

            foreach (Type t in types)
            {
                // Find classes that inherit from AResourceHandler
                if (!t.IsSubclassOf(typeof(AResourceHandler))) continue;

                // Instantiate the handler to get its mappings
                AResourceHandler arh = (AResourceHandler)t
                    .GetConstructor(new Type[] { })
                    .Invoke(new object[] { });

                // Extract all type mappings
                foreach (Type k in arh.Keys)
                {
                    foreach (string s in arh[k])
                        typeMap.Add(new KeyValuePair<string, Type>(s, k));
                }
            }
        }
        catch { }  // Silently ignore non-.NET DLLs
    }
    typeMap.Sort((x, y) => x.Key.CompareTo(y.Key));
}
```

### Creating Resources (WrapperDealer.cs:40-43, 64-67)

```csharp
// Create new empty resource
public static IResource CreateNewResource(int APIversion, string resourceType)
{
    return WrapperForType(resourceType, APIversion, null);
}

// Get resource from package
public static IResource GetResource(int APIversion, IPackage pkg,
    IResourceIndexEntry rie, bool AlwaysDefault)
{
    return WrapperForType(
        AlwaysDefault ? "*" : rie["ResourceType"],
        APIversion,
        (pkg as APackage).GetResource(rie)
    );
}
```

### Wrapper Lookup (WrapperDealer.cs:117-129)

```csharp
static IResource WrapperForType(string type, int APIversion, Stream s)
{
    // Find wrapper for specific type, excluding disabled wrappers
    Type t = typeMap.Find(x => !disabled.Contains(x) && x.Key == type).Value;

    // Fall back to default wrapper ("*")
    if (t == null)
        t = typeMap.Find(x => !disabled.Contains(x) && x.Key == "*").Value;

    if (t == null)
        throw new InvalidOperationException("Could not find a resource handler");

    // Instantiate via reflection: new WrapperType(APIversion, stream)
    return (IResource)t
        .GetConstructor(new Type[] { typeof(int), typeof(Stream), })
        .Invoke(new object[] { APIversion, s });
}
```

## AResourceHandler Pattern

### Base Class (AResourceHandler.cs)

```csharp
public abstract class AResourceHandler : Dictionary<Type, List<string>>, IResourceHandler
{
    public AResourceHandler() { }
}
```

A simple dictionary that maps:
- **Key**: Wrapper class Type (e.g., `typeof(StblResource)`)
- **Value**: List of ResourceType strings (e.g., `["0x220557DA"]`)

### Handler Implementation Example (StblResource.cs:423-429)

```csharp
public class StblResourceHandler : AResourceHandler
{
    public StblResourceHandler()
    {
        // Register StblResource for ResourceType 0x220557DA
        this.Add(typeof(StblResource), new List<string>(new[] { "0x220557DA" }));
    }
}
```

### Default Handler Pattern

```csharp
public class DefaultResourceHandler : AResourceHandler
{
    public DefaultResourceHandler()
    {
        // "*" is the catch-all for unknown types
        this.Add(typeof(DefaultResource), new List<string>(new[] { "*" }));
    }
}
```

## Resource Wrapper Pattern

### Required Constructor

Every wrapper must have this constructor signature:

```csharp
public MyResource(int APIversion, Stream s) : base(APIversion, s)
{
    if (this.stream == null)
    {
        // Create new resource with defaults
        this.stream = this.UnParse();
        this.OnResourceChanged(this, EventArgs.Empty);
    }
    this.stream.Position = 0;
    this.Parse(this.stream);
}
```

### Parse/UnParse Pattern

```csharp
private void Parse(Stream s)
{
    BinaryReader r = new BinaryReader(s);
    // Read header
    uint magic = r.ReadUInt32();
    if (magic != FOURCC("STBL"))
        throw new InvalidDataException("Bad magic");

    // Read fields
    this.version = r.ReadUInt16();
    // ...
}

protected override Stream UnParse()
{
    MemoryStream ms = new MemoryStream();
    BinaryWriter w = new BinaryWriter(ms);

    // Write header
    w.Write((uint)FOURCC("STBL"));
    w.Write(this.version);
    // ...

    ms.Position = 0;
    return ms;
}
```

### Property Pattern with Change Notification

```csharp
[ElementPriority(0)]
public ushort Version
{
    get { return this.version; }
    set
    {
        if (this.version != value)
        {
            this.version = value;
            this.OnResourceChanged(this, EventArgs.Empty);
        }
    }
}
```

## Disabled Wrappers Feature

WrapperDealer supports disabling specific wrappers:

```csharp
// Access disabled list
public static ICollection<KeyValuePair<string, Type>> Disabled {
    get { return disabled; }
}

// Usage
WrapperDealer.Disabled.Add(
    new KeyValuePair<string, Type>("0x220557DA", typeof(StblResource))
);
```

When disabled, requests for that type fall through to DefaultResource.

## Known Resource Types

Based on the wrapper assemblies, key ResourceType codes include:

| Code | Wrapper | Description |
|------|---------|-------------|
| 0x220557DA | StblResource | String Table |
| 0x034AEECB | CASPartResource | CAS Part |
| 0x319E4F1D | CCOLResource | Catalog Collection |
| 0x0C1FE246 | COBJResource | Catalog Object |
| * | DefaultResource | Fallback for unknown types |

## Rewrite Recommendations

### Modernize Discovery

Replace reflection-based discovery with:

**Option 1: Attribute-based registration**
```csharp
[ResourceWrapper("0x220557DA")]
public class StblResource : IResource { }

// At startup, scan for [ResourceWrapper] attributes
```

**Option 2: Dependency Injection**
```csharp
services.AddResourceWrapper<StblResource>("0x220557DA");
services.AddResourceWrapper<DefaultResource>("*");
```

**Option 3: Source Generators**
```csharp
// Generate registration code at compile time
[ResourceHandler]
public partial class StblResource { }
```

### Improve Type Safety

Replace string-based ResourceType with strongly-typed enum:

```csharp
public enum ResourceType : uint
{
    StringTable = 0x220557DA,
    CASPart = 0x034AEECB,
    CatalogObject = 0x0C1FE246,
    // ...
}

public interface IResourceWrapper<TResource> where TResource : IResource
{
    ResourceType[] SupportedTypes { get; }
    TResource Create(Stream? s);
}
```

### Eliminate Reflection for Instantiation

Cache constructor delegates:

```csharp
private static readonly Dictionary<ResourceType, Func<Stream?, IResource>> _factories;

static WrapperDealer()
{
    _factories = new Dictionary<ResourceType, Func<Stream?, IResource>>
    {
        [ResourceType.StringTable] = s => new StblResource(s),
        [ResourceType.CASPart] = s => new CASPartResource(s),
        // ...
    };
}

public static IResource GetResource(ResourceType type, Stream? s)
    => _factories.TryGetValue(type, out var factory)
        ? factory(s)
        : _factories[ResourceType.Default](s);
```

### Add Async Support

```csharp
public interface IResourceWrapper
{
    ValueTask<IResource> CreateAsync(Stream? s, CancellationToken ct = default);
}
```

### Improve Error Handling

```csharp
public static Result<IResource> GetResource(ResourceType type, Stream s)
{
    if (!_factories.TryGetValue(type, out var factory))
        return Result<IResource>.Failure($"Unknown resource type: {type}");

    try
    {
        return Result<IResource>.Success(factory(s));
    }
    catch (InvalidDataException ex)
    {
        return Result<IResource>.Failure($"Invalid resource format: {ex.Message}");
    }
}
```

### Lazy Loading

```csharp
public class LazyResource : IResource
{
    private readonly Lazy<IResource> _inner;

    public LazyResource(ResourceType type, Func<Stream> streamProvider)
    {
        _inner = new Lazy<IResource>(() =>
            WrapperDealer.GetResource(type, streamProvider()));
    }

    public Stream Stream => _inner.Value.Stream;
}
```
