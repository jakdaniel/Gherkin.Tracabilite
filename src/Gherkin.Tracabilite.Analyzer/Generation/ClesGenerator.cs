using System.Text;
using System.Threading;
using Gherkin.Tracabilite.Analyzer.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gherkin.Tracabilite.Analyzer.Generation;

/// <summary>
/// Émet, pour chaque méthode [TestScenario], la classe <c>{Methode}_Cles</c> contenant une constante par Clé valide (spec §5).
/// Ne rapporte aucun diagnostic : il n'émet rien quand SCN011, SCN012 ou SCN013 s'applique.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ClesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fichiers = context.SyntaxProvider.ForAttributeWithMetadataName(
                MethodesScenario.NomAttribut,
                static (noeud, _) => noeud is MethodDeclarationSyntax,
                static (ctx, ct) => Produire(ctx, ct))
            .Where(static fichier => fichier is not null)
            .Select(static (fichier, _) => fichier!.Value);

        context.RegisterSourceOutput(fichiers, static (spc, fichier) => spc.AddSource(fichier.NomFichier, fichier.Contenu));
    }

    private static (string NomFichier, string Contenu)? Produire(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methode || ctx.Attributes.Length == 0)
        {
            return null;
        }

        var attribut = ctx.Attributes[0];
        if (attribut.ConstructorArguments.Length != 1 || attribut.ConstructorArguments[0].Value is not string texte)
        {
            return null;
        }

        // SCN012 : le Scénario doit être un littéral de chaîne.
        if (!EstLitteral(attribut, ct))
        {
            return null;
        }

        // SCN011 et SCN013 : classes partial et méthode non surchargée (règle partagée avec l'analyzer).
        if (!MethodesScenario.ClesEmises(methode))
        {
            return null;
        }

        var cles = ScenarioParser.Analyser(texte, methode.Name).ClesValides;
        var nomFichier = MethodesScenario.NomDeMetadonnees(methode) + "." + methode.Name + ".g.cs";
        var objet = ctx.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        return (nomFichier, Generer(methode, cles, objet));
    }

    private static bool EstLitteral(AttributeData attribut, CancellationToken ct) =>
        attribut.ApplicationSyntaxReference?.GetSyntax(ct) is AttributeSyntax syntaxe
        && MethodesScenario.LitteralDuScenario(syntaxe) is not null;

    private static string Generer(IMethodSymbol methode, IReadOnlyList<string> cles, INamedTypeSymbol objet)
    {
        var sb = new StringBuilder();
        var profondeur = 0;
        var ouvertures = 0;

        var ns = MethodesScenario.NomNamespace(methode.ContainingNamespace);
        if (ns.Length > 0)
        {
            Ouvrir(sb, ref profondeur, $"namespace {ns}");
            ouvertures++;
        }

        foreach (var type in MethodesScenario.Chaine(methode))
        {
            Ouvrir(sb, ref profondeur, $"partial {MethodesScenario.Genre(type)} {MethodesScenario.NomEcrit(type)}{Parametres(type)}");
            ouvertures++;
        }

        Ouvrir(sb, ref profondeur, $"public static class {MethodesScenario.NomClasseCles(methode.Name)}");
        ouvertures++;

        foreach (var cle in cles)
        {
            // Une Clé qui porte le nom d'un membre de object (Equals, ToString…) masque ce membre : CS0108 sans « new ».
            var masque = objet.GetMembers(cle).Length > 0 ? "new " : string.Empty;
            Ecrire(sb, profondeur, $"public {masque}const string {cle} = \"{cle}\";");
        }

        for (var i = 0; i < ouvertures; i++)
        {
            profondeur--;
            Ecrire(sb, profondeur, "}");
        }

        return sb.ToString();
    }

    private static string Parametres(INamedTypeSymbol type)
    {
        if (type.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return "<" + string.Join(", ", type.TypeParameters.Select(p => p.Name)) + ">";
    }

    private static void Ouvrir(StringBuilder sb, ref int profondeur, string ligne)
    {
        Ecrire(sb, profondeur, ligne);
        Ecrire(sb, profondeur, "{");
        profondeur++;
    }

    private static void Ecrire(StringBuilder sb, int profondeur, string ligne)
    {
        sb.Append(' ', profondeur * 4).Append(ligne).Append('\n');
    }
}
