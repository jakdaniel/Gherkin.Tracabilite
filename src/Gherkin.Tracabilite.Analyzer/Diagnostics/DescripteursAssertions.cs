using Microsoft.CodeAnalysis;

namespace Gherkin.Tracabilite.Analyzer.Diagnostics;

/// <summary>Descripteurs des assertions qui ne vérifient rien (spec §6 et §8, SCN014 à SCN016).</summary>
internal static class DescripteursAssertions
{
    private const string Categorie = "Tracabilite";

    // RS1032 : messages figés par la spec §8 (plusieurs phrases ou exemple de code), suppression locale assumée.
#pragma warning disable RS1032
    /// <summary>Arguments : {0} = Clé (ou texte de l'argument), {1} = nom de la méthode de test.</summary>
    public static readonly DiagnosticDescriptor Scn014 = new(
        "SCN014",
        "Lambda async passé à Verify",
        "La Vérification de [{0}] passe un lambda async à Verify : il devient async void et son échec ne serait pas détecté. "
        + "Écrivez : await ScenarioAssert.VerifyAsync(nameof({1}_Cles.{0}), async () => …);",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Arguments : {0} = Clé (ou texte de l'argument).</summary>
    public static readonly DiagnosticDescriptor Scn015 = new(
        "SCN015",
        "Tâche de VerifyAsync ignorée",
        "La Vérification de [{0}] appelle VerifyAsync sans utiliser la tâche retournée : "
        + "l'assertion ne serait pas attendue. Ajoutez await devant ScenarioAssert.VerifyAsync(…).",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Arguments : {0} = Clé (ou texte de l'argument), {1} = description du Résultat attendu.</summary>
    public static readonly DiagnosticDescriptor Scn016 = new(
        "SCN016",
        "Assertion vide",
        "La Vérification de [{0}] a une assertion vide : elle ne vérifie rien. "
        + "Écrivez dans le lambda l'assertion qui prouve le Résultat attendu « {1} ».",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS1032
}
