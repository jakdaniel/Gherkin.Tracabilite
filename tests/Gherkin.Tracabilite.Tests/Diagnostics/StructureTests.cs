using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer;
using Gherkin.Tracabilite.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Gherkin.Tracabilite.Tests.Diagnostics;

public class StructureTests
{
    [Fact]
    public async Task SCN011_classe_de_test_non_partial_pointe_l_attribut_avec_le_message_exact()
    {
        var diagnostics = await Analyser(CompilationTestes.Fichier(CompilationTestes.Classe("public class TacheTests")));

        var scn011 = Assert.Single(diagnostics.Where(d => d.Id == "SCN011"));
        Assert.Equal(
            "Le type 'TacheTests' doit être déclaré partial pour recevoir les Clés générées du Scénario de 'TacheTests.Creer'.",
            scn011.GetMessage());
        Assert.StartsWith("TestScenario(", TexteDe(scn011));
    }

    [Fact]
    public async Task SCN011_designe_le_premier_type_non_partial_depuis_l_exterieur_avec_la_precision()
    {
        var source = CompilationTestes.Fichier(
            "public class Externe\n{\n" + CompilationTestes.Classe("public partial class TacheTests") + "\n}");

        var diagnostics = await Analyser(source);

        var scn011 = Assert.Single(diagnostics.Where(d => d.Id == "SCN011"));
        Assert.Equal(
            "Le type 'Externe' doit être déclaré partial pour recevoir les Clés générées du Scénario de 'TacheTests.Creer' (il contient la classe de test).",
            scn011.GetMessage());
    }

    [Fact]
    public async Task SCN011_ne_designe_pas_un_intermediaire_quand_l_exterieur_est_partial()
    {
        var source = CompilationTestes.Fichier(
            "public partial class Externe\n{\n    public class Milieu\n    {\n" + CompilationTestes.Classe("public partial class TacheTests") + "\n    }\n}");

        var diagnostics = await Analyser(source);

        var scn011 = Assert.Single(diagnostics.Where(d => d.Id == "SCN011"));
        Assert.Equal(
            "Le type 'Milieu' doit être déclaré partial pour recevoir les Clés générées du Scénario de 'TacheTests.Creer' (il contient la classe de test).",
            scn011.GetMessage());
    }

    [Fact]
    public async Task SCN011_absent_quand_toute_la_chaine_est_partial()
    {
        var source = CompilationTestes.Fichier(
            "public partial class Externe\n{\n" + CompilationTestes.Classe("public partial class TacheTests") + "\n}");

        var diagnostics = await Analyser(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SCN011");
    }

    [Fact]
    public async Task SCN013_signale_chaque_surcharge_sur_son_attribut()
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
        var source = CompilationTestes.Fichier(
            CompilationTestes.Classe("public partial class TacheTests").Replace(CompilationTestes.Methode, methodes));

        var diagnostics = await Analyser(source);

        var scn013 = diagnostics.Where(d => d.Id == "SCN013").ToArray();
        Assert.Equal(2, scn013.Length);
        foreach (var d in scn013)
        {
            Assert.Equal(
                "Plusieurs méthodes 'Creer' portent [TestScenario] dans 'TacheTests'. Donnez à chacune un nom unique.",
                d.GetMessage());
            Assert.StartsWith("TestScenario(", TexteDe(d));
        }
    }

    [Fact]
    public async Task Aucun_diagnostic_structure_pour_une_classe_valide()
    {
        var diagnostics = await Analyser(CompilationTestes.Fichier(CompilationTestes.Classe("public partial class TacheTests")));

        Assert.Empty(diagnostics.Where(d => d.Id is "SCN011" or "SCN013"));
    }

    private static Task<ImmutableArray<Diagnostic>> Analyser(string source) =>
        CompilationTestes.AnalyserAsync(new StructureAnalyzer(), source);

    private static string TexteDe(Diagnostic diagnostic)
    {
        var arbre = diagnostic.Location.SourceTree!;
        return arbre.GetText().ToString(diagnostic.Location.SourceSpan);
    }
}
