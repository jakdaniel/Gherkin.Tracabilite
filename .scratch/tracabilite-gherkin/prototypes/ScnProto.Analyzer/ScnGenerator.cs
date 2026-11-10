using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ScnProto.Analyzer;

// Prototype jetable : pour chaque méthode annotée [TestScenario], émet dans la classe partielle
// une constante `public const string Cle = "Cle";` par ALORS[Cle] trouvé dans le Scénario.
[Generator(Microsoft.CodeAnalysis.LanguageNames.CSharp)]
public sealed class ScnGenerator : IIncrementalGenerator
{
    private static readonly Regex KeyRegex = new(@"ALORS\[([^\]]+)\]", RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var infos = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Demo.TestScenarioAttribute",
            predicate: static (node, _) => node is MethodDeclarationSyntax,
            transform: static (ctx, _) => ToInfo(ctx))
            .Where(static i => i is not null);

        context.RegisterSourceOutput(infos.Collect(), static (spc, items) => Emit(spc, items));
    }

    private static ScenarioInfo? ToInfo(GeneratorAttributeSyntaxContext ctx)
    {
        var method = (IMethodSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];
        if (attr.ConstructorArguments.Length != 1 || attr.ConstructorArguments[0].Value is not string scenario)
            return null;

        var keys = KeyRegex.Matches(scenario).Cast<Match>().Select(m => m.Groups[1].Value);
        return new ScenarioInfo(
            method.ContainingNamespace.IsGlobalNamespace ? "" : method.ContainingNamespace.ToDisplayString(),
            method.ContainingType.Name,
            keys.Distinct().ToArray());
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<ScenarioInfo?> items)
    {
        foreach (var group in items.Where(i => i is not null).GroupBy(i => (i!.Namespace, i!.TypeName)))
        {
            var sb = new StringBuilder();
            if (group.Key.Namespace.Length > 0)
                sb.AppendLine($"namespace {group.Key.Namespace};");
            sb.AppendLine($"public partial class {group.Key.TypeName}");
            sb.AppendLine("{");
            foreach (var key in group.SelectMany(i => i!.Keys).Distinct())
                sb.AppendLine($"    public const string {key} = \"{key}\";");
            sb.AppendLine("}");
            spc.AddSource($"{group.Key.TypeName}.g.cs", sb.ToString());
        }
    }
}

public sealed class ScenarioInfo
{
    public ScenarioInfo(string ns, string typeName, string[] keys)
    {
        Namespace = ns;
        TypeName = typeName;
        Keys = keys;
    }

    public string Namespace { get; }
    public string TypeName { get; }
    public string[] Keys { get; }
}
