using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer;
using Gherkin.Tracabilite.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using static Gherkin.Tracabilite.Tests.Diagnostics.ScenarioAnalyzerHarnais;

namespace Gherkin.Tracabilite.Tests.Diagnostics;

// Spec §6 et §8 : SCN001, SCN002, SCN010. Les Clés sont générées par ClesGenerator, comme dans un build réel.
public class VerificationsAnalyzerTests
{
    [Fact]
    public async Task SCN010_verify_dans_une_methode_sans_attribut_est_signale_sur_l_appel()
    {
        var code = """
            using Gherkin.Tracabilite.Runtime;

            public class TacheTests
            {
                public void Aide()
                {
                    ScenarioAssert.Verify("x", () => { });
                }
            }
            """;

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN010"));

        Assert.Equal(
            "ScenarioAssert.Verify est appelé dans 'Aide', qui n'a pas d'attribut [TestScenario]. "
            + "Ajoutez l'attribut ou retirez l'appel.",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "ScenarioAssert.Verify("), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task SCN001_resultat_attendu_sans_verification_est_signale_sur_sa_ligne()
    {
        var code = SourceAvecCorps("""
            Scénario :
            Quand a
            Alors[TacheEnregistree] la tâche est enregistrée
            """, "");

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN001"));

        Assert.Equal(
            "Le Résultat attendu [TacheEnregistree] (« la tâche est enregistrée ») du Scénario de 'TacheTests.CreerTache' "
            + "n'a aucune Vérification. "
            + "Ajoutez dans la méthode : ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => …);",
            diagnostic.GetMessage());
        Assert.Equal(PositionDe(code, "Alors[TacheEnregistree]"), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task Verification_nameof_et_constante_directe_sont_acceptees()
    {
        var code = SourceAvecCorps(ScenarioDeuxResultats, """
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });
            ScenarioAssert.Verify(CreerTache_Cles.Notification, () => { var x = 1; });
            """);

        Assert.Empty(await AnalyserVerificationsAsync(code));
    }

    [Fact]
    public async Task Plusieurs_verifications_d_une_meme_cle_sont_acceptees()
    {
        var code = SourceAvecCorps(ScenarioDeuxResultats, """
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });
            """);

        Assert.Empty(await AnalyserVerificationsAsync(code));
    }

    [Theory]
    [InlineData("if (true)\n{\n    ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });\n}")]
    [InlineData("foreach (var i in new[] { 1 })\n{\n    ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });\n}")]
    [InlineData("void Verifier()\n{\n    ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });\n}")]
    [InlineData("Action a = () => ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { var x = 1; });")]
    public async Task Verification_dans_if_boucle_fonction_locale_ou_lambda_est_comptee(string corps)
    {
        var code = SourceAvecCorps(ScenarioDeuxResultats, corps + "\nScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { var x = 1; });");

        Assert.Empty(await AnalyserVerificationsAsync(code));
    }

    [Fact]
    public async Task SCN002_chaine_litterale_est_signalee_sur_l_argument_avec_les_cles_disponibles()
    {
        var code = SourceAvecCorps(ScenarioDeuxResultats, """
            ScenarioAssert.Verify("TacheEnregistree", () => { });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN002"));

        Assert.Equal(
            "L'argument de Verify doit être une Clé du Scénario de 'CreerTache', écrite nameof(CreerTache_Cles.<Clé>). "
            + "Reçu : \"TacheEnregistree\" (chaîne littérale). Clés disponibles : TacheEnregistree, Notification.",
            diagnostic.GetMessage());
        Assert.Equal(PositionArgument(code, "ScenarioAssert.Verify(\"TacheEnregistree\""), PositionDiagnostic(diagnostic));
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Theory]
    [InlineData("const string Autre = \"Autre\";", "nameof(Autre)", "chaîne littérale")]
    [InlineData("var Autre = \"Autre\";", "Autre", "valeur non constante")]
    public async Task SCN002_argument_constant_ou_non_est_classe_selon_sa_constance(string declaration, string argument, string cause)
    {
        var corps = declaration + "\n"
            + $"ScenarioAssert.Verify({argument}, () => {{ }});\n"
            + "ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { });\n"
            + "ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { });";
        var code = SourceAvecCorps(ScenarioDeuxResultats, corps);

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN002"));

        Assert.Equal(
            "L'argument de Verify doit être une Clé du Scénario de 'CreerTache', écrite nameof(CreerTache_Cles.<Clé>). "
            + $"Reçu : {argument} ({cause}). Clés disponibles : TacheEnregistree, Notification.",
            diagnostic.GetMessage());
    }

    [Fact]
    public async Task SCN002_cle_d_un_autre_scenario_est_signalee()
    {
        var autre = """"
            public partial class AutreTests
            {
                [TestScenario("""
                    Scénario :
                    Quand a
                    Alors[Autre] x
                    """)]
                public void Autre()
                {
                }
            }
            """";
        var code = SourceAvecCorps(ScenarioDeuxResultats, """
            ScenarioAssert.Verify(nameof(AutreTests.Autre_Cles.Autre), () => { });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheEnregistree), () => { });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { });
            """) + "\n" + autre;

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN002"));

        Assert.Equal(
            "L'argument de Verify doit être une Clé du Scénario de 'CreerTache', écrite nameof(CreerTache_Cles.<Clé>). "
            + "Reçu : nameof(AutreTests.Autre_Cles.Autre) (Clé d'un autre Scénario). "
            + "Clés disponibles : TacheEnregistree, Notification.",
            diagnostic.GetMessage());
        Assert.Equal(PositionArgument(code, "ScenarioAssert.Verify(nameof(AutreTests"), PositionDiagnostic(diagnostic));
    }

    [Fact]
    public async Task SCN002_valeur_non_constante_est_signalee()
    {
        var code = SourceAvecCorps(ScenarioDeuxResultats, """
            string cle = "TacheEnregistree";
            ScenarioAssert.Verify(cle, () => { });
            ScenarioAssert.Verify(nameof(CreerTache_Cles.Notification), () => { });
            """);

        var diagnostic = Assert.Single(Filtrer(await AnalyserVerificationsAsync(code), "SCN002"));

        Assert.Equal(
            "L'argument de Verify doit être une Clé du Scénario de 'CreerTache', écrite nameof(CreerTache_Cles.<Clé>). "
            + "Reçu : cle (valeur non constante). Clés disponibles : TacheEnregistree, Notification.",
            diagnostic.GetMessage());
        Assert.Equal(PositionArgument(code, "ScenarioAssert.Verify(cle"), PositionDiagnostic(diagnostic));
    }

    /// <summary>Position du premier caractère de l'argument de l'appel repéré par <paramref name="marqueur"/>.</summary>
    private static (int Ligne, int Colonne) PositionArgument(string code, string marqueur)
    {
        var (ligne, colonne) = PositionDe(code, marqueur);
        return (ligne, colonne + "ScenarioAssert.Verify(".Length);
    }

    private const string ScenarioDeuxResultats = """
        Scénario :
        Quand a
        Alors[TacheEnregistree] la tâche est enregistrée
        Et[Notification] l'utilisateur est prévenu
        """;

    /// <summary>Classe de test dont le corps de <c>CreerTache</c> est <paramref name="corps"/> (lignes indentées de 8 espaces).</summary>
    private static string SourceAvecCorps(string scenario, string corps) =>
        "using Gherkin.Tracabilite.Runtime;\n\npublic partial class TacheTests\n{\n"
        + "    [TestScenario(\"\"\"\n"
        + Indenter(scenario) + "\n"
        + "            \"\"\")]\n"
        + "    public void CreerTache()\n    {\n"
        + string.Join("\n", corps.Replace("\r\n", "\n").Split('\n').Select(l => "        " + l)) + "\n"
        + "    }\n}\n";

    /// <summary>Compile avec le générateur de Clés, puis analyse par <see cref="VerificationsAnalyzer"/>.</summary>
    private static async Task<ImmutableArray<Diagnostic>> AnalyserVerificationsAsync(string code)
    {
        var generation = CompilationTestes.Generer(code);
        var avecAnalyzers = generation.Sortie.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new VerificationsAnalyzer()));
        return await avecAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
