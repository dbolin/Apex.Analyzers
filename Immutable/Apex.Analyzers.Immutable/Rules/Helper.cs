using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class Helper
    {
        internal static bool HasImmutableAttribute(ITypeSymbol type)
        {
            var attributes = type.GetAttributes();
            return attributes.Any(x => x.AttributeClass.Name == "ImmutableAttribute"
                && x.AttributeClass.ContainingNamespace?.Name == "System");
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

        internal static bool IsImmutableType(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Dynamic)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return true;
            }

            if(HasImmutableAttribute(type))
            {
                if(type is INamedTypeSymbol nts
                    && nts.IsGenericType)
                {
                    return IsGenericTypeImmutable(nts);
                }

                return true;
            }

            if(IsWhitelistedType(type))
            {
                return true;
            }

            if(type.BaseType?.SpecialType == SpecialType.System_Enum)
            {
                return true;
            }

            return false;
        }

        private static bool IsGenericTypeImmutable(INamedTypeSymbol type)
        {
            var members = type.GetMembers();
            var fields = members.OfType<IFieldSymbol>();
            var autoProperties = members.OfType<IPropertySymbol>().Where(x => IsAutoProperty(x));

            var typesToCheck =
                fields.Where(x => type.TypeArguments.Any(t => t == x.Type)).Select(x => x.Type)
                .Concat(autoProperties.Where(x => type.TypeArguments.Any(t => t == x.Type)).Select(x => x.Type))
                .ToList();

            return typesToCheck.All(IsImmutableType);
        }

        private static bool IsWhitelistedType(ITypeSymbol type)
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
                case SpecialType.System_ValueType:
                    break;
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
            return false;
        }
    }
}
