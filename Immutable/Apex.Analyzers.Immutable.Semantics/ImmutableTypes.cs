using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

namespace Apex.Analyzers.Immutable.Semantics
{
    public sealed class ImmutableTypes
    {
        private readonly ConcurrentDictionary<ITypeSymbol, Entry> _entries = new ConcurrentDictionary<ITypeSymbol, Entry>(SymbolEqualityComparer.Default);

        private Compilation Compilation { get; set; }
        private AnalyzerOptions AnalyzerOptions { get; set; }
        private CancellationToken CancellationToken { get; set; }

        public ImmutableTypes(Compilation compilation, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
        {
            Initialize(compilation, analyzerOptions, cancellationToken);
        }

        internal ImmutableTypes()
        {
        }

        internal void Initialize(Compilation compilation, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
        {
            Compilation = compilation;
            AnalyzerOptions = analyzerOptions;
            CancellationToken = cancellationToken;
        }

        public bool IsImmutableType(ITypeSymbol type, ref string genericTypeArgument)
        {
            var entry = _entries.GetOrAdd(type, x => GetEntry(x, null));
            if (!string.IsNullOrEmpty(entry.MutableGenericTypeArgument))
            {
                genericTypeArgument = entry.MutableGenericTypeArgument;
            }
            return entry.IsImmutable;
        }

        private Entry GetEntry(ITypeSymbol type, HashSet<ITypeSymbol> excludedTypes)
        {
            if (type.TypeKind == TypeKind.Dynamic)
            {
                return Entry.NotImmutable;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return Entry.Immutable;
            }

            if (type is INamedTypeSymbol nts && nts.IsGenericType)
            {
                if (Helper.HasImmutableAttribute(type) || IsWhitelistedType(nts.OriginalDefinition))
                {
                    if (type.TypeKind == TypeKind.Delegate)
                    {
                        return Entry.Immutable;
                    }
                    else if (Helper.HasImmutableAttributeAndShouldVerify(type))
                    {
                        return GetGenericImmutableTypeEntry(nts, excludedTypes);
                    }
                    else
                    {
                        return GetGenericTypeArgumentsEntry(nts, excludedTypes);
                    }
                }
            }

            if (Helper.HasImmutableAttribute(type) || IsWhitelistedType(type))
            {
                return Entry.Immutable;
            }

            return Entry.NotImmutable;
        }

        private Entry GetGenericTypeArgumentsEntry(INamedTypeSymbol type, HashSet<ITypeSymbol> excludedTypes = null)
        {
            excludedTypes = excludedTypes ?? new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            excludedTypes.Add(type);

            var typesToCheck = type.TypeArguments;
            return GetGenericTypeArgumentsEntry(typesToCheck, excludedTypes);
        }

        private Entry GetGenericImmutableTypeEntry(INamedTypeSymbol type, HashSet<ITypeSymbol> excludedTypes = null)
        {
            excludedTypes = excludedTypes ?? new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            excludedTypes.Add(type);

            var members = type.GetMembers();
            var fields = members.OfType<IFieldSymbol>();
            var autoProperties = members.OfType<IPropertySymbol>().Where(x => Helper.IsAutoProperty(x));

            var filter = ShouldCheckTypeForGenericImmutability(type);

            var typesToCheck =
                fields.Select(x => x.Type).Where(filter)
                .Concat(autoProperties.Select(x => x.Type).Where(filter))
                .Where(x => !excludedTypes.Contains(x))
                .ToList();
            return GetGenericTypeArgumentsEntry(typesToCheck, excludedTypes);
        }

        private Entry GetGenericTypeArgumentsEntry(IEnumerable<ITypeSymbol> typesToCheck, HashSet<ITypeSymbol> excludedTypes)
        {
            var result = Entry.Immutable;
            foreach (var typeToCheck in typesToCheck)
            {
                result = GetEntry(typeToCheck, excludedTypes);
                if (!result.IsImmutable)
                {
                    if (string.IsNullOrEmpty(result.MutableGenericTypeArgument))
                    {
                        result.MutableGenericTypeArgument = typeToCheck.Name;
                    }
                    break;
                }
            }
            return result;
        }

        private static Func<ITypeSymbol, bool> ShouldCheckTypeForGenericImmutability(INamedTypeSymbol type)
        {
            return t =>
            {
                if (type.TypeArguments.Any(x => SymbolEqualityComparer.Default.Equals(x, t)))
                {
                    return true;
                }

                return t is INamedTypeSymbol nts && nts.IsGenericType;
            };
        }

        public bool IsWhitelistedType(ITypeSymbol type)
        {
            if (Helper.HasImmutableNamespace(type))
            {
                return SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition);
            }

            switch (type.SpecialType)
            {
                case SpecialType.None:
                    break;
                case SpecialType.System_Object:
                case SpecialType.System_Enum:
                    return true;
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Delegate:
                    break;
                case SpecialType.System_ValueType:
                    return true;
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    return true;
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Array:
                    break;
                case SpecialType.System_Collections_IEnumerable:
                    break;
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                    break;
                case SpecialType.System_Collections_Generic_IList_T:
                    break;
                case SpecialType.System_Collections_Generic_ICollection_T:
                    break;
                case SpecialType.System_Collections_IEnumerator:
                    break;
                case SpecialType.System_Collections_Generic_IEnumerator_T:
                    break;
                case SpecialType.System_Collections_Generic_IReadOnlyList_T:
                    break;
                case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
                    break;
                case SpecialType.System_Nullable_T:
                    return true;
                case SpecialType.System_DateTime:
                    return true;
                case SpecialType.System_Runtime_CompilerServices_IsVolatile:
                    break;
                case SpecialType.System_IDisposable:
                    break;
                case SpecialType.System_TypedReference:
                    break;
                case SpecialType.System_ArgIterator:
                    break;
                case SpecialType.System_RuntimeArgumentHandle:
                    break;
                case SpecialType.System_RuntimeFieldHandle:
                    break;
                case SpecialType.System_RuntimeMethodHandle:
                    break;
                case SpecialType.System_RuntimeTypeHandle:
                    break;
                case SpecialType.System_IAsyncResult:
                    break;
                case SpecialType.System_AsyncCallback:
                    break;
            }

            if (GetWhitelist().Contains(type))
            {
                return true;
            }

            if (type.BaseType?.SpecialType == SpecialType.System_Enum)
            {
                return true;
            }

            return false;
        }

        private const string ImmutableTypesFileName = "ImmutableTypes.txt";
        private IImmutableSet<ISymbol> _whitelist;

        private IImmutableSet<ISymbol> GetWhitelist()
        {
            return _whitelist ?? (_whitelist = ReadWhitelist());
        }

        private IImmutableSet<ISymbol> ReadWhitelist()
        {
            var query =
                from additionalFile in AnalyzerOptions.AdditionalFiles
                where StringComparer.Ordinal.Equals(Path.GetFileName(additionalFile.Path), ImmutableTypesFileName)
                let sourceText = additionalFile.GetText(CancellationToken)
                where sourceText != null
                from line in sourceText.Lines
                let text = line.ToString()
                where !string.IsNullOrWhiteSpace(text)
                select text;

            var entries = query.ToList();
            entries.Add("System.Guid");
            entries.Add("System.TimeSpan");
            entries.Add("System.DateTimeOffset");
            entries.Add("System.Uri");
            entries.Add("System.Nullable`1");
            entries.Add("System.Collections.Generic.KeyValuePair`2");
            var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var entry in entries)
            {
                var symbols = DocumentationCommentId.GetSymbolsForDeclarationId($"T:{entry}", Compilation);
                if (symbols.IsDefaultOrEmpty)
                {
                    continue;
                }
                foreach (var symbol in symbols)
                {
                    result.Add(symbol);
                }
            }
            return result.ToImmutableHashSet(SymbolEqualityComparer.Default);
        }

        private struct Entry
        {
            public static Entry Immutable => new Entry { IsImmutable = true };
            public static Entry NotImmutable => new Entry { IsImmutable = false };

            public bool IsImmutable { get; set; }
            public string MutableGenericTypeArgument { get; set; }
        }
    }
}
