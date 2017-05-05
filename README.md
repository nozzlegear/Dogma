# Dogma

[![NuGet](https://img.shields.io/nuget/v/Dogma.svg?maxAge=3600)](https://www.nuget.org/packages/Dogma/)
[![license](https://img.shields.io/github/license/nozzlegear/dogma.svg?maxAge=3600)](https://raw.githubusercontent.com/nozzlegear/dogma/master/LICENSE)

Dogma is a simple C# => TypeScript module interface declaration generator. Synchronize your dogmas!

## Why?

There are a bunch of other C# to TypeScript tools out there, like [Reinforced.Typings](https://github.com/reinforced/Reinforced.Typings) and [NJsonSchema](https://github.com/rsuter/NJsonSchema), but I couldn't find any that would support:

- a. Transforming the classes and interfaces to *just* TypeScript interfaces.
- b. Add an `export` statement on those TypeScript interfaces.
- c. Export those interfaces **from a declared module**. 
- d. Serialize enums to TypeScript's string literal type, e.g. `"EnumValue1" | "EnumValue2"`.

Almost every tool I've used would either turn my C# pocos to a class with a ton of extra cruft and methods added, wouldn't export the interfaces, or wouldn't declare a module. 

## Usage

Dogma will search whatever Assembly you give it for any classes that are decorated with the `[ToTypeScript]` attribute. It will then iterate through all discovered classes, any subclasses, and any base classes or objects, turning them into TypeScript interfaces.

```cs
[ToTypeScript("my-module")]
public class Foo : Bar
{
    [JsonProperty("hello")]
    public string Hello { get; set; }
}

public class Bar
{
    [JsonProperty("world")]
    public bool World { get; set; }
}
```

Once you've got your pocos ready to convert, pass in the `Assembly` you want to search to `Dogma.Generator.GenerateFiles`, and write the returned modules to `.d.ts` files:

```cs
Assembly assembly = MethodToGetMyAssembly();
var modules = Dogma.Generator.GenerateModules(assembly);

foreach (var module in modules)
{
    System.IO.Files.WriteAllText($"path/to/{module.ModuleName}.generated.d.ts", file.Code);
}
```

The example class above will generate the following TypeScript:

```typescript
declare module "my-module" {
    export interface Foo extends Bar {
        hello: string;
    }

    export interface Bar {
        world: boolean;
    }
}
```

## Getting the Assembly

Dogma needs a reference to the Assembly you're searching, which can be sort-of difficult when you're running DotNet Core. The easiest way I've found to load an Assembly from a DotNet Core console project is to install the `Microsoft.Extensions.DependencyModel` package and manually load your Assembly.

For example, in the Dogma.Tests project, I load the Assembly like this:

```cs
using Microsoft.Extensions.DependencyModel;

var assemblyName = DependencyContext.Default
    .GetDefaultAssemblyNames()
    .Where(a => a.Name == "Dogma.Tests")
    .First();
var assembly = Assembly.Load(assemblyName);
```

Make sure you replace the `Dogma.Tests` string with the name of your assembly. In most cases that's just the name of your project, but otherwise it will be specified in the `<AssemblyName>myAssemblyName</AssemblyName>` element in your `.csproj` file.

## Roadmap

- [ ] Allow overriding a property type with a custom attribute. 
- [ ] Allow overriding a property type with a custom attribute that can specify a module to import the type from.
- [x] Handle enums.
- [ ] If a discovered type has it's own module attribute, move it out of the current module and import it instead.
- [x] Use `[JsonProperty]` attribute to determine the name of a property.
