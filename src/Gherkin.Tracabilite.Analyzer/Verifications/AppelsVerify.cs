using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Verifications;

/// <summary>Reconnaissance des appels <c>ScenarioAssert.Verify</c> / <c>VerifyAsync</c> et de leur méthode propriétaire.</summary>
internal static class AppelsVerify
{
    internal const string NomScenarioAssert = "Gherkin.Tracabilite.Runtime.ScenarioAssert";

    /// <summary>Nom de l'appel (<c>Verify</c> ou <c>VerifyAsync</c>), ou null si l'appel n'est pas une Vérification.</summary>
    internal static string? NomSiVerification(InvocationExpressionSyntax appel, SemanticModel modele, CancellationToken ct)
    {
        if (modele.GetSymbolInfo(appel, ct).Symbol is not IMethodSymbol { ContainingType: { } type } symbole)
        {
            return null;
        }

        if (type.ToDisplayString() != NomScenarioAssert)
        {
            return null;
        }

        return symbole.Name is "Verify" or "VerifyAsync" ? symbole.Name : null;
    }

    /// <summary>
    /// Méthode qui contient l'appel : la méthode déclarée la plus proche, en traversant lambdas et fonctions locales.
    /// Retourne null si l'appel est hors méthode (constructeur, accesseur, initialiseur, niveau du fichier).
    /// </summary>
    internal static MethodDeclarationSyntax? MethodeProprietaire(SyntaxNode noeud)
    {
        for (var parent = noeud.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case MethodDeclarationSyntax methode:
                    return methode;
                case BaseMethodDeclarationSyntax:
                case AccessorDeclarationSyntax:
                case BasePropertyDeclarationSyntax:
                case BaseTypeDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }
}
