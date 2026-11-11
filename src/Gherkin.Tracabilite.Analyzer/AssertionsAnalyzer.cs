using System.Collections.Immutable;
using System.Linq;
using Gherkin.Tracabilite.Analyzer.Diagnostics;
using Gherkin.Tracabilite.Analyzer.Verifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gherkin.Tracabilite.Analyzer;

/// <summary>Diagnostics des assertions qui ne vérifient rien : SCN014 (lambda async dans Verify), SCN015 (tâche de VerifyAsync ignorée), SCN016 (assertion vide).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssertionsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DescripteursAssertions.Scn014,
            DescripteursAssertions.Scn015,
            DescripteursAssertions.Scn016);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyserAppel, SyntaxKind.InvocationExpression);
    }

    /// <summary>Appel Verify/VerifyAsync d'une méthode [TestScenario] : signale chaque assertion fautive. Hors méthode de Scénario, c'est SCN010 qui signale.</summary>
    private static void AnalyserAppel(SyntaxNodeAnalysisContext ctx)
    {
        var appel = (InvocationExpressionSyntax)ctx.Node;
        if (AppelsVerify.NomSiVerification(appel, ctx.SemanticModel, ctx.CancellationToken) is null)
        {
            return;
        }

        var methode = AppelsVerify.MethodeProprietaire(appel);
        var contexte = methode is null
            ? null
            : ContexteScenario.Creer(methode, ctx.SemanticModel, ctx.CancellationToken);
        if (contexte is null)
        {
            return;
        }

        var verification = Verification.Resoudre(appel, contexte, ctx.SemanticModel, ctx.CancellationToken);
        if (verification is null || verification.Fautes.Count == 0)
        {
            return;
        }

        // {Cle} est le texte de l'argument si celui-ci n'est pas une Clé valide (SCN002).
        var cle = verification.Cle ?? verification.TexteArgument;
        var description = verification.Cle is null
            ? "?"
            : contexte.Analyse?.Etapes
                .FirstOrDefault(e => e.EstResultatAttendu && e.Cle == verification.Cle)?.Description
              ?? "?";

        foreach (var faute in verification.Fautes)
        {
            object[] arguments = faute.Descripteur.Id switch
            {
                "SCN014" => new object[] { cle, contexte.Methode },
                "SCN016" => new object[] { cle, description },
                _ => new object[] { cle },
            };

            ctx.ReportDiagnostic(Diagnostic.Create(faute.Descripteur, faute.Noeud.GetLocation(), arguments));
        }
    }
}
