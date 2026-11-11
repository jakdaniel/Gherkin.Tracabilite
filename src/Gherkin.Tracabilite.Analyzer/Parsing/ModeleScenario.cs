namespace Gherkin.Tracabilite.Analyzer.Parsing;

internal enum Langue
{
    Francais,
    Anglais,
}

/// <summary>Rôle d'une ligne du texte d'un Scénario (spec §4).</summary>
internal enum Role
{
    /// <summary>Ligne vide, commentaire ou corps de docstring : ignorée.</summary>
    Ignoree,

    /// <summary>Ligne de tableau <c>| … |</c> rattachée à une étape : ignorée.</summary>
    Tableau,

    /// <summary>Ligne non reconnue (SCN008).</summary>
    Inconnue,

    Fonctionnalite,
    Scenario,
    Narration,

    /// <summary>Texte libre entre l'en-tête Scénario et la première étape.</summary>
    Texte,

    /// <summary>Soit, Étant donné, Sachant que, Given.</summary>
    Contexte,

    /// <summary>Quand, Lorsque, When.</summary>
    Action,

    /// <summary>Alors, Donc, Then.</summary>
    Resultat,

    /// <summary>Et, Mais, And, But.</summary>
    Prolongement,
}

/// <summary>Une ligne du texte. <see cref="Offset"/> est la position de son premier caractère dans le texte.</summary>
internal sealed class LigneScenario
{
    internal LigneScenario(int numero, int offset, string contenu)
    {
        Numero = numero;
        Offset = offset;
        Contenu = contenu;
    }

    /// <summary>Numéro de ligne, 1-based, dans le texte.</summary>
    public int Numero { get; }

    /// <summary>Index 0-based, dans le texte, du premier caractère de la ligne.</summary>
    public int Offset { get; }

    /// <summary>Contenu brut de la ligne, sans fin de ligne.</summary>
    public string Contenu { get; }

    public Role Role { get; internal set; } = Role.Ignoree;

    /// <summary>Mot-clé tel qu'écrit (ex. « Étant donnée que »), ou null pour une ligne sans mot-clé.</summary>
    public string? MotCle { get; internal set; }

    /// <summary>Langue du mot-clé, ou null si le mot-clé est ambigu (Scenario, But) ou absent.</summary>
    public Langue? LangueMot { get; internal set; }
}

/// <summary>Une étape (Contexte, Action, Résultat ou Prolongement) du Scénario.</summary>
internal sealed class Etape
{
    internal Etape(LigneScenario ligne, string? cle, bool cleValide, string description, bool estResultatAttendu)
    {
        Ligne = ligne;
        Cle = cle;
        CleValide = cleValide;
        Description = description;
        EstResultatAttendu = estResultatAttendu;
    }

    public LigneScenario Ligne { get; }

    /// <summary>Clé telle qu'écrite entre crochets, ou null si absente.</summary>
    public string? Cle { get; }

    /// <summary>Vrai si la Clé est un identifiant C# valide, non réservé, et différent de <c>{Methode}_Cles</c>.</summary>
    public bool CleValide { get; }

    /// <summary>Description après le mot-clé et la Clé, sans espaces de bord.</summary>
    public string Description { get; }

    /// <summary>Vrai pour un Résultat attendu : ligne Alors/Donc/Then, ou Et/Mais qui suit un tel résultat.</summary>
    public bool EstResultatAttendu { get; }
}

/// <summary>Modèle produit par le parser pour un texte de Scénario.</summary>
internal sealed class AnalyseScenario
{
    internal AnalyseScenario(
        IReadOnlyList<LigneScenario> lignes,
        IReadOnlyList<Etape> etapes,
        IReadOnlyList<ErreurScenario> erreurs,
        IReadOnlyList<string> clesValides,
        Langue? langueDetectee,
        string? motCleLangue)
    {
        Lignes = lignes;
        Etapes = etapes;
        Erreurs = erreurs;
        ClesValides = clesValides;
        LangueDetectee = langueDetectee;
        MotCleLangue = motCleLangue;

        var resultats = new List<Etape>();
        foreach (var etape in etapes)
        {
            if (etape.EstResultatAttendu)
            {
                resultats.Add(etape);
            }
        }

        ResultatsAttendus = resultats;
    }

    public IReadOnlyList<LigneScenario> Lignes { get; }

    public IReadOnlyList<Etape> Etapes { get; }

    /// <summary>Résultats attendus, dans l'ordre du texte, y compris ceux sans Clé ou à Clé invalide.</summary>
    public IReadOnlyList<Etape> ResultatsAttendus { get; }

    /// <summary>Clés valides des Résultats attendus, sans doublon, dans l'ordre d'apparition.</summary>
    public IReadOnlyList<string> ClesValides { get; }

    /// <summary>Erreurs de grammaire, dans l'ordre du texte ; les erreurs de structure sans ligne sont en fin de liste.</summary>
    public IReadOnlyList<ErreurScenario> Erreurs { get; }

    /// <summary>Langue détectée, ou null si aucun mot-clé non ambigu n'est présent.</summary>
    public Langue? LangueDetectee { get; }

    /// <summary>Premier mot-clé non ambigu ayant servi à détecter la langue, tel qu'écrit.</summary>
    public string? MotCleLangue { get; }
}
