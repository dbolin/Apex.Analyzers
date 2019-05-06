using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class Helper
    {
        internal static bool HasImmutableAttribute(ITypeSymbol type)
        {
            if(type == null)
            {
                return false;
            }

            var attributes = type.GetAttributes();
            return attributes.Any(x => x.AttributeClass?.Name == "ImmutableAttribute"
                && x.AttributeClass?.ContainingNamespace?.Name == "System");
        }

        internal static bool IsAutoProperty(IPropertySymbol symbol)
        {
            var getSyntax = symbol.GetMethod?.DeclaringSyntaxReferences.Select(x => x.GetSyntax());
            var result = getSyntax?.OfType<AccessorDeclarationSyntax>().Where(x => x.Body == null && x.ExpressionBody == null);
            if(result != null && result.Any())
            {
                return true;
            }

            var setSyntax = symbol.SetMethod?.DeclaringSyntaxReferences.Select(x => x.GetSyntax());
            result = setSyntax?.OfType<AccessorDeclarationSyntax>().Where(x => x.Body == null && x.ExpressionBody == null);
            return result != null && result.Any();
        }

        internal static bool IsImmutableType(ITypeSymbol type, Compilation compilation, ref string genericTypeParameter, HashSet<ITypeSymbol> excludedTypes = null)
        {
            if (type.TypeKind == TypeKind.Dynamic)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return true;
            }

            if(HasImmutableNamespace(type) || type.OriginalDefinition == GetNullableType(compilation))
            {
                if (type is INamedTypeSymbol nts
                    && nts.IsGenericType)
                {
                    return AreGenericTypeArgumentsImmutable(nts, compilation, ref genericTypeParameter, excludedTypes);
                }

                return true;
            }

            if (HasImmutableAttribute(type))
            {
                if(type is INamedTypeSymbol nts
                    && nts.IsGenericType)
                {
                    return IsGenericTypeImmutable(nts, compilation, ref genericTypeParameter, excludedTypes);
                }

                return true;
            }

            if(IsWhitelistedType(type, compilation))
            {
                return true;
            }

            if(type.BaseType?.SpecialType == SpecialType.System_Enum)
            {
                return true;
            }

            return false;
        }

        internal static bool HasImmutableNamespace(ITypeSymbol type)
        {
            return type.ContainingNamespace?.Name == "Immutable"
                && type.ContainingNamespace?.ContainingNamespace?.Name == "Collections"
                && type.ContainingNamespace?.ContainingNamespace?.ContainingNamespace?.Name == "System";
        }

        internal static bool ShouldCheckMemberTypeForImmutability(ISymbol symbol)
        {
            return !symbol.IsStatic
                && (symbol.DeclaredAccessibility != Accessibility.Private
                    || !symbol.GetAttributes().Any(x => x.AttributeClass.Name == "NonSerializedAttribute"
                    && x.AttributeClass.ContainingNamespace?.Name == "System"));
        }

        private static bool IsGenericTypeImmutable(INamedTypeSymbol type, Compilation compilation,
            ref string genericTypeParameter,
            HashSet<ITypeSymbol> excludedTypes = null)
        {
            if(excludedTypes == null)
            {
                excludedTypes = new HashSet<ITypeSymbol>();
            }
            excludedTypes.Add(type);

            var members = type.GetMembers();
            var fields = members.OfType<IFieldSymbol>();
            var autoProperties = members.OfType<IPropertySymbol>().Where(x => IsAutoProperty(x));

            var filter = ShouldCheckTypeForGenericImmutability(type);

            var typesToCheck =
                fields.Select(x => x.Type).Where(filter)
                .Concat(autoProperties.Select(x => x.Type).Where(filter))
                .Where(x => !excludedTypes.Contains(x))
                .ToList();

            var result = true;
            foreach (var typeToCheck in typesToCheck)
            {
                result = IsImmutableType(typeToCheck, compilation, ref genericTypeParameter, excludedTypes);
                if (!result)
                {
                    if (genericTypeParameter == null)
                    {
                        genericTypeParameter = typeToCheck.Name;
                    }
                    break;
                }
            }

            return result;
        }

        private static bool AreGenericTypeArgumentsImmutable(INamedTypeSymbol type, Compilation compilation,
            ref string genericTypeParameter,
            HashSet<ITypeSymbol> excludedTypes = null)
        {
            if (excludedTypes == null)
            {
                excludedTypes = new HashSet<ITypeSymbol>();
            }
            excludedTypes.Add(type);

            var typesToCheck = type.TypeArguments;

            var result = true;
            foreach(var typeToCheck in typesToCheck)
            {
                result = IsImmutableType(typeToCheck, compilation, ref genericTypeParameter, excludedTypes);
                if(!result)
                {
                    if (genericTypeParameter == null)
                    {
                        genericTypeParameter = typeToCheck.Name;
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
                if(type.TypeArguments.Any(x => x == t))
                {
                    return true;
                }

                return t is INamedTypeSymbol nts && nts.IsGenericType;
            };
        }

        private static bool IsWhitelistedType(ITypeSymbol type, Compilation compilation)
        {
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

            if(
                GetGuidType(compilation) == type
                || GetTimeSpanType(compilation) == type
                || GetDateTimeOffsetType(compilation) == type
                || GetUriType(compilation) == type
                )
            {
                return true;
            }

            return false;
        }

        private static ITypeSymbol _guidSymbol;

        private static ITypeSymbol GetGuidType(Compilation compilation)
        {
            return _guidSymbol ?? (_guidSymbol = compilation.GetTypeByMetadataName("System.Guid"));
        }

        private static ITypeSymbol _timeSpanSymbol;

        private static ITypeSymbol GetTimeSpanType(Compilation compilation)
        {
            return _timeSpanSymbol ?? (_timeSpanSymbol = compilation.GetTypeByMetadataName("System.TimeSpan"));
        }

        private static ITypeSymbol _dateTimeOffsetSymbol;

        private static ITypeSymbol GetDateTimeOffsetType(Compilation compilation)
        {
            return _dateTimeOffsetSymbol ?? (_dateTimeOffsetSymbol = compilation.GetTypeByMetadataName("System.DateTimeOffset"));
        }

        private static ITypeSymbol _uriSymbol;

        private static ITypeSymbol GetUriType(Compilation compilation)
        {
            return _uriSymbol ?? (_uriSymbol = compilation.GetTypeByMetadataName("System.Uri"));
        }

        private static ITypeSymbol _nullableType;

        private static ITypeSymbol GetNullableType(Compilation compilation)
        {
            return _nullableType ?? (_nullableType = compilation.GetTypeByMetadataName("System.Nullable`1"));
        }
    }
}
