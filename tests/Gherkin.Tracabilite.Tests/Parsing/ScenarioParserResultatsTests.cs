using Gherkin.Tracabilite.Analyzer.Parsing;
using Xunit;

namespace Gherkin.Tracabilite.Tests.Parsing;

// Spec §4 : Résultats attendus, Clés, structure.
public class ScenarioParserResultatsTests
{
    private static AnalyseScenario Analyser(string texte, string methode = "Methode") =>
        ScenarioParser.Analyser(texte, methode);

    private static IReadOnlyList<string> Codes(AnalyseScenario analyse) =>
        analyse.Erreurs.Select(e => e.Code).ToList();

    [Fact]
    public void Alors_donc_et_then_sont_des_resultats_attendus()
    {
        var fr = Analyser("Scénario :\nQuand a\nAlors[A] x\nDonc[B] y");
        var en = Analyser("Scenario:\nWhen a\nThen[C] z");

        Assert.Equal(new[] { "A", "B" }, fr.ClesValides);
        Assert.Equal(new[] { "C" }, en.ClesValides);
        Assert.Empty(Codes(fr));
        Assert.Empty(Codes(en));
    }

    [Fact]
    public void Et_et_mais_qui_suivent_un_alors_sont_des_resultats()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[A] x\nEt[B] y\nMais[C] z\nQuand b\nEt c");

