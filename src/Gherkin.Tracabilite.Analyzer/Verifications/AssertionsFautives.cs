using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gherkin.Tracabilite.Analyzer.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Verifications;

/// <summary>Une assertion qui ne vérifie rien : son descripteur (SCN014 à SCN016) et le nœud à signaler.</summary>
internal readonly struct FauteAssertion
{
    internal FauteAssertion(DiagnosticDescriptor descripteur, SyntaxNode noeud)
    {
        Descripteur = descripteur;
        Noeud = noeud;
    }

    internal DiagnosticDescriptor Descripteur { get; }

    internal SyntaxNode Noeud { get; }
}

/// <summary>
/// Détection des assertions qui compilent sans rien vérifier (spec §6) : SCN014 (lambda async dans Verify),
/// SCN015 (tâche de VerifyAsync ignorée) et SCN016 (assertion vide). Ne dépend pas de la validité de la Clé.
/// </summary>
internal static class AssertionsFautives
{
    internal static IReadOnlyList<FauteAssertion> Detecter(
        InvocationExpressionSyntax appel,
        string nom,
        SemanticModel modele,
        CancellationToken ct)
    {
        var fautes = new List<FauteAssertion>();

        if (appel.ArgumentList.Arguments.Count >= 2
            && appel.ArgumentList.Arguments[1].Expression is AnonymousFunctionExpressionSyntax fonction)
        {
            var asynchrone = fonction.ChildTokens().Any(t => t.IsKind(SyntaxKind.AsyncKeyword));
            if (nom == "Verify" && asynchrone)
            {
                fautes.Add(new FauteAssertion(DescripteursAssertions.Scn014, fonction));
            }

            if (EstAssertionVide(fonction, modele, ct))
            {
                fautes.Add(new FauteAssertion(DescripteursAssertions.Scn016, fonction));
            }
        }

        if (nom == "VerifyAsync" && EstTacheIgnoree(appel, modele, ct))
        {
            fautes.Add(new FauteAssertion(DescripteursAssertions.Scn015, appel));
        }

        return fautes;
    }

    /// <summary>Corps vide (<c>() =&gt; { }</c>, <c>async () =&gt; { }</c>) ou corps-expression <c>Task.CompletedTask</c>.</summary>
    private static bool EstAssertionVide(AnonymousFunctionExpressionSyntax fonction, SemanticModel modele, CancellationToken ct)
    {
        if (fonction.Block is not null)
        {
            return fonction.Block.Statements.Count == 0;
        }

        return fonction.ExpressionBody is MemberAccessExpressionSyntax corps
            && modele.GetSymbolInfo(corps, ct).Symbol is IPropertySymbol { Name: "CompletedTask", ContainingType: { } type }
            && type.ToDisplayString() == "System.Threading.Tasks.Task";
    }

    /// <summary>
    /// Appel dont la tâche est perdue : instruction-expression, affectation à <c>_</c>, ou corps-expression
    /// d'un lambda converti en délégué <c>void</c> (la tâche n'est alors ni attendue ni retournée).
    /// </summary>
    private static bool EstTacheIgnoree(InvocationExpressionSyntax appel, SemanticModel modele, CancellationToken ct)
    {
        if (appel.Parent is ExpressionStatementSyntax)
        {
            return true;
        }

        if (appel.Parent is AssignmentExpressionSyntax affectation)
        {
            return affectation.Right == appel
                && affectation.Left is IdentifierNameSyntax { Identifier.ValueText: "_" };
        }

        return appel.Parent is LambdaExpressionSyntax lambda
            && lambda.Body == appel
            && modele.GetTypeInfo(lambda, ct).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod: { ReturnsVoid: true } };
    }
}
