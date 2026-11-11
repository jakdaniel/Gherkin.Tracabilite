using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer.Generation;
using Gherkin.Tracabilite.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gherkin.Tracabilite.Tests.Support;

/// <summary>Compilations en mémoire pour tester le générateur et l'analyzer.</summary>
internal static class CompilationTestes
{
    /// <summary>Méthode de test valide avec une seule Clé, sans erreur de compilation une fois le générateur appliqué.</summary>
    internal const string Methode = """
        [TestScenario("Scénario : Creer\nSoit une tâche\nALORS[TacheEnregistree] elle existe")]
        public void Creer()
        {
            ScenarioAssert.Verify(nameof(Creer_Cles.TacheEnregistree), () => { });
        }
        """;

    private static readonly ImmutableArray<MetadataReference> References = CreerReferences();

    internal static string Fichier(string contenu) =>
        "using Gherkin.Tracabilite.Runtime;\n\nnamespace Demo;\n\n" + contenu;

    /// <summary>Classe de test avec <see cref="Methode"/> ; <paramref name="entete"/> est la déclaration sans accolade.</summary>
    internal static string Classe(string entete) => entete + "\n{\n" + Methode + "\n}";

    internal static CSharpCompilation Creer(params string[] sources)
    {
        var arbres = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        return CSharpCompilation.Create(
            "Tests",
            arbres,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static ResultatGeneration Generer(params string[] sources)
    {
        var compilation = Creer(sources);
        var driver = CSharpGeneratorDriver.Create(new ClesGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var sortie, out var diagnostics);
        var run = driver.GetRunResult();
        var generes = run.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();
        return new ResultatGeneration(sortie, diagnostics, generes);
    }

    internal static async Task<ImmutableArray<Diagnostic>> AnalyserAsync(DiagnosticAnalyzer analyseur, params string[] sources)
    {
        var compilation = Creer(sources).WithAnalyzers(ImmutableArray.Create(analyseur));
        return await compilation.GetAnalyzerDiagnosticsAsync();
    }

    internal static IEnumerable<Diagnostic> Erreurs(Compilation compilation) =>
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

    internal static IEnumerable<Diagnostic> Avertissements(Compilation compilation) =>
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning);

    private static ImmutableArray<MetadataReference> CreerReferences()
    {
        var references = new List<MetadataReference>();
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (var chemin in tpa.Split(Path.PathSeparator))
        {
            if (chemin.Length > 0)
            {
                references.Add(MetadataReference.CreateFromFile(chemin));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(TestScenarioAttribute).Assembly.Location));
        return references.ToImmutableArray();
    }
}

internal sealed class ResultatGeneration
{
    internal ResultatGeneration(
        Compilation sortie,
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<GeneratedSourceResult> sources)
    {
        Sortie = sortie;
        Diagnostics = diagnostics;
        Sources = sources;
    }

    internal Compilation Sortie { get; }

    internal ImmutableArray<Diagnostic> Diagnostics { get; }

    internal ImmutableArray<GeneratedSourceResult> Sources { get; }
}
