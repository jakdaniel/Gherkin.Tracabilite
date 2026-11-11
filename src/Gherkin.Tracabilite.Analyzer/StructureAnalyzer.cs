using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer.Diagnostics;
using Gherkin.Tracabilite.Analyzer.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gherkin.Tracabilite.Analyzer;

/// <summary>Structure de la classe de test : SCN011 (type non partial) et SCN013 (surcharge de méthode [TestScenario]).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DescripteursStructure.Scn011, DescripteursStructure.Scn013);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyserMethode, SymbolKind.Method);
    }

    private static void AnalyserMethode(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not IMethodSymbol methode)
        {
            return;
        }

        var attribut = methode.GetAttributes().FirstOrDefault(MethodesScenario.EstTestScenario);
        if (attribut == null)
        {
            return;
        }

        var emplacement = attribut.ApplicationSyntaxReference?.GetSyntax(ctx.CancellationToken).GetLocation()
            ?? Location.None;
        var classeDeTest = methode.ContainingType;

        if (MethodesScenario.EstSurchargee(methode))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DescripteursStructure.Scn013,
                emplacement,
                methode.Name,
                classeDeTest.Name));
        }

        var premierNonPartial = MethodesScenario.Chaine(methode).FirstOrDefault(t => !MethodesScenario.EstPartielle(t));
        if (premierNonPartial != null)
        {
            var precision = SymbolEqualityComparer.Default.Equals(premierNonPartial, classeDeTest)
                ? "."
                : " (il contient la classe de test).";

            ctx.ReportDiagnostic(Diagnostic.Create(
                DescripteursStructure.Scn011,
                emplacement,
                premierNonPartial.Name,
                classeDeTest.Name,
                methode.Name,
                precision));
        }
    }
}
