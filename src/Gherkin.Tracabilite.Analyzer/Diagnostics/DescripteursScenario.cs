using Microsoft.CodeAnalysis;

namespace Gherkin.Tracabilite.Analyzer.Diagnostics;

/// <summary>Descripteurs des diagnostics de Scénario (spec §8, SCN003 à SCN009 et SCN012).</summary>
internal static class DescripteursScenario
{
    private const string Categorie = "Tracabilite";

    public static readonly DiagnosticDescriptor Scn003 = new(
        "SCN003",
        "Résultat attendu sans Clé",
        "Le Résultat attendu « {0} » n'a pas de Clé. Ajoutez un identifiant entre crochets après le mot-clé, par exemple : {1}[NomDeLaCle] {2}.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn004 = new(
        "SCN004",
        "Clé en double dans un Scénario",
        "La Clé [{0}] apparaît plusieurs fois dans le Scénario de '{1}'. Chaque Clé doit être unique : renommez celle-ci.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn005 = new(
        "SCN005",
        "Clé invalide",
        "La Clé [{0}] est invalide : {1}. Utilisez lettres, chiffres et _, sans commencer par un chiffre.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn006 = new(
        "SCN006",
        "Clé sur une étape qui n'est pas un Résultat attendu",
        "La Clé [{0}] est posée sur une étape {1} : seules les lignes ALORS/DONC/THEN et les ET/MAIS/AND/BUT qui les suivent portent une Clé. Retirez-la.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // RS1032 : une phrase unique terminée par un point. Message figé par la spec §8, suppression locale assumée.
#pragma warning disable RS1032
    public static readonly DiagnosticDescriptor Scn007 = new(
        "SCN007",
        "Structure du Scénario invalide",
        "Scénario de '{0}' mal structuré : {1}.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

#pragma warning restore RS1032

    public static readonly DiagnosticDescriptor Scn008 = new(
        "SCN008",
        "Ligne non reconnue dans un Scénario",
        "Ligne non reconnue dans le Scénario de '{0}' : « {1} ». Mots-clés attendus : {2}.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn009 = new(
        "SCN009",
        "Langue mélangée dans un Scénario",
        "Le Scénario de '{0}' est en {1} (d'après « {2} ») mais cette ligne est en {3} : « {4} ». Une seule langue par Scénario.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

#pragma warning disable RS1032
    public static readonly DiagnosticDescriptor Scn012 = new(
        "SCN012",
        "Scénario qui n'est pas un littéral",
        "Le Scénario de '{0}' doit être un littéral écrit dans l'attribut (raw string \"\"\"…\"\"\" recommandé), pas une constante ni une expression.",
        Categorie,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS1032
}
