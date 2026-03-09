# Installation

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Install via NuGet

```bash
dotnet add package Picea
```

Or add to your `.csproj`:

```xml
<PackageReference Include="Picea" Version="1.0.*" />
```

## Verify Installation

```csharp
using Picea;

Console.WriteLine(typeof(Automaton<,,,,>).Assembly.GetName().Version);
```

## Package Ecosystem

| Package | Description |
| ------- | ----------- |
| `Picea` | The kernel — Automaton, Runtime, Decider, Result |
| `Picea.Abies` | MVU (Model-View-Update) runtime for Blazor |
| `Picea.Glauca` | Event Sourcing patterns |
| `Picea.Rubens` | Actor system patterns |
| `Picea.Mariana` | Resilience patterns |

## Template

```bash
dotnet new install Picea.Templates
dotnet new picea-automaton -n MyAutomaton
```

See [the template documentation](https://github.com/picea/picea) for details.
