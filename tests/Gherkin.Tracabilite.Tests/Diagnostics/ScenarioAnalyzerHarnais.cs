using System.Collections.Immutable;
using Gherkin.Tracabilite.Analyzer;
using Gherkin.Tracabilite.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gherkin.Tracabilite.Tests.Diagnostics;

/// <summary>Compilation en mémoire + analyse par <see cref="ScenarioAnalyzer"/>.</summary>
internal static class ScenarioAnalyzerHarnais
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(ChargerReferences);

    private static ImmutableArray<MetadataReference> ChargerReferences()
    {
        var plateforme = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var references = plateforme
            .Select(chemin => (MetadataReference)MetadataReference.CreateFromFile(chemin))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(TestScenarioAttribute).Assembly.Location));

        return references.ToImmutableArray();
    }

    /// <summary>
    /// Analyse le code et retourne les diagnostics de l'analyzer.
    /// <paramref name="severites"/> simule un .editorconfig (<c>dotnet_diagnostic.SCNxxx.severity</c>).
    /// </summary>
    internal static async Task<ImmutableArray<Diagnostic>> AnalyserAsync(
        string code,
        IReadOnlyDictionary<string, ReportDiagnostic>? severites = null,
        string chemin = "Test.cs")
    {
        var arbre = CSharpSyntaxTree.ParseText(code, path: chemin);

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        if (severites is not null)
        {
            options = options.WithSyntaxTreeOptionsProvider(new FournisseurSeverites(severites));
        }

        var compilation = CSharpCompilation.Create("Test", [arbre], References.Value, options);
        var avecAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ScenarioAnalyzer()));

        return await avecAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>Diagnostics d'un code donné, filtrés sur un identifiant.</summary>
    internal static ImmutableArray<Diagnostic> Filtrer(ImmutableArray<Diagnostic> diagnostics, string id) =>
        diagnostics.Where(d => d.Id == id).ToImmutableArray();

    /// <summary>
    /// Position (ligne et colonne, 0-based) du premier caractère non blanc de la ligne qui contient <paramref name="marqueur"/>.
    /// Le marqueur doit être unique dans le code.
    /// </summary>
    internal static (int Ligne, int Colonne) PositionDe(string code, string marqueur)
    {
        var lignes = code.Replace("\r\n", "\n").Split('\n');
        var index = Array.FindIndex(lignes, l => l.Contains(marqueur, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new InvalidOperationException($"Marqueur introuvable : {marqueur}");
        }

        var ligne = lignes[index];
        return (index, ligne.Length - ligne.TrimStart().Length);
    }

    /// <summary>Position de début d'un diagnostic, 0-based.</summary>
    internal static (int Ligne, int Colonne) PositionDiagnostic(Diagnostic diagnostic)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        return (position.Line, position.Character);
    }

    /// <summary>Ajoute 12 espaces à chaque ligne : indentation de la méthode de test.</summary>
    internal static string Indenter(string scenario) =>
        string.Join("\n", scenario.Replace("\r\n", "\n").Split('\n').Select(l => "            " + l));

    /// <summary>Classe de test minimale avec <c>[TestScenario("""…""")]</c>, les lignes du Scénario étant indentées.</summary>
    internal static string Source(string scenario, string methode = "CreerTache") =>
        "using Gherkin.Tracabilite.Runtime;\n\n"
        + "public partial class TacheTests\n{\n"
        + "    [TestScenario(\"\"\"\n"
        + Indenter(scenario) + "\n"
        + "            \"\"\")]\n"
        + "    public void " + methode + "()\n    {\n    }\n}\n";

    /// <summary>Fournisseur de sévérités : équivalent d'un .editorconfig pour les tests.</summary>
    private sealed class FournisseurSeverites : SyntaxTreeOptionsProvider
    {
        private readonly IReadOnlyDictionary<string, ReportDiagnostic> _severites;

        internal FournisseurSeverites(IReadOnlyDictionary<string, ReportDiagnostic> severites)
        {
            _severites = severites;
        }

        public override bool TryGetDiagnosticValue(
            SyntaxTree tree,
            string diagnosticId,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity) => TryGetGlobalDiagnosticValue(diagnosticId, cancellationToken, out severity);

        public override bool TryGetGlobalDiagnosticValue(
            string diagnosticId,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity) => _severites.TryGetValue(diagnosticId, out severity);

        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken) =>
            GeneratedKind.Unknown;
    }
}
