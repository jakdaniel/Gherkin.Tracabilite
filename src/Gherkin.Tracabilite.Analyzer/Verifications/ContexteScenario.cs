using System.Linq;
using System.Threading;
using Gherkin.Tracabilite.Analyzer.Generation;
using Gherkin.Tracabilite.Analyzer.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Verifications;

/// <summary>
/// Scénario d'une méthode <c>[TestScenario]</c> tel que les Vérifications le voient : son littéral, son analyse,
/// et si la classe <c>{Methode}_Cles</c> est effectivement générée (spec §5 : littéral, non surchargée, types partial).
/// </summary>
internal sealed class ContexteScenario
{
    private ContexteScenario(
        MethodDeclarationSyntax syntaxe,
        IMethodSymbol symbole,
        AttributeSyntax attribut,
        SyntaxToken? litteral,
        AnalyseScenario? analyse,
        bool clesGenerees)
    {
        Syntaxe = syntaxe;
        Symbole = symbole;
        Attribut = attribut;
        Litteral = litteral;
        Analyse = analyse;
        ClesGenerees = clesGenerees;
    }

    internal MethodDeclarationSyntax Syntaxe { get; }

    internal IMethodSymbol Symbole { get; }

    /// <summary>Nom de la méthode de test.</summary>
    internal string Methode => Syntaxe.Identifier.ValueText;

    /// <summary>Classe de test, déclarant la méthode.</summary>
    internal INamedTypeSymbol Classe => Symbole.ContainingType;

    /// <summary>Attribut <c>[TestScenario]</c> de la méthode.</summary>
    internal AttributeSyntax Attribut { get; }

    /// <summary>Littéral du Scénario, ou null si le Scénario n'est pas un littéral (SCN012).</summary>
    internal SyntaxToken? Litteral { get; }

    /// <summary>Analyse du Scénario, ou null si le Scénario n'est pas un littéral.</summary>
    internal AnalyseScenario? Analyse { get; }

    /// <summary>Vrai si le générateur émet <c>{Methode}_Cles</c> pour cette méthode.</summary>
    internal bool ClesGenerees { get; }

    /// <summary>Clés valides du Scénario (vide si le Scénario n'est pas un littéral).</summary>
    internal IReadOnlyList<string> ClesValides => Analyse?.ClesValides ?? Array.Empty<string>();

    /// <summary>Contexte de la méthode, ou null si elle ne porte pas [TestScenario].</summary>
    internal static ContexteScenario? Creer(MethodDeclarationSyntax methode, SemanticModel modele, CancellationToken ct)
    {
        if (modele.GetDeclaredSymbol(methode, ct) is not IMethodSymbol symbole
            || !symbole.GetAttributes().Any(MethodesScenario.EstTestScenario))
        {
            return null;
        }

        var attribut = methode.AttributeLists
            .SelectMany(liste => liste.Attributes)
            .FirstOrDefault(a => EstAttributTestScenario(a, modele, ct));
        if (attribut is null)
        {
            return null;
        }

        var litteral = LitteralDuScenario(attribut);
        if (litteral is null)
        {
            return new ContexteScenario(methode, symbole, attribut, null, null, clesGenerees: false);
        }

        var analyse = ScenarioParser.Analyser(litteral.Value.ValueText, methode.Identifier.ValueText);
        var generees = !MethodesScenario.EstSurchargee(symbole)
            && MethodesScenario.Chaine(symbole).All(MethodesScenario.EstPartielle);

        return new ContexteScenario(methode, symbole, attribut, litteral, analyse, generees);
    }

    private static bool EstAttributTestScenario(AttributeSyntax attribut, SemanticModel modele, CancellationToken ct) =>
        modele.GetSymbolInfo(attribut, ct).Symbol is IMethodSymbol { ContainingType: { } type }
        && type.ToDisplayString() == MethodesScenario.NomAttribut;

    private static SyntaxToken? LitteralDuScenario(AttributeSyntax attribut)
    {
        if (attribut.ArgumentList is { Arguments.Count: 1 } liste
            && liste.Arguments[0].Expression is LiteralExpressionSyntax litteral
            && litteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return litteral.Token;
        }

        return null;
    }
}
