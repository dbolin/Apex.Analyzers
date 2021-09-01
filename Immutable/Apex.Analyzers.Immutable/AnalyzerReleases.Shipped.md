; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
IMM001  |  Architecture  |  Error   | Fields in an immutable type must be readonly
IMM002  |  Architecture  |  Error   | Auto properties in an immutable type must not define a set method
IMM003  |  Architecture  |  Error   | Types of fields in an immutable type must be immutable
IMM004  |  Architecture  |  Error   | Types of auto properties in an immutable type must be immutable
IMM005  |  Architecture  |  Warning | 'This' should not be passed out of the constructor of an immutable type
IMM006  |  Architecture  |  Error   | The base type of an immutable type must be 'object' or immutable
IMM007  |  Architecture  |  Error   | Types derived from an immutable type must be immutable
IMM008  |  Architecture  |  Warning | 'This' should not be passed out of an init only property method of an immutable type

