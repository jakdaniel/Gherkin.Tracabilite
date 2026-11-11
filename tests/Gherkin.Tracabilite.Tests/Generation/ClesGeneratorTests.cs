using Gherkin.Tracabilite.Tests.Support;
using Xunit;

namespace Gherkin.Tracabilite.Tests.Generation;

public class ClesGeneratorTests
{
    [Fact]
    public void Nameof_des_Cles_compile_et_le_generateur_ne_rapporte_rien()
    {
        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests")));

        Assert.Empty(resultat.Diagnostics);
        Assert.Single(resultat.Sources);
        Assert.Equal("Demo.TacheTests.Creer.g.cs", resultat.Sources[0].HintName);
        Assert.Contains("public static class Creer_Cles", resultat.Sources[0].SourceText.ToString());
        Assert.Contains("public const string TacheEnregistree = \"TacheEnregistree\";", resultat.Sources[0].SourceText.ToString());
        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
    }

    [Fact]
    public void Deux_classes_de_meme_nom_dans_deux_namespaces_donnent_deux_fichiers()
    {
        var source = """
            using Gherkin.Tracabilite.Runtime;

            namespace A
            {
                public partial class TacheTests
                {
            """ + CompilationTestes.Methode + """

                }
            }

            namespace B
            {
                public partial class TacheTests
                {
            """ + CompilationTestes.Methode + """

                }
            }
            """;

        var resultat = CompilationTestes.Generer(source);

        Assert.Empty(resultat.Diagnostics);
        var noms = resultat.Sources.Select(s => s.HintName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "A.TacheTests.Creer.g.cs", "B.TacheTests.Creer.g.cs" }, noms);
        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
    }

    [Theory]
    [InlineData("public partial class TacheTests<T> where T : class", "Demo.TacheTests`1.Creer.g.cs")]
    [InlineData("public partial record TacheTests", "Demo.TacheTests.Creer.g.cs")]
    [InlineData("public partial struct TacheTests", "Demo.TacheTests.Creer.g.cs")]
    [InlineData("public partial record struct TacheTests", "Demo.TacheTests.Creer.g.cs")]
    public void Genres_de_type_sont_reproduits(string entete, string hintAttendu)
    {
        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe(entete)));

        Assert.Empty(resultat.Diagnostics);
        Assert.Single(resultat.Sources);
        Assert.Equal(hintAttendu, resultat.Sources[0].HintName);
        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
    }

    [Fact]
    public void Classe_imbriquee_dans_un_type_partial_est_reproduite()
    {
        var source = CompilationTestes.Fichier(
            "public partial class Externe\n{\n" + CompilationTestes.Classe("public partial class TacheTests") + "\n}");

        var resultat = CompilationTestes.Generer(source);

        Assert.Empty(resultat.Diagnostics);
        Assert.Single(resultat.Sources);
        Assert.Equal("Demo.Externe+TacheTests.Creer.g.cs", resultat.Sources[0].HintName);
        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
    }

    [Fact]
    public void Cles_valides_sont_emises_malgre_des_defauts_du_scenario()
    {
        var methode = """
            [TestScenario("Scénario : Creer\nALORS elle existe\nALORS[Ok] fine\nDONC[Ok] doublon\nALORS[1Mauvais] mal")]
            public void Creer()
            {
                ScenarioAssert.Verify(nameof(Creer_Cles.Ok), () => { });
            }
            """;

        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests").Replace(CompilationTestes.Methode, methode)));

        Assert.Single(resultat.Sources);
        var texte = resultat.Sources[0].SourceText.ToString();
        Assert.Contains("public const string Ok = \"Ok\";", texte);
        Assert.DoesNotContain("Mauvais", texte);
        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
    }

    [Fact]
    public void Cles_nommees_comme_un_membre_d_object_ne_produisent_aucun_avertissement()
    {
        var methode = """
            [TestScenario("Scénario : Creer\nALORS[Equals] egal\nALORS[ToString] texte\nALORS[GetHashCode] hash\nALORS[GetType] type")]
            public void Creer()
            {
                ScenarioAssert.Verify(nameof(Creer_Cles.Equals), () => { });
                ScenarioAssert.Verify(nameof(Creer_Cles.ToString), () => { });
                ScenarioAssert.Verify(nameof(Creer_Cles.GetHashCode), () => { });
                ScenarioAssert.Verify(nameof(Creer_Cles.GetType), () => { });
            }
            """;

        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests").Replace(CompilationTestes.Methode, methode)));

        Assert.Empty(CompilationTestes.Erreurs(resultat.Sortie));
        Assert.Empty(resultat.Sortie.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Fact]
    public void Rien_n_est_emis_si_la_classe_de_test_n_est_pas_partial()
    {
        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public class TacheTests")));

        Assert.Empty(resultat.Sources);
        Assert.Empty(resultat.Diagnostics);
    }

    [Fact]
    public void Rien_n_est_emis_si_un_type_englobant_n_est_pas_partial()
    {
        var source = CompilationTestes.Fichier(
            "public class Externe\n{\n" + CompilationTestes.Classe("public partial class TacheTests") + "\n}");

        var resultat = CompilationTestes.Generer(source);

        Assert.Empty(resultat.Sources);
        Assert.Empty(resultat.Diagnostics);
    }

    [Fact]
    public void Rien_n_est_emis_si_le_scenario_n_est_pas_un_litteral()
    {
        var methode = """
            private const string Texte = "Scénario : Creer\nALORS[TacheEnregistree] elle existe";

            [TestScenario(Texte)]
            public void Creer()
            {
            }
            """;

        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests").Replace(CompilationTestes.Methode, methode)));

        Assert.Empty(resultat.Sources);
        Assert.Empty(resultat.Diagnostics);
    }

    [Fact]
    public void Rien_n_est_emis_pour_des_methodes_surchargees()
    {
        var methodes = """
            [TestScenario("Scénario : Creer\nALORS[TacheEnregistree] elle existe")]
            public void Creer()
            {
            }

            [TestScenario("Scénario : Creer\nALORS[TacheEnregistree] elle existe")]
            public void Creer(int n)
            {
            }
            """;

        var resultat = CompilationTestes.Generer(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests").Replace(CompilationTestes.Methode, methodes)));

        Assert.Empty(resultat.Sources);
        Assert.Empty(resultat.Diagnostics);
    }
}
