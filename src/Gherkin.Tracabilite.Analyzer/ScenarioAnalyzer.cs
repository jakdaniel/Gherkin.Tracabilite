using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer.Diagnostics;
using Gherkin.Tracabilite.Analyzer.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Gherkin.Tracabilite.Analyzer;

/// <summary>
/// Diagnostics du texte de Scénario de <c>[TestScenario]</c> (spec §8, SCN003 à SCN009 et SCN012).
/// Le texte est lu depuis le littéral de l'attribut et analysé par <see cref="ScenarioParser"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScenarioAnalyzer : DiagnosticAnalyzer
{
    private const string NomCompletAttribut = "Gherkin.Tracabilite.Runtime.TestScenarioAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DescripteursScenario.Scn003,
            DescripteursScenario.Scn004,
            DescripteursScenario.Scn005,
            DescripteursScenario.Scn006,
            DescripteursScenario.Scn007,
            DescripteursScenario.Scn008,
            DescripteursScenario.Scn009,
            DescripteursScenario.Scn012);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyserMethode, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyserMethode(SyntaxNodeAnalysisContext ctx)
    {
        var methode = (MethodDeclarationSyntax)ctx.Node;
        var nom = methode.Identifier.ValueText;

        foreach (var liste in methode.AttributeLists)
        {
            foreach (var attribut in liste.Attributes)
            {
                if (!EstTestScenario(attribut, ctx))
                {
                    continue;
                }

                AnalyserAttribut(ctx, attribut, nom);
            }
        }
    }

    private static bool EstTestScenario(AttributeSyntax attribut, SyntaxNodeAnalysisContext ctx)
    {
        var symbole = ctx.SemanticModel.GetSymbolInfo(attribut, ctx.CancellationToken).Symbol;
        return symbole is IMethodSymbol { ContainingType: { } type }
            && type.ToDisplayString() == NomCompletAttribut;
    }

    private static void AnalyserAttribut(SyntaxNodeAnalysisContext ctx, AttributeSyntax attribut, string methode)
    {
        if (attribut.ArgumentList is null || attribut.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        var expression = attribut.ArgumentList.Arguments[0].Expression;
        if (expression is not LiteralExpressionSyntax litteral
            || !litteral.IsKind(SyntaxKind.StringLiteralExpression))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(DescripteursScenario.Scn012, attribut.GetLocation(), methode));
            return;
        }

        var analyse = ScenarioParser.Analyser(litteral.Token.ValueText, methode);
        var contexte = new ContexteLocalisation(litteral.Token, attribut);

        foreach (var erreur in analyse.Erreurs)
        {
            var diagnostic = CreerDiagnostic(erreur, methode, contexte);
            if (diagnostic is not null)
            {
                ctx.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static Diagnostic? CreerDiagnostic(ErreurScenario erreur, string methode, ContexteLocalisation contexte)
    {
        var ligne = erreur.Ligne;
        var emplacement = contexte.Emplacement(ligne);

        switch (erreur)
        {
            case ResultatSansCle sansCle:
            {
                var contenu = sansCle.Ligne!.Contenu.Trim();
                var motCle = sansCle.Ligne.MotCle ?? string.Empty;
                var description = contenu.Length >= motCle.Length ? contenu.Substring(motCle.Length).Trim() : contenu;
                return Diagnostic.Create(DescripteursScenario.Scn003, emplacement, contenu, motCle, description);
            }

            case CleEnDouble enDouble:
                return Diagnostic.Create(DescripteursScenario.Scn004, emplacement, enDouble.Cle, methode);

            case CleInvalide invalide:
                return Diagnostic.Create(
                    DescripteursScenario.Scn005,
                    emplacement,
                    invalide.Cle,
                    TexteCauseCle(invalide.Cause, methode));

            case CleSurMauvaiseEtape mauvaise:
                return Diagnostic.Create(
                    DescripteursScenario.Scn006,
                    emplacement,
                    mauvaise.Cle,
                    mauvaise.Ligne!.MotCle ?? string.Empty);

            case StructureInvalide structure:
                return Diagnostic.Create(DescripteursScenario.Scn007, emplacement, methode, TexteCauseStructure(structure.Cause));

            case LigneNonReconnue non:
                return Diagnostic.Create(
                    DescripteursScenario.Scn008,
                    emplacement,
                    methode,
                    non.Ligne!.Contenu.Trim(),
                    string.Join(", ", non.MotsClesAttendus));

            case LanguesMelangees melange:
                return Diagnostic.Create(
                    DescripteursScenario.Scn009,
                    emplacement,
                    methode,
                    NomLangue(melange.LangueDetectee),
                    melange.MotCleLangue,
                    NomLangue(melange.AutreLangue),
                    melange.Ligne!.Contenu.Trim());

            default:
                return null;
        }
    }

    private static string TexteCauseCle(CauseCleInvalide cause, string methode) => cause switch
    {
        CauseCleInvalide.NonIdentifiant => "ce n'est pas un identifiant C# valide",
        CauseCleInvalide.MotReserve => "c'est un mot réservé",
        _ => $"elle porte le nom de la classe générée {methode}_Cles",
    };

    private static string TexteCauseStructure(CauseStructure cause) => cause switch
    {
        CauseStructure.AucuneLigneScenario => "aucune ligne Scénario",
        CauseStructure.PlusieursLignesScenario => "plusieurs lignes Scénario",
        CauseStructure.AucunAlors => "aucun ALORS",
        CauseStructure.EtOuMaisSansEtapePrecedente => "ET ou MAIS sans étape précédente",
        _ => "description vide après la Clé",
    };

    private static string NomLangue(Langue langue) => langue == Langue.Francais ? "français" : "anglais";
}
