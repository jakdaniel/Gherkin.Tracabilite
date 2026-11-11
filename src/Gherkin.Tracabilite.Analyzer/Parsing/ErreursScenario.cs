namespace Gherkin.Tracabilite.Analyzer.Parsing;

/// <summary>Erreur de grammaire du texte d'un Scénario (SCN003 à SCN009). Aucun diagnostic n'est rapporté par le parser lui-même.</summary>
internal abstract class ErreurScenario
{
    protected ErreurScenario(LigneScenario? ligne)
    {
        Ligne = ligne;
    }

    public abstract string Code { get; }

    /// <summary>Ligne fautive, ou null pour une erreur qui porte sur le Scénario entier.</summary>
    public LigneScenario? Ligne { get; }
}

/// <summary>SCN003 : Résultat attendu sans Clé.</summary>
internal sealed class ResultatSansCle : ErreurScenario
{
    internal ResultatSansCle(LigneScenario ligne) : base(ligne)
    {
    }

    public override string Code => "SCN003";
}

/// <summary>SCN004 : Clé déjà utilisée par un Résultat attendu précédent du même Scénario.</summary>
internal sealed class CleEnDouble : ErreurScenario
{
    internal CleEnDouble(LigneScenario ligne, string cle) : base(ligne)
    {
        Cle = cle;
    }

    public override string Code => "SCN004";

    public string Cle { get; }
}

/// <summary>Causes possibles d'une Clé invalide (SCN005).</summary>
internal enum CauseCleInvalide
{
    NonIdentifiant,
    MotReserve,
    NomClasseGeneree,
}

/// <summary>SCN005 : Clé invalide.</summary>
internal sealed class CleInvalide : ErreurScenario
{
    internal CleInvalide(LigneScenario ligne, string cle, CauseCleInvalide cause) : base(ligne)
    {
        Cle = cle;
        Cause = cause;
    }

    public override string Code => "SCN005";

    public string Cle { get; }

    public CauseCleInvalide Cause { get; }
}

/// <summary>SCN006 : Clé posée sur une étape qui n'est pas un Résultat attendu.</summary>
internal sealed class CleSurMauvaiseEtape : ErreurScenario
{
    internal CleSurMauvaiseEtape(LigneScenario ligne, string cle) : base(ligne)
    {
        Cle = cle;
    }

    public override string Code => "SCN006";

    public string Cle { get; }
}

/// <summary>Causes possibles d'une erreur de structure (SCN007).</summary>
internal enum CauseStructure
{
    AucuneLigneScenario,
    PlusieursLignesScenario,
    AucunAlors,
    EtOuMaisSansEtapePrecedente,
    DescriptionVideApresCle,
}

/// <summary>SCN007 : structure du Scénario invalide.</summary>
internal sealed class StructureInvalide : ErreurScenario
{
    internal StructureInvalide(LigneScenario? ligne, CauseStructure cause) : base(ligne)
    {
        Cause = cause;
    }

    public override string Code => "SCN007";

    public CauseStructure Cause { get; }
}

/// <summary>SCN008 : ligne non reconnue (y compris docstring).</summary>
internal sealed class LigneNonReconnue : ErreurScenario
{
    internal LigneNonReconnue(LigneScenario ligne, Langue? langue) : base(ligne)
    {
        MotsClesAttendus = MotsCles.Attendus(langue);
    }

    public override string Code => "SCN008";

    /// <summary>Mots-clés attendus pour la langue détectée (les deux langues si elle est inconnue).</summary>
    public IReadOnlyList<string> MotsClesAttendus { get; }
}

/// <summary>SCN009 : ligne dans une autre langue que celle du Scénario.</summary>
internal sealed class LanguesMelangees : ErreurScenario
{
    internal LanguesMelangees(LigneScenario ligne, Langue langueDetectee, string motCleLangue, Langue autreLangue)
        : base(ligne)
    {
        LangueDetectee = langueDetectee;
        MotCleLangue = motCleLangue;
        AutreLangue = autreLangue;
    }

    public override string Code => "SCN009";

    public Langue LangueDetectee { get; }

    /// <summary>Premier mot-clé non ambigu ayant déterminé la langue, tel qu'écrit.</summary>
    public string MotCleLangue { get; }

    /// <summary>Langue de la ligne fautive.</summary>
    public Langue AutreLangue { get; }
}
