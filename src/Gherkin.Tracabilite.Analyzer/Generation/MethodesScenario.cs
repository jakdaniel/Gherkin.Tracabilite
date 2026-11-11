using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Generation;

/// <summary>Accès aux méthodes [TestScenario] et à leur chaîne de types, partagé par le générateur et l'analyzer.</summary>
internal static class MethodesScenario
{
    internal const string NomAttribut = "Gherkin.Tracabilite.Runtime.TestScenarioAttribute";

    /// <summary>Suffixe de la classe générée qui porte les Clés d'une méthode : <c>{Methode}_Cles</c>.</summary>
    internal const string SuffixeCles = "_Cles";

    private static readonly SymbolDisplayFormat FormatNamespace = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    /// <summary>Nom de la classe générée qui porte les Clés de la méthode <paramref name="methode"/>.</summary>
    internal static string NomClasseCles(string methode) => methode + SuffixeCles;

    internal static bool EstTestScenario(AttributeData attribut) =>
        attribut.AttributeClass is { } classe && classe.ToDisplayString() == NomAttribut;

    /// <summary>Vrai si l'attribut de syntaxe est [TestScenario] (d'après sa liaison sémantique).</summary>
    internal static bool EstAttributTestScenario(AttributeSyntax attribut, SemanticModel modele, CancellationToken ct) =>
        modele.GetSymbolInfo(attribut, ct).Symbol is IMethodSymbol { ContainingType: { } type }
        && type.ToDisplayString() == NomAttribut;

    /// <summary>
    /// Littéral de chaîne unique passé à l'attribut, ou null si l'attribut n'a pas exactement un argument
    /// qui est un littéral de chaîne (SCN012).
    /// </summary>
    internal static SyntaxToken? LitteralDuScenario(AttributeSyntax attribut)
    {
        if (attribut.ArgumentList is { Arguments.Count: 1 } liste
            && liste.Arguments[0].Expression is LiteralExpressionSyntax litteral
            && litteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return litteral.Token;
        }

        return null;
    }

    /// <summary>Vrai si le générateur émet la classe <c>{Methode}_Cles</c> : méthode non surchargée et types partial (SCN011, SCN013).</summary>
    internal static bool ClesEmises(IMethodSymbol methode) =>
        !EstSurchargee(methode) && Chaine(methode).All(EstPartielle);

    /// <summary>Vrai si plusieurs méthodes de même nom de la classe portent [TestScenario] (SCN013).</summary>
    internal static bool EstSurchargee(IMethodSymbol methode) =>
        methode.ContainingType.GetMembers(methode.Name)
            .OfType<IMethodSymbol>()
            .Count(m => m.GetAttributes().Any(EstTestScenario)) > 1;

    /// <summary>Types englobant la méthode, du plus externe au plus interne (la classe de test est le dernier).</summary>
    internal static IReadOnlyList<INamedTypeSymbol> Chaine(IMethodSymbol methode)
    {
        var types = new List<INamedTypeSymbol>();
        for (var t = methode.ContainingType; t != null; t = t.ContainingType)
        {
            types.Add(t);
        }

        types.Reverse();
        return types;
    }

    /// <summary>Vrai si toutes les déclarations du type portent le mot-clé partial.</summary>
    internal static bool EstPartielle(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not TypeDeclarationSyntax declaration
                || !declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Nom de métadonnées de la classe de test : namespace, puis types englobant séparés par « + », arité en `N.</summary>
    internal static string NomDeMetadonnees(IMethodSymbol methode)
    {
        var nom = string.Join("+", Chaine(methode).Select(t => t.MetadataName));
        return methode.ContainingNamespace.IsGlobalNamespace
            ? nom
            : methode.ContainingNamespace.ToDisplayString() + "." + nom;
    }

    /// <summary>Namespace tel qu'il s'écrit dans le code (vide pour le namespace global).</summary>
    internal static string NomNamespace(INamespaceSymbol ns) =>
        ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString(FormatNamespace);

    /// <summary>Nom d'un type tel qu'il s'écrit dans le code (préfixé par « @ » s'il est un mot-clé).</summary>
    internal static string NomEcrit(INamedTypeSymbol type) =>
        SyntaxFacts.GetKeywordKind(type.Name) != SyntaxKind.None ? "@" + type.Name : type.Name;

    /// <summary>Genre de type à reproduire : class, record, struct, record struct ou interface.</summary>
    internal static string Genre(INamedTypeSymbol type)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Struct:
                return type.IsRecord ? "record struct" : "struct";
            case TypeKind.Interface:
                return "interface";
            default:
                return type.IsRecord ? "record" : "class";
        }
    }
}
