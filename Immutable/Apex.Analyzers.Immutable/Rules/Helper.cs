using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class Helper
    {
        internal static bool HasImmutableAttributeAndShouldVerify(ITypeSymbol type)
        {
            if(type == null)
            {
                return false;
            }

            var attributes = type.GetAttributes();
            return attributes.Any(x => x.AttributeClass?.Name == "ImmutableAttribute"
                && x.AttributeClass?.ContainingNamespace?.Name == "System"
                && (x.ConstructorArguments.Length == 0 || (x.ConstructorArguments.First().Value as bool?) == false));
        }

        internal static bool HasImmutableAttribute(ITypeSymbol type)
        {
            if (type == null)
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
    }
}
