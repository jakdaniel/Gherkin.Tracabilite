using Microsoft.CodeAnalysis;

namespace Gherkin.Tracabilite.Analyzer.Diagnostics;

/// <summary>Descripteurs des diagnostics de structure de la classe de test (SCN011, SCN013).</summary>
internal static class DescripteursStructure
{
    private const string Categorie = "Tracabilite";

    /// <summary>Args : type non partial, classe de test, méthode, précision suivie du point final.</summary>
    internal static readonly DiagnosticDescriptor Scn011 = new DiagnosticDescriptor(
        "SCN011",
        "Type non partial",
        "Le type '{0}' doit être déclaré partial pour recevoir les Clés générées du Scénario de '{1}.{2}'{3}",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Args : méthode, classe de test.</summary>
    internal static readonly DiagnosticDescriptor Scn013 = new DiagnosticDescriptor(
        "SCN013",
        "Surcharge de méthode [TestScenario]",
        "Plusieurs méthodes '{0}' portent [TestScenario] dans '{1}'. Donnez à chacune un nom unique.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
