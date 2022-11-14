﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

static class SymbolExtensions
{
    static readonly SymbolDisplayFormat fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static string ToAssemblyNamespace(this INamespaceSymbol symbol)
        => symbol.ContainingAssembly.Name + "." + symbol.ToDisplayString(fullNameFormat);

    public static string ToFullName(this ISymbol symbol, Compilation compilation)
    {
        var fullName = symbol.ToDisplayString(fullNameFormat);
        if (compilation.GetMetadataReference(symbol.ContainingAssembly) is MetadataReference reference &&
            !reference.Properties.Aliases.IsDefaultOrEmpty)
            return reference.Properties.Aliases.First() + "::" + fullName;

        return "global::" + fullName;
    }

    public static string ToFullName(this ISymbol symbol, NameSyntax name, CancellationToken cancellation = default)
    {
        var fullName = symbol.ToDisplayString(fullNameFormat);
        var root = name.SyntaxTree.GetRoot(cancellation);
        var aliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>().Select(x => x.Identifier.Text).ToList();

        var candidate = name;
        while (candidate is QualifiedNameSyntax qualified)
            candidate = qualified.Left;

        if (candidate is IdentifierNameSyntax identifier &&
            aliases.FirstOrDefault(x => x == identifier.Identifier.Text) is string alias)
            return alias + ":" + fullName;

        return fullName;
    }

    /// <summary>
    /// Checks whether the <paramref name="this"/> type inherits or implements the 
    /// <paramref name="baseTypeOrInterface"/> type, even if it's a generic type.
    /// </summary>
    public static bool Is(this ITypeSymbol? @this, ITypeSymbol? baseTypeOrInterface)
    {
        if (@this == null || baseTypeOrInterface == null)
            return false;

        if (@this.Equals(baseTypeOrInterface, SymbolEqualityComparer.Default) == true)
            return true;

        if (baseTypeOrInterface is INamedTypeSymbol namedExpected &&
            @this is INamedTypeSymbol namedActual &&
            namedActual.IsGenericType &&
            namedActual.ConstructedFrom.Equals(namedExpected, SymbolEqualityComparer.Default))
            return true;

        foreach (var iface in @this.AllInterfaces)
            if (iface.Is(baseTypeOrInterface))
                return true;

        if (@this.BaseType?.Name.Equals("object", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        return Is(@this.BaseType, baseTypeOrInterface);
    }
}
