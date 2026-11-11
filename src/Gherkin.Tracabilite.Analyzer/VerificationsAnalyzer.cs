using System.Collections.Immutable;
using System.Linq;
using Gherkin.Tracabilite.Analyzer.Diagnostics;
using Gherkin.Tracabilite.Analyzer.Verifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gherkin.Tracabilite.Analyzer;

/// <summary>Diagnostics des Vérifications : SCN001 (Résultat attendu sans Vérification), SCN002 (argument de Verify) et SCN010 (Verify hors [TestScenario]).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VerificationsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DescripteursVerifications.Scn001,
            DescripteursVerifications.Scn002,
            DescripteursVerifications.Scn010);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyserAppel, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyserMethode, SyntaxKind.MethodDeclaration);
    }

    /// <summary>
    /// SCN001 : chaque Résultat attendu à Clé valide doit avoir au moins une Vérification valable de sa Clé.
    /// Une Vérification fautive (SCN014 à SCN016) ne compte pas.
    /// </summary>
    private static void AnalyserMethode(SyntaxNodeAnalysisContext ctx)
    {
        var methode = (MethodDeclarationSyntax)ctx.Node;
        var contexte = ContexteScenario.Creer(methode, ctx.SemanticModel, ctx.CancellationToken);
        if (contexte?.Litteral is null || contexte.Analyse is null || !contexte.ClesGenerees)
        {
            return;
        }

        var couvertes = new HashSet<string>(
            Verification.Collecter(methode, contexte, ctx.SemanticModel, ctx.CancellationToken)
                .Where(v => v.Cle is not null && !v.Fautive)
                .Select(v => v.Cle!),
            StringComparer.Ordinal);

        var localisation = new ContexteLocalisation(contexte.Litteral.Value, contexte.Attribut);
        foreach (var etape in contexte.Analyse.Etapes)
        {
            if (!etape.EstResultatAttendu || !etape.CleValide || etape.Cle is null || couvertes.Contains(etape.Cle))
            {
                continue;
            }

            ctx.ReportDiagnostic(Diagnostic.Create(
                DescripteursVerifications.Scn001,
                localisation.Emplacement(etape.Ligne),
                etape.Cle,
                etape.Description,
                contexte.Classe.Name,
                contexte.Methode));
        }
    }

    /// <summary>SCN010 : appel hors méthode [TestScenario]. SCN002 : argument <c>cle</c> qui n'est pas une Clé du Scénario.</summary>
    private static void AnalyserAppel(SyntaxNodeAnalysisContext ctx)
    {
        var appel = (InvocationExpressionSyntax)ctx.Node;
        var nomVerification = AppelsVerify.NomSiVerification(appel, ctx.SemanticModel, ctx.CancellationToken);
        if (nomVerification is null)
        {
            return;
        }

        var methode = AppelsVerify.MethodeProprietaire(appel);
        var contexte = methode is null
            ? null
            : ContexteScenario.Creer(methode, ctx.SemanticModel, ctx.CancellationToken);
        if (contexte is null)
        {
            var nom = methode?.Identifier.ValueText
                ?? appel.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText
                ?? "<fichier>";

            ctx.ReportDiagnostic(Diagnostic.Create(DescripteursVerifications.Scn010, appel.GetLocation(), nomVerification, nom));
            return;
        }

        // Pas de Clés générées : la cause est ailleurs (SCN011, SCN012, SCN013), inutile d'en rajouter.
        if (!contexte.ClesGenerees)
        {
            return;
        }

        var verification = Verification.Resoudre(appel, contexte, ctx.SemanticModel, ctx.CancellationToken);
        if (verification is null || verification.Cle is not null || verification.Cause is not { } cause)
        {
            return;
        }

        var clesDisponibles = contexte.ClesValides.Count == 0 ? "aucune" : string.Join(", ", contexte.ClesValides);
        ctx.ReportDiagnostic(Diagnostic.Create(
            DescripteursVerifications.Scn002,
            verification.Argument.GetLocation(),
            verification.Nom,
            contexte.Methode,
            verification.TexteArgument,
            TexteCause(cause),
            clesDisponibles));
    }

    private static string TexteCause(CauseCleVerification cause) => cause switch
    {
        CauseCleVerification.CleAutreScenario => "Clé d'un autre Scénario",
        CauseCleVerification.ValeurNonConstante => "valeur non constante",
        _ => "chaîne littérale",
    };
}
