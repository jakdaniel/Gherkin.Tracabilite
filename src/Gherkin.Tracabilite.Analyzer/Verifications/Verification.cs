using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gherkin.Tracabilite.Analyzer.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Verifications;

/// <summary>Cause d'un argument <c>cle</c> invalide (SCN002).</summary>
internal enum CauseCleVerification
{
    /// <summary>Chaîne littérale ou constante qui n'est pas une Clé générée.</summary>
    ChaineLitterale,

    /// <summary>Clé générée d'un autre Scénario (<c>{Autre}_Cles</c>).</summary>
    CleAutreScenario,

    /// <summary>Valeur qui n'est pas une constante.</summary>
    ValeurNonConstante,
}

/// <summary>
/// Un appel <c>ScenarioAssert.Verify</c> ou <c>VerifyAsync</c> d'une méthode de test, avec la Clé résolue
/// (argument <c>nameof(X_Cles.Cle)</c> ou constante <c>X_Cles.Cle</c> du type de la méthode) ou null si l'argument est invalide.
/// </summary>
internal sealed class Verification
{
    private Verification(
        InvocationExpressionSyntax appel,
        string nom,
        ExpressionSyntax argument,
        string? cle,
        CauseCleVerification? cause,
        IReadOnlyList<FauteAssertion> fautes)
    {
        Appel = appel;
        Nom = nom;
        Argument = argument;
        Cle = cle;
        Cause = cause;
        Fautes = fautes;
    }

    internal InvocationExpressionSyntax Appel { get; }

    /// <summary>Nom de l'appel : <c>Verify</c> ou <c>VerifyAsync</c>.</summary>
    internal string Nom { get; }

    /// <summary>Premier argument (<c>cle</c>).</summary>
    internal ExpressionSyntax Argument { get; }

    /// <summary>Texte de l'argument <c>cle</c>, tel qu'écrit.</summary>
    internal string TexteArgument => Argument.ToString();

    /// <summary>Clé résolue, ou null si l'argument n'est pas une Clé valide du Scénario de la méthode.</summary>
    internal string? Cle { get; }

    /// <summary>Cause de l'invalidité de l'argument, ou null si <see cref="Cle"/> est résolue.</summary>
    internal CauseCleVerification? Cause { get; }

    /// <summary>Assertions qui ne vérifient rien (SCN014 à SCN016) ; vide si la Vérification est valable.</summary>
    internal IReadOnlyList<FauteAssertion> Fautes { get; }

    /// <summary>Vrai si la Vérification est fautive : elle ne compte pas pour SCN001 (spec §6).</summary>
    internal bool Fautive => Fautes.Count > 0;

    /// <summary>
    /// Toutes les Vérifications dont la méthode propriétaire est <paramref name="methode"/>
    /// (lambdas, fonctions locales, boucles et <c>if</c> compris ; les méthodes imbriquées exclues).
    /// </summary>
    internal static IReadOnlyList<Verification> Collecter(
        MethodDeclarationSyntax methode,
        ContexteScenario contexte,
        SemanticModel modele,
        CancellationToken ct)
    {
        var resultat = new List<Verification>();
        foreach (var appel in methode.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!ReferenceEquals(AppelsVerify.MethodeProprietaire(appel), methode))
            {
                continue;
            }

            var verification = Resoudre(appel, contexte, modele, ct);
            if (verification is not null)
            {
                resultat.Add(verification);
            }
        }

        return resultat;
    }

    /// <summary>Vérification représentée par l'appel, ou null si ce n'est pas un appel Verify/VerifyAsync ou s'il n'a pas d'argument.</summary>
    internal static Verification? Resoudre(
        InvocationExpressionSyntax appel,
        ContexteScenario contexte,
        SemanticModel modele,
        CancellationToken ct)
    {
        var nom = AppelsVerify.NomSiVerification(appel, modele, ct);
        if (nom is null || appel.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var argument = appel.ArgumentList.Arguments[0].Expression;
        var cle = CleResolue(argument, contexte, modele, ct);
        var cause = cle is null ? CauseInvalide(argument, modele, ct) : (CauseCleVerification?)null;
        return new Verification(appel, nom, argument, cle, cause, AssertionsFautives.Detecter(appel, nom, modele, ct));
    }

    /// <summary>Expression désignant la Clé : l'argument de <c>nameof</c>, ou l'argument lui-même.</summary>
    private static ExpressionSyntax CibleDeLaCle(ExpressionSyntax argument) =>
        EstNameof(argument) && ((InvocationExpressionSyntax)argument).ArgumentList.Arguments.Count == 1
            ? ((InvocationExpressionSyntax)argument).ArgumentList.Arguments[0].Expression
            : argument;

    private static bool EstNameof(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } };

    private static string? CleResolue(ExpressionSyntax argument, ContexteScenario contexte, SemanticModel modele, CancellationToken ct)
    {
        if (modele.GetSymbolInfo(CibleDeLaCle(argument), ct).Symbol is not IFieldSymbol { IsConst: true, ContainingType: { } cles } champ
            || cles.Name != MethodesScenario.NomClasseCles(contexte.Methode)
            || !SymbolEqualityComparer.Default.Equals(cles.ContainingType, contexte.Classe)
            || champ.ConstantValue is not string valeur)
        {
            return null;
        }

        return contexte.ClesValides.Contains(valeur) ? valeur : null;
    }

    private static CauseCleVerification CauseInvalide(ExpressionSyntax argument, SemanticModel modele, CancellationToken ct)
    {
        if (modele.GetSymbolInfo(CibleDeLaCle(argument), ct).Symbol is IFieldSymbol { IsConst: true, ContainingType: { } cles }
            && cles.Name.EndsWith(MethodesScenario.SuffixeCles, StringComparison.Ordinal))
        {
            return CauseCleVerification.CleAutreScenario;
        }

        // Expression constante qui n'est pas une Clé du Scénario (nameof(Autre) compris) : chaîne littérale.
        if (modele.GetConstantValue(argument, ct).Value is string)
        {
            return CauseCleVerification.ChaineLitterale;
        }

        return CauseCleVerification.ValeurNonConstante;
    }
}
