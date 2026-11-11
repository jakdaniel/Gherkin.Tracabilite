using Microsoft.CodeAnalysis;

namespace Gherkin.Tracabilite.Analyzer.Diagnostics;

/// <summary>Descripteurs des diagnostics de Vérification (spec §8, SCN001, SCN002 et SCN010).</summary>
internal static class DescripteursVerifications
{
    private const string Categorie = "Tracabilite";

    // RS1032 : messages figés par la spec §8 (plusieurs phrases ou exemple de code), suppression locale assumée.
#pragma warning disable RS1032
    public static readonly DiagnosticDescriptor Scn001 = new(
        "SCN001",
        "Résultat attendu sans Vérification",
        "Le Résultat attendu [{0}] (« {1} ») du Scénario de '{2}.{3}' n'a aucune Vérification. "
        + "Ajoutez dans la méthode : ScenarioAssert.Verify(nameof({3}_Cles.{0}), () => …);",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn002 = new(
        "SCN002",
        "Argument de Verify qui n'est pas une Clé du Scénario",
        "L'argument de {0} doit être une Clé du Scénario de '{1}', écrite nameof({1}_Cles.<Clé>). "
        + "Reçu : {2} ({3}). Clés disponibles : {4}.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn010 = new(
        "SCN010",
        "Vérification hors d'une méthode [TestScenario]",
        "ScenarioAssert.{0} est appelé dans '{1}', qui n'a pas d'attribut [TestScenario]. "
        + "Ajoutez l'attribut ou retirez l'appel.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS1032
}