        Assert.Equal(new[] { "A", "B", "C" }, analyse.ClesValides);
        Assert.Empty(Codes(analyse));
        Assert.False(analyse.Etapes[5].EstResultatAttendu);
    }

    [Fact]
    public void Et_apres_une_action_n_est_pas_un_resultat_et_sa_cle_est_SCN006()
    {
        var analyse = Analyser("Scénario :\nQuand a\nEt[B] b\nAlors[A] x");

        Assert.Equal(new[] { "A" }, analyse.ClesValides);
        var erreur = Assert.IsType<CleSurMauvaiseEtape>(Assert.Single(analyse.Erreurs));
        Assert.Equal("SCN006", erreur.Code);
        Assert.Equal(3, erreur.Ligne!.Numero);
    }

    [Fact]
    public void Plusieurs_blocs_lorsque_alors_sont_permis()
    {
        var analyse = Analyser("Scénario :\nSoit un\nQuand a\nAlors[A] x\nQuand b\nAlors[B] y");

        Assert.Empty(Codes(analyse));
        Assert.Equal(new[] { "A", "B" }, analyse.ClesValides);
    }

    [Fact]
    public void Cle_collee_ou_espacee_au_mot_cle_est_reconnue()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Collee] x\nAlors [Espacee] y");

        Assert.Empty(Codes(analyse));
        Assert.Equal(new[] { "Collee", "Espacee" }, analyse.ClesValides);
        Assert.Equal("x", analyse.ResultatsAttendus[0].Description);
    }

    [Fact]
    public void Cle_identifiant_unicode_est_valide()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[TâcheEnregistrée_2] x");

        Assert.Empty(Codes(analyse));
        Assert.Equal(new[] { "TâcheEnregistrée_2" }, analyse.ClesValides);
    }

    [Fact]
    public void Cle_mot_contextuel_est_permise()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[var] x\nAlors[record] y");

        Assert.Empty(Codes(analyse));
        Assert.Equal(new[] { "var", "record" }, analyse.ClesValides);
    }

    [Fact]
    public void Cle_mot_reserve_est_SCN005_cause_mot_reserve()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[class] x");

        var erreur = Assert.IsType<CleInvalide>(Assert.Single(analyse.Erreurs));
        Assert.Equal("SCN005", erreur.Code);
        Assert.Equal(CauseCleInvalide.MotReserve, erreur.Cause);
        Assert.Empty(analyse.ClesValides);
    }

    [Fact]
    public void Cle_non_identifiant_est_SCN005_cause_non_identifiant()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[@x] y\nAlors[1abc] z");

        Assert.Equal(2, analyse.Erreurs.Count);
        Assert.All(analyse.Erreurs, e => Assert.Equal("SCN005", e.Code));
        Assert.All(analyse.Erreurs, e => Assert.Equal(CauseCleInvalide.NonIdentifiant, ((CleInvalide)e).Cause));
    }

    [Fact]
    public void Cle_nom_de_classe_generee_est_SCN005()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Methode_Cles] x", "Methode");

        var erreur = Assert.IsType<CleInvalide>(Assert.Single(analyse.Erreurs));
        Assert.Equal(CauseCleInvalide.NomClasseGeneree, erreur.Cause);
    }

    [Fact]
    public void Cle_sensible_a_la_casse()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Cle] x\nAlors[cle] y");

        Assert.Empty(Codes(analyse));
        Assert.Equal(new[] { "Cle", "cle" }, analyse.ClesValides);
    }

    [Fact]
    public void Cle_en_double_est_SCN004_et_absente_des_cles_en_double()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Cle] x\nAlors[Cle] y");

        var erreur = Assert.IsType<CleEnDouble>(Assert.Single(analyse.Erreurs));
        Assert.Equal("SCN004", erreur.Code);
        Assert.Equal(4, erreur.Ligne!.Numero);
        Assert.Equal(new[] { "Cle" }, analyse.ClesValides);
    }

    [Fact]
    public void Resultat_sans_cle_est_SCN003()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors il y a un effet");

        var erreur = Assert.IsType<ResultatSansCle>(Assert.Single(analyse.Erreurs));
        Assert.Equal("SCN003", erreur.Code);
        Assert.Equal("Alors il y a un effet", erreur.Ligne!.Contenu);
        Assert.Single(analyse.ResultatsAttendus);
        Assert.Null(analyse.ResultatsAttendus[0].Cle);
    }

    [Fact]
    public void Description_vide_apres_la_cle_est_SCN007()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Cle]");

        var erreur = Assert.IsType<StructureInvalide>(Assert.Single(analyse.Erreurs));
        Assert.Equal("SCN007", erreur.Code);
        Assert.Equal(CauseStructure.DescriptionVideApresCle, erreur.Cause);
        Assert.Equal(new[] { "Cle" }, analyse.ClesValides);
    }

    [Fact]
    public void Cles_valides_sont_conservees_malgre_les_autres_erreurs()
    {
        var analyse = Analyser("Scénario :\nQuand a\nAlors[Bonne] x\nAlors pas de cle\nAlors[class] y");

        Assert.Equal(new[] { "Bonne" }, analyse.ClesValides);
        Assert.Equal(2, analyse.Erreurs.Count);
    }

    [Fact]
    public void Structure_aucune_ligne_scenario_est_SCN007_sans_ligne()
    {
        var analyse = Analyser("Soit un\nAlors[Cle] x");

        Assert.Contains(analyse.Erreurs, e => e is StructureInvalide s
            && s.Cause == CauseStructure.AucuneLigneScenario && s.Ligne == null);
    }

    [Fact]
    public void Structure_plusieurs_lignes_scenario_est_SCN007_sur_la_seconde()
    {
        var analyse = Analyser("Scénario :\nSoit un\nAlors[A] x\nScénario : deux\nAlors[B] y");

        var erreur = Assert.IsType<StructureInvalide>(Assert.Single(analyse.Erreurs));
        Assert.Equal(CauseStructure.PlusieursLignesScenario, erreur.Cause);
        Assert.Equal(4, erreur.Ligne!.Numero);
    }

    [Fact]
    public void Structure_aucun_alors_est_SCN007()
    {
        var analyse = Analyser("Scénario :\nSoit un\nQuand a");

        Assert.Contains(analyse.Erreurs, e => e is StructureInvalide s
            && s.Cause == CauseStructure.AucunAlors && s.Ligne == null);
    }

    [Fact]
    public void Alors_avant_l_entete_scenario_ne_rapporte_pas_aucun_alors()
    {
        var analyse = Analyser("Alors[Cle] x");

        Assert.DoesNotContain(analyse.Erreurs, e => e is StructureInvalide s && s.Cause == CauseStructure.AucunAlors);
        Assert.Contains(analyse.Erreurs, e => e is StructureInvalide s
            && s.Cause == CauseStructure.AucuneLigneScenario && s.Ligne == null);
        Assert.Contains(analyse.Erreurs, e => e is LigneNonReconnue);
    }

    // Roslyn refuse les caractères hors BMP dans un identifiant (CS1056) : ces Clés sont SCN005, même si la catégorie Unicode est une lettre.
    [Theory]
    [InlineData("Clé", true)]
    [InlineData("a\U0001D465", false)]
    [InlineData("\U0001D465a", false)]
    [InlineData("a\U0001D7D9", false)]
    [InlineData("a\uD835", false)]
    public void Cles_avec_caracteres_hors_BMP_sont_SCN005(string cle, bool valide)
    {
        var analyse = Analyser($"Scénario :\nQuand a\nAlors[{cle}] x");

        Assert.Equal(valide ? new[] { cle } : Array.Empty<string>(), analyse.ClesValides);
        Assert.Equal(valide ? Array.Empty<string>() : new[] { "SCN005" }, Codes(analyse).ToArray());
    }

    [Fact]
    public void Structure_et_en_premiere_etape_est_SCN007()
    {
        var analyse = Analyser("Scénario :\nEt un\nAlors[A] x");

        var erreur = Assert.IsType<StructureInvalide>(Assert.Single(analyse.Erreurs));
        Assert.Equal(CauseStructure.EtOuMaisSansEtapePrecedente, erreur.Cause);
        Assert.Equal(2, erreur.Ligne!.Numero);
    }

    [Fact]
    public void Feature_est_facultative()
    {
        var analyse = Analyser("Scénario :\nSoit un\nAlors[Cle] x");

        Assert.Empty(Codes(analyse));
    }

    [Fact]
    public void Scenario_anglais_complet_sans_erreur()
    {
        var analyse = Analyser("Feature: Traçabilité\nScenario: Enregistrer\nGiven a task\nWhen I save\nThen[Saved] it is stored\nAnd[Listed] it is listed\n");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Langue.Anglais, analyse.LangueDetectee);
        Assert.Equal(new[] { "Saved", "Listed" }, analyse.ClesValides);
    }
}
