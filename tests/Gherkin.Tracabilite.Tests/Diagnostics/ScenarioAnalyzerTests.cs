using Microsoft.CodeAnalysis;
using Xunit;
using static Gherkin.Tracabilite.Tests.Diagnostics.ScenarioAnalyzerHarnais;

namespace Gherkin.Tracabilite.Tests.Diagnostics;

// Spec §8 : SCN003 à SCN009 et SCN012, code, emplacement et message exact.
public class ScenarioAnalyzerTests
{
    [Fact]
    public async Task Scenario_valide_ne_rapporte_aucun_diagnostic()
    {
        var code = Source("""
            Exemple :
            Soit a
            Étant données b
            Sachant que c
            Quand d
            Donc[A] x
            """);

        var diagnostics = await AnalyserAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SCN003_resultat_attendu_sans_cle_est_signale_sur_sa_ligne()
    {
        var code = Source("""
            Scénario :
            Quand a
            Alors une erreur est levée
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN003"));

        Assert.Equal(
            "Le Résultat attendu « Alors une erreur est levée » n'a pas de Clé. "
            + "Ajoutez un identifiant entre crochets après le mot-clé, par exemple : Alors[NomDeLaCle] une erreur est levée.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Alors une erreur"), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task SCN004_cle_en_double_est_signalee_sur_la_seconde_occurrence()
    {
        var code = Source("""
            Scénario :
            Quand a
            Alors[A] x
            Donc[A] y
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN004"));

        Assert.Equal(
            "La Clé [A] apparaît plusieurs fois dans le Scénario de 'CreerTache'. "
            + "Chaque Clé doit être unique : renommez celle-ci.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Donc[A]"), PositionDiagnostic(diagnostic));
    }

    [Theory]
    [InlineData("1abc", "ce n'est pas un identifiant C# valide")]
    [InlineData("class", "c'est un mot réservé")]
    [InlineData("CreerTache_Cles", "elle porte le nom de la classe générée CreerTache_Cles")]
    public async Task SCN005_cle_invalide_est_signalee_avec_sa_cause(string cle, string cause)
    {
        var code = Source($"""
            Scénario :
            Quand a
            Alors[{cle}] x
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN005"));

        Assert.Equal(
            $"La Clé [{cle}] est invalide : {cause}. "
            + "Utilisez lettres, chiffres et _, sans commencer par un chiffre.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, $"Alors[{cle}]"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN006_cle_sur_une_etape_qui_n_est_pas_un_resultat_est_signalee()
    {
        var code = Source("""
            Scénario :
            Quand[A] a
            Alors[B] b
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN006"));

        Assert.Equal(
            "La Clé [A] est posée sur une étape Quand : seules les lignes ALORS/DONC/THEN "
            + "et les ET/MAIS/AND/BUT qui les suivent portent une Clé. Retirez-la.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Quand[A]"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN007_sans_ligne_Scenario_est_signale_sur_l_attribut()
    {
        var code = Source("""
            Quand a
            Alors[A] x
            """);

        // Le parser rapporte aussi « aucun ALORS » : il ignore les étapes avant une ligne Scénario (ticket 08).
        var diagnostic = Assert.Single(
            Filtrer(await AnalyserAsync(code), "SCN007"),
            d => d.GetMessage() == "Scénario de 'CreerTache' mal structuré : aucune ligne Scénario.");

        Assert.Equal(PositionDe(code, "[TestScenario").Ligne, PositionDiagnostic(diagnostic).Ligne);
        var debut = diagnostic.Location.SourceSpan.Start;
        Assert.Equal("TestScenario", code.Substring(debut, "TestScenario".Length));
    }

    [Fact]
    public async Task SCN007_sans_Alors_est_signale_sur_l_attribut()
    {
        var code = Source("""
            Scénario :
            Quand a
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN007"));

        Assert.Equal("Scénario de 'CreerTache' mal structuré : aucun ALORS.", diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "[TestScenario").Ligne, PositionDiagnostic(diagnostic).Ligne);
    }

    [Fact]
    public async Task SCN007_description_vide_apres_la_cle_est_signalee_sur_la_ligne()
    {
        var code = Source("""
            Scénario :
            Quand a
            Alors[A]
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN007"));

        Assert.Equal("Scénario de 'CreerTache' mal structuré : description vide après la Clé.", diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Alors[A]"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN007_et_en_premiere_etape_est_signale_sur_la_ligne()
    {
        var code = Source("""
            Scénario :
            Et b
            Alors[A] x
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN007"));

        Assert.Equal("Scénario de 'CreerTache' mal structuré : ET ou MAIS sans étape précédente.", diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Et b"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN008_ligne_non_reconnue_liste_les_mots_cles_de_la_langue()
    {
        var code = Source("""
            Scénario :
            Quand a
            Foo bar
            Alors[A] x
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN008"));

        Assert.Equal(
            "Ligne non reconnue dans le Scénario de 'CreerTache' : « Foo bar ». Mots-clés attendus : "
            + "Fonctionnalité :, Scénario :, Exemple :, En tant que, Je veux, Afin de, Soit, Étant donné, "
            + "Sachant que, Quand, Lorsque, Alors, Donc, Et, Mais.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Foo bar"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN009_ligne_dans_une_autre_langue_est_signalee()
    {
        var code = Source("""
            Scénario :
            Quand a
            When b
            Alors[A] x
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN009"));

        Assert.Equal(
            "Le Scénario de 'CreerTache' est en français (d'après « Quand ») mais cette ligne est en anglais : "
            + "« When b ». Une seule langue par Scénario.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "When b"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN009_ne_se_declenche_pas_pour_Scenario_sans_accent_suivi_d_etapes_francaises()
    {
        var code = Source("""
            Scenario :
            Soit a
            Étant donnée b
            Quand c
            Alors[A] x
            """);

        var diagnostics = await AnalyserAsync(code);

        Assert.Empty(Filtrer(diagnostics, "SCN009"));
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task But_en_narration_d_un_scenario_francais_est_du_texte_libre()
    {
        var code = Source("""
            Scénario :
            But de ce test
            Quand a
            Alors[A] x
            """);

        var diagnostics = await AnalyserAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SCN012_scenario_qui_n_est_pas_un_litteral_est_signale_sur_l_attribut()
    {
        var code = """"
            using Gherkin.Tracabilite.Runtime;

            public partial class TacheTests
            {
                private const string Scenario = """
                    Scénario :
                    Quand a
                    Alors[A] x
                    """;

                [TestScenario(Scenario)]
                public void CreerTache()
                {
                }
            }
            """";

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN012"));

        Assert.Equal(
            "Le Scénario de 'CreerTache' doit être un littéral écrit dans l'attribut "
            + "(raw string \"\"\"…\"\"\" recommandé), pas une constante ni une expression.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "[TestScenario(Scenario)]").Ligne, PositionDiagnostic(diagnostic).Ligne);
    }

    [Fact]
    public async Task SCN003_position_exacte_quand_les_triples_guillemets_sont_seuls_sur_leur_ligne()
    {
        var code = """"
            using Gherkin.Tracabilite.Runtime;

            public partial class TacheTests
            {
                [TestScenario(
                    """
                    Scénario :
                    Quand a
                    Alors une erreur est levée
                    """)]
                public void CreerTache()
                {
                }
            }
            """";

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN003"));

        Assert.Equal(PositionDe(code, "Alors une erreur"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task Diagnostic_rétrogradé_par_editorconfig_prend_la_sévérité_configurée()
    {
        var code = Source("""
            Scénario :
            Quand a
            Alors une erreur est levée
            """);

        var diagnostic = Assert.Single(Filtrer(
            await AnalyserAsync(code, new Dictionary<string, ReportDiagnostic> { ["SCN003"] = ReportDiagnostic.Warn }),
            "SCN003"));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task Code_genere_n_est_pas_analyse()
    {
        var code = "// <auto-generated/>\n" + Source("""
            Scénario :
            Quand a
            Alors une erreur est levée
            """);

        var diagnostics = await AnalyserAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Meme_code_non_genere_est_bien_signale()
    {
        var code = Source("""
            Scénario :
            Quand a
            Alors une erreur est levée
            """);

        var diagnostics = await AnalyserAsync(code);

        Assert.Single(Filtrer(diagnostics, "SCN003"));
    }
}
