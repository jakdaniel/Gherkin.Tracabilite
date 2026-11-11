using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer;
using Gherkin.Tracabilite.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using static Gherkin.Tracabilite.Tests.Diagnostics.ScenarioAnalyzerHarnais;

namespace Gherkin.Tracabilite.Tests.Diagnostics;

// Spec §6 et §8 : SCN014, SCN015, SCN016. Une Vérification fautive ne compte pas pour SCN001.
public class AssertionsAnalyzerTests
{
    private const string DescriptionTache = "la tâche est enregistrée";

    [Fact]
    public async Task SCN014_lambda_async_passe_a_verify_est_signale_sur_le_lambda()
    {
        var code = SourceAvecCorps("""
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN014"));

        Assert.Equal(
            "La Vérification de [TacheEnregistree] passe un lambda async à Verify : il devient async void et son échec ne serait pas détecté. "
            + "Écrivez : await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => …);",
            diagnostic.GetMessage());
        Assert.Equal(PositionExacte(code, "async () => { var x = 1; });"), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Theory]
    [InlineData("async () => { var x = 1; }")]
    [InlineData("async delegate { var x = 1; }")]
    public async Task SCN014_toute_fonction_anonyme_async_passee_a_verify_est_signalee(string lambda)
    {
        var code = SourceAvecCorps(
            $"ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), {lambda});\n"
            + "ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });");

        Assert.Single(Filtrer(await AnalyserAsync(code), "SCN014"));
    }

    [Fact]
    public async Task SCN014_cle_invalide_remplace_cle_par_le_texte_de_l_argument()
    {
        var code = SourceAvecCorps("""
            ScenarioAssert.Verify("TacheEnregistree", async () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN014"));

        Assert.Equal(
            "La Vérification de [\"TacheEnregistree\"] passe un lambda async à Verify : il devient async void et son échec ne serait pas détecté. "
            + "Écrivez : await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.\"TacheEnregistree\"), async () => …);",
            diagnostic.GetMessage());
        Assert.Equal(PositionExacte(code, "async () => { var x = 1; });"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN014_non_declenche_pour_verify_async_avec_lambda_async()
    {
        var code = SourceAvecCorps("""
            await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });
            await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.Notification), async () => { var x = 1; });
            """);

        Assert.Empty(Filtrer(await AnalyserAsync(code), "SCN014"));
    }

    [Fact]
    public async Task SCN014_hors_methode_de_scenario_n_est_pas_signale()
    {
        var code = """
            using System.Threading.Tasks;
            using Gherkin.Tracabilite.Runtime;

            public class TacheTests
            {
                public async Task Aide()
                {
                    ScenarioAssert.Verify("x", async () => { var x = 1; });
                }
            }
            """;

        var diagnostics = await AnalyserAsync(code);

        Assert.Empty(Filtrer(diagnostics, "SCN014"));
        Assert.Single(Filtrer(diagnostics, "SCN010"));
    }

    [Fact]
    public async Task SCN015_verify_async_en_instruction_expression_est_signale_sur_l_appel()
    {
        var code = SourceAvecCorps("""
            ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN015"));

        Assert.Equal(
            "La Vérification de [TacheEnregistree] appelle VerifyAsync sans utiliser la tâche retournée : "
            + "l'assertion ne serait pas attendue. Ajoutez await devant ScenarioAssert.VerifyAsync(…).",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree)"), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task SCN015_verify_async_affecte_a_discard_est_signale()
    {
        var code = SourceAvecCorps("""
            _ = ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN015"));

        Assert.Equal(PositionExacte(code, "ScenarioAssert.VerifyAsync"), PositionDiagnostic(diagnostic));
    }

    [Theory]
    [InlineData("await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });")]
    [InlineData("var tache = ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });")]
    [InlineData("await Task.WhenAll(ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; }));")]
    public async Task SCN015_utilisations_acceptees_de_la_tache(string corps)
    {
        var code = SourceAvecCorps(corps + "\nScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });");

        Assert.Empty(Filtrer(await AnalyserAsync(code), "SCN015"));
    }

    [Fact]
    public async Task SCN015_return_de_la_tache_est_acceptee()
    {
        var code = SourceAvecCorps(
            "return ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });",
            signature: "public Task CreerTache()");

        Assert.Empty(Filtrer(await AnalyserAsync(code), "SCN015"));
    }

    [Fact]
    public async Task SCN015_cle_invalide_remplace_cle_par_le_texte_de_l_argument()
    {
        var code = SourceAvecCorps("""
            ScenarioAssert.VerifyAsync("TacheEnregistree", async () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN015"));

        Assert.Equal(
            "La Vérification de [\"TacheEnregistree\"] appelle VerifyAsync sans utiliser la tâche retournée : "
            + "l'assertion ne serait pas attendue. Ajoutez await devant ScenarioAssert.VerifyAsync(…).",
            diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { });", "() => { });")]
    [InlineData("ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), async () => { });", "async () => { });")]
    [InlineData("await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { });", "async () => { });")]
    [InlineData("await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), () => Task.CompletedTask);", "() => Task.CompletedTask);")]
    public async Task SCN016_assertion_vide_est_signalee_sur_le_lambda(string verification, string marqueur)
    {
        var code = SourceAvecCorps(verification + "\nScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });");

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN016"));

        Assert.Equal(
            "La Vérification de [TacheEnregistree] a une assertion vide : elle ne vérifie rien. "
            + "Écrivez dans le lambda l'assertion qui prouve le Résultat attendu « " + DescriptionTache + " ».",
            diagnostic.GetMessage());
        Assert.Equal(PositionExacte(code, marqueur), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task SCN016_lambda_avec_corps_n_est_pas_signale()
    {
        var code = SourceAvecCorps("""
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });
            await ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.Notification), () => Task.FromResult(1));
            """);

        Assert.Empty(Filtrer(await AnalyserAsync(code), "SCN016"));
    }

    [Theory]
    [InlineData("ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });")]
    [InlineData("ScenarioAssert.VerifyAsync(nameof(CreerTache_Cles.TacheEnregistree), async () => { var x = 1; });")]
    [InlineData("ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { });")]
    public async Task Verification_fautive_ne_compte_pas_pour_SCN001(string verification)
    {
        var code = SourceAvecCorps(verification + "\nScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });");

        var diagnostic = Assert.Single(Filtrer(await AnalyserAsync(code), "SCN001"));

        Assert.Equal(
            "Le Résultat attendu [TacheEnregistree] (« la tâche est enregistrée ») du Scénario de 'TacheTests.CreerTache' "
            + "n'a aucune Vérification. "
            + "Ajoutez dans la méthode : ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => …);",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Alors[TacheEnregistree]"), PositionDiagnostic(diagnostic));
    }

    private const string ScenarioDeuxResultats = """
        Scénario :
        Quand a
        Alors[TacheEnregistree] la tâche est enregistrée
        Et[Notification] l'utilisateur est prévenu
        """;

    /// <summary>Classe de test dont la méthode <c>CreerTache</c> porte <paramref name="corps"/> (lignes indentées de 8 espaces).</summary>
    private static string SourceAvecCorps(string corps, string signature = "public async Task CreerTache()") =>
        "using System.Threading.Tasks;\nusing Gherkin.Tracabilite.Runtime;\n\npublic partial class TacheTests\n{\n"
        + "    [TestScenario(\"\"\"\n"
        + Indenter(ScenarioDeuxResultats) + "\n"
        + "            \"\"\")]\n"
        + "    " + signature + "\n    {\n"
        + string.Join("\n", corps.Replace("\r\n", "\n").Split('\n').Select(l => "        " + l)) + "\n"
        + "    }\n}\n";

    /// <summary>Compile avec le générateur de Clés, puis analyse par les analyzers de Vérification (SCN014 à SCN016 et SCN001).</summary>
    private static async Task<ImmutableArray<Diagnostic>> AnalyserAsync(string code)
    {
        var generation = CompilationTestes.Generer(code);
        var analyseurs = ImmutableArray.Create<DiagnosticAnalyzer>(new AssertionsAnalyzer(), new VerificationsAnalyzer());
        return await generation.Sortie.WithAnalyzers(analyseurs).GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>Position (ligne et colonne, 0-based) du premier caractère de <paramref name="marqueur"/>, qui doit être unique dans le code.</summary>
    private static (int Ligne, int Colonne) PositionExacte(string code, string marqueur)
    {
        var index = code.IndexOf(marqueur, StringComparison.Ordinal);
        if (index < 0 || code.IndexOf(marqueur, index + 1, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException($"Marqueur absent ou non unique : {marqueur}");
        }

        var ligne = code.Substring(0, index).Count(c => c == '\n');
        var debutLigne = code.LastIndexOf('\n', index) + 1;
        return (ligne, index - debutLigne);
    }
}
