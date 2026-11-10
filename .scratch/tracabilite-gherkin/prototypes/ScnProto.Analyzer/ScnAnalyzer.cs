using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ScnProto.Analyzer;

// Prototype jetable. SCN001 = règle "réelle" (error par défaut). SCN002, SCN901, SCN902, SCN903 = sondes.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScnAnalyzer : DiagnosticAnalyzer
{
    // Variante de contrôle pour la question 4 : GeneratedCodeAnalysisFlags.None (demandé) ou Analyze.
    private const GeneratedCodeAnalysisFlags GeneratedFlag = GeneratedCodeAnalysisFlags.None;

    public static readonly DiagnosticDescriptor Scn001 = new(
        "SCN001", "Résultat attendu sans Clé",
        "Résultat attendu sans Clé : « {0} » (ajouter [Clé] après ALORS)",
        "ScnProto", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn002 = new(
        "SCN002", "Sonde position ALORS[Clé]",
        "SONDE ALORS[{0}] : ligne {1}, colonne {2}, fichier {3}",
        "ScnProto", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn901 = new(
        "SCN901", "Sonde nameof",
        "SONDE nameof({0}) : symbole={1}, kind={2}, reason={3}, constante={4}, declare dans={5}",
        "ScnProto", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn902 = new(
        "SCN902", "Sonde type",
        "SONDE type {0} : declaration dans {1}",
        "ScnProto", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Scn903 = new(
        "SCN903", "Sonde arbre",
        "SONDE arbre de syntaxe : {0}",
        "ScnProto", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Scn001, Scn002, Scn901, Scn902, Scn903);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedFlag);
        context.RegisterSyntaxTreeAction(AnalyzeTree);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext ctx)
    {
        var tree = ctx.Tree;
        ctx.ReportDiagnostic(Diagnostic.Create(Scn903, Location.Create(tree, new TextSpan(0, 0)), tree.FilePath));
    }

    private static void AnalyzeType(SymbolAnalysisContext ctx)
    {
        var named = (INamedTypeSymbol)ctx.Symbol;
        foreach (var loc in named.Locations)
            ctx.ReportDiagnostic(Diagnostic.Create(Scn902, loc, named.Name, loc.SourceTree?.FilePath ?? "<none>"));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
    {
        var inv = (InvocationExpressionSyntax)ctx.Node;
        if (inv.Expression is not IdentifierNameSyntax id || id.Identifier.ValueText != "nameof")
            return;
        if (inv.ArgumentList.Arguments.Count != 1)
            return;

        var arg = inv.ArgumentList.Arguments[0].Expression;
        var info = ctx.SemanticModel.GetSymbolInfo(arg, ctx.CancellationToken);
        var sym = info.Symbol;
        var constant = ctx.SemanticModel.GetConstantValue(inv, ctx.CancellationToken);
        var decl = sym is null
            ? "<aucun>"
            : string.Join(" ; ", sym.Locations.Select(l => (l.SourceTree?.FilePath ?? "<sans arbre>") + (l.IsInSource ? "" : " (hors source)")));

        ctx.ReportDiagnostic(Diagnostic.Create(Scn901, inv.GetLocation(),
            arg.ToString(),
            sym?.ToDisplayString() ?? "null",
            sym?.Kind.ToString() ?? "null",
            info.CandidateReason.ToString(),
            constant.HasValue ? constant.Value?.ToString() ?? "null" : "aucune",
            decl));
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        foreach (var attrList in method.AttributeLists)
        foreach (var attr in attrList.Attributes)
        {
            var name = attr.Name.ToString();
            if (name != "TestScenario" && name != "TestScenarioAttribute")
                continue;
            if (attr.ArgumentList is null || attr.ArgumentList.Arguments.Count != 1)
                continue;
            if (attr.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax lit
                || !lit.IsKind(SyntaxKind.StringLiteralExpression))
                continue;

            CheckScenario(ctx, lit.Token);
        }
    }

    // Parcourt le texte BRUT du token (raw string inclus, indentation comprise) ligne par ligne.
    // La Location est le sous-intervalle de la ligne ALORS dans le fichier : colonnes réelles.
    private static void CheckScenario(SyntaxNodeAnalysisContext ctx, SyntaxToken token)
    {
        var tree = token.SyntaxTree!;
        var text = token.Text;
        int lineStart = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.TrimStart(' ', '\t');
            if (trimmed.StartsWith("ALORS"))
            {
                int absStart = token.SpanStart + lineStart + (line.Length - trimmed.Length);
                var span = new TextSpan(absStart, trimmed.Length);
                var location = Location.Create(tree, span);
                var lp = tree.GetLineSpan(span).StartLinePosition;

                if (trimmed.StartsWith("ALORS[") && trimmed.IndexOf(']') > 6)
                {
                    var key = trimmed.Substring(6, trimmed.IndexOf(']') - 6);
                    ctx.ReportDiagnostic(Diagnostic.Create(Scn002, location, key,
                        lp.Line + 1, lp.Character + 1, tree.FilePath));
                }
                else
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Scn001, location, trimmed));
                }
            }
            lineStart += rawLine.Length + 1;
        }
    }
}
