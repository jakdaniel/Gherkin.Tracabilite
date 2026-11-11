using Gherkin.Tracabilite.Analyzer.Parsing;
using Xunit;

namespace Gherkin.Tracabilite.Tests.Parsing;

// Spec §4 : mots-clés, langue, lignes.
public class ScenarioParserMotsClesTests
{
    private static AnalyseScenario Analyser(string texte) => ScenarioParser.Analyser(texte, "Methode");

    private static IReadOnlyList<string> Codes(AnalyseScenario analyse) =>
        analyse.Erreurs.Select(e => e.Code).ToList();

    private static Role RoleDe(AnalyseScenario analyse, int numero) =>
        analyse.Lignes.Single(l => l.Numero == numero).Role;

    [Fact]
    public void Mots_cles_insensibles_a_la_casse()
    {
        var analyse = Analyser("SCÉNARIO :\nSOIT un contexte\nQUAND une action\nALORS[Cle] un résultat");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Langue.Francais, analyse.LangueDetectee);
    }

    [Fact]
    public void Mots_cles_insensibles_aux_accents()
    {
        var analyse = Analyser("Scenario :\nEtant donnee un contexte\nQuand une action\nAlors[Cle] un résultat");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Contexte, analyse.Lignes[1].Role);
    }

    [Fact]
    public void Espaces_multiples_reduits_a_un_dans_les_mots_cles()
    {
        var analyse = Analyser("Scénario :\nÉtant    donné   que un contexte\nAlors[Cle] un résultat");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Contexte, RoleDe(analyse, 2));
    }

    [Fact]
    public void Espace_facultatif_avant_deux_points()
    {
        var analyse = Analyser("Scénario:\nSoit un contexte\nAlors[Cle] un résultat");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Scenario, RoleDe(analyse, 1));
    }

    [Fact]
    public void Que_et_qu_apostrophe_sont_facultatifs()
    {
        var analyse = Analyser("Scénario :\nSoit un\nQuand a\nEt qu'il y ait\nEt que y\nEt z\nAlors[A] w");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Prolongement, RoleDe(analyse, 4));
        Assert.Equal(Role.Prolongement, RoleDe(analyse, 5));
        Assert.Equal(Role.Prolongement, RoleDe(analyse, 6));
    }

    [Fact]
    public void Lorsqu_apostrophe_est_une_action()
    {
        var analyse = Analyser("Scénario :\nLorsqu'un utilisateur agit\nAlors[Cle] un résultat");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Action, RoleDe(analyse, 2));
    }

    [Fact]
    public void Mot_entier_exemples_n_est_pas_exemple()
    {
        var analyse = Analyser("Exemples :\nSoit un\nAlors[Cle] x");

        Assert.Contains("SCN008", Codes(analyse));
        Assert.Equal(Role.Inconnue, RoleDe(analyse, 1));
    }

    [Fact]
    public void Exemple_est_accepte_comme_entete_de_scenario()
    {
        var analyse = Analyser("Exemple : Enregistrer\nSoit un\nAlors[Cle] x");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Scenario, RoleDe(analyse, 1));
    }

    [Fact]
    public void Scenario_et_but_ambigus_ne_servent_pas_a_la_detection()
    {
        var analyse = Analyser("Scenario :\nGiven un contexte\nThen[Cle] x");

        Assert.Equal(Langue.Anglais, analyse.LangueDetectee);
        Assert.Equal("Given", analyse.MotCleLangue);
        Assert.Empty(Codes(analyse));
    }

    [Fact]
    public void Langue_du_premier_mot_cle_non_ambigu()
    {
        var analyse = Analyser("Scénario :\nSoit un\nAlors[Cle] x");

        Assert.Equal(Langue.Francais, analyse.LangueDetectee);
        Assert.Equal("Soit", analyse.MotCleLangue);
    }

    [Fact]
    public void But_en_scenario_francais_est_du_texte_libre_avant_la_premiere_etape()
    {
        var analyse = Analyser("Scénario :\nBut de ce scénario\nSoit un\nAlors[Cle] x");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Texte, RoleDe(analyse, 2));
    }

    [Fact]
    public void But_en_scenario_francais_hors_zone_est_ligne_non_reconnue()
    {
        var analyse = Analyser("Scénario :\nSoit un\nBut de ce scénario\nAlors[Cle] x");

        Assert.Contains("SCN008", Codes(analyse));
    }

    [Fact]
    public void But_en_scenario_anglais_est_un_prolongement()
    {
        var analyse = Analyser("Scenario:\nGiven un\nThen[A] x\nBut[B] y\n");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Prolongement, RoleDe(analyse, 4));
        Assert.Contains(analyse.ResultatsAttendus, e => e.Ligne.Numero == 4);
    }

    [Fact]
    public void Narration_permise_avant_la_premiere_etape_et_ignoree()
    {
        var analyse = Analyser("Fonctionnalité : Traçabilité\nEn tant que dev\nJe veux tracer\nAfin de prouver\nScénario :\nSoit un\nAlors[Cle] x");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Narration, RoleDe(analyse, 2));
        Assert.Equal(Role.Narration, RoleDe(analyse, 3));
        Assert.Equal(Role.Narration, RoleDe(analyse, 4));
    }

    [Fact]
    public void Narration_apres_une_etape_est_ligne_non_reconnue()
    {
        var analyse = Analyser("Scénario :\nSoit un\nEn tant que dev\nAlors[Cle] x");

        Assert.Contains("SCN008", Codes(analyse));
    }

    [Fact]
    public void Commentaires_lignes_vides_et_indentation_ignores()
    {
        var analyse = Analyser("\n   # commentaire\n  Scénario :\n\n    Soit un\n  Alors[Cle] x\n");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Ignoree, RoleDe(analyse, 1));
        Assert.Equal(Role.Ignoree, RoleDe(analyse, 2));
        Assert.Equal(Role.Ignoree, RoleDe(analyse, 4));
    }

    [Fact]
    public void Tableaux_rattaches_a_une_etape_sont_ignores()
    {
        var analyse = Analyser("Scénario :\nSoit un\n| a | b |\n| 1 | 2 |\nAlors[Cle] x");

        Assert.Empty(Codes(analyse));
        Assert.Equal(Role.Tableau, RoleDe(analyse, 3));
    }

    [Fact]
    public void Docstring_est_en_erreur_SCN008()
    {
        var analyse = Analyser("Scénario :\nSoit un\n\"\"\"\nun bloc\n\"\"\"\nAlors[Cle] x");

        Assert.Single(analyse.Erreurs, e => e.Code == "SCN008" && e.Ligne!.Numero == 3);
        Assert.Single(analyse.Erreurs);
    }

    [Fact]
    public void Ligne_inconnue_est_en_erreur_SCN008_avec_mots_cles_attendus()
    {
        var analyse = Analyser("Scénario :\nSoit un\nblabla\nAlors[Cle] x");

        var erreur = Assert.IsType<LigneNonReconnue>(Assert.Single(analyse.Erreurs));
        Assert.Equal(3, erreur.Ligne!.Numero);
        Assert.Contains("Alors", erreur.MotsClesAttendus);
        Assert.DoesNotContain("Then", erreur.MotsClesAttendus);
    }

    [Fact]
    public void Langue_melangee_est_SCN009_une_seule_fois()
    {
        var analyse = Analyser("Scénario :\nSoit un\nGiven autre\nWhen autre\nAlors[Cle] x");

        var erreur = Assert.IsType<LanguesMelangees>(Assert.Single(analyse.Erreurs));
        Assert.Equal(3, erreur.Ligne!.Numero);
        Assert.Equal(Langue.Francais, erreur.LangueDetectee);
        Assert.Equal(Langue.Anglais, erreur.AutreLangue);
        Assert.Equal("Soit", erreur.MotCleLangue);
    }

    [Fact]
    public void Positions_relatives_au_texte()
    {
        var analyse = Analyser("ab\r\ncd");

        Assert.Equal(1, analyse.Lignes[0].Numero);
        Assert.Equal(0, analyse.Lignes[0].Offset);
        Assert.Equal("ab", analyse.Lignes[0].Contenu);
        Assert.Equal(2, analyse.Lignes[1].Numero);
        Assert.Equal(4, analyse.Lignes[1].Offset);
        Assert.Equal("cd", analyse.Lignes[1].Contenu);
    }
}
