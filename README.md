# Apex.Analyzers
Roslyn powered analyzers for C# to support convention defined architecture

## Immutable Types

Provides an `ImmutableAttribute` type which can be applied to classes and structs.  The analyzer ensures that the following rules hold for types marked with the attribute.

| ID | Severity | Rule
| --- | --- | --- |
| `IMM001` | Error | Fields in an immutable type must be readonly
| `IMM002` | Error | Auto properties in an immutable type must not define a set method
| `IMM003` | Error | Types of fields in an immutable type must be immutable
| `IMM004` | Error | Types of auto properties in an immutable type must be immutable
| `IMM005` | Warning | 'This' reference cannot be passed out of the constructor of an immutable type
| `IMM006` | Warning | 'This' reference cannot be captured in a closure within the constructor of an immutable type
| `IMM007` | Error | The base type of an immutable type must be 'object' or immutable
| `IMM008` | Error | Types derived from an immutable type must be immutable

