# Filtering code generation sources using C# attributes

> Copyright (C) 2026 The Qt Company Ltd.
> SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GFDL-1.3-no-invariants-only

It's possible to adjust which types and type members are included and excluded from code generation
by using the attributes described below.

## Ignore types

The simplest way of filtering out a type, regardless of where it is defined &mdash; either in the
current module or in an external assembly &mdash; is by listing that type in a `[Qt.IgnoreType]`
assembly-level attribute.

```csharp
[assembly: Qt.IgnoreType(typeof(List<int>), typeof(List<string>))]

// This class will be included
public class Foo
{
    // These 2 methods will be excluded due to ignored parameter types
    public void Bar(List<int> arg) { }
    public void Bar(List<string> arg) { }

    // This method will be included
    public void Bar(List<double> arg) { }
}
```

### Generic types

For generic types, e.g. `List<T>` where `T` is a type variable, the `typeof()` facility is not
available. To ignore a generic type and all of its specializations, the string form of the generic
type must instead be used.

```csharp
[assembly: Qt.IgnoreType("System.Collections.Generic.List`1")]

// This class will be included
public class Foo
{
    // These 3 methods will be excluded due to ignored parameter types
    public void Bar(List<int> arg) { }
    public void Bar(List<string> arg) { }
    public void Bar(List<double> arg) { }
}
```

## Ignore type definition

It's possible to exclude a type defined in the current project's code by adding a `[Qt.Ignore]`
attribute to the type declaration.

```csharp
// This class will be excluded
[Qt.Ignore]
public class Foo
{ }
```

### Ignore type member definition

Similarly, a member of a type defined in code can also be excluded from code generation using a
`[Qt.Ignore]` attribute.

```csharp
// This class will be included
public class Foo
{
    // This property will be excluded
    [Qt.Ignore]
    public int Bar { get; set; }
}
```

## Ignore type hierarchy

The `[Qt.IgnoreType]` attribute can also be used to filter out entire type hierarchies, i.e. any
type that derives from the root type listed in the attribute. This is achieved by specifying
`Inherited = true` in the attribute instantiation.

```csharp
[assembly: Qt.IgnoreType("System.Collections.Generic.Dictionary`2", Inherited = true)]

// This class will be excluded as it derives from an ignored type hierarchy
public class Foo : Dictionary<int, string>
{
    ...
}
```

### Include sub-type of ignored type hierarchy

It's still possible to include types deriving from an ignored type hierarchy. These must be opted-in
to the code generation by using the `[Qt.Include]` attribute.

```csharp
[assembly: Qt.IgnoreType(typeof(Foo), Inherited = true)]

// This class will be ignored (ignored base type)
public class Foo
{ }

// This class will be ignored (derived type of ignored base type)
public class Oof : Foo
{ }

// This class will be included (opted-in by attribute)
[Qt.Include]
public class Bar : Foo
{ }
```
