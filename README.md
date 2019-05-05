# Apex.Analyzers
Roslyn powered analyzers for C# to support convention defined architecture

## Immutable Types

[![Build Status](https://numenfall.visualstudio.com/Games/_apis/build/status/Apex.Analyzers-CI?branchName=master)](https://numenfall.visualstudio.com/Games/_build/latest?definitionId=5&branchName=master)

[Nuget Package](https://www.nuget.org/packages/Apex.Serialization/)

Provides an `ImmutableAttribute` type which can be applied to classes, structs, and interfaces.  The analyzer ensures that the following rules hold for types marked with the attribute.

| ID | Severity | Rule | Code Fix
| --- | --- | --- | --- |
| `IMM001` | Error | Fields in an immutable type must be readonly | Yes |
| `IMM002` | Error | Auto properties in an immutable type must not define a set method | Yes |
| `IMM003` | Error | Types of fields in an immutable type must be immutable | No |
| `IMM004` | Error | Types of auto properties in an immutable type must be immutable | No |
| `IMM005` | Warning | 'This' should not be passed out of the constructor of an immutable type | No |
| `IMM006` | Error | The base type of an immutable type must be 'object' or immutable | No |
| `IMM007` | Error | Types derived from an immutable type must be immutable | No |

