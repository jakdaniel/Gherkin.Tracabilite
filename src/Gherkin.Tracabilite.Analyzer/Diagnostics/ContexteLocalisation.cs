using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Gherkin.Tracabilite.Analyzer.Parsing;

namespace Gherkin.Tracabilite.Analyzer.Diagnostics;

/// <summary>
/// Calcule l'emplacement d'une ligne du Scénario dans le fichier. Pour un raw string multiligne,
/// la ligne <c>n</c> du texte correspond à la ligne <c>n</c> du littéral (le texte commence après le premier saut de ligne).
/// Sinon, ou sans ligne fautive, l'emplacement est celui du littéral ou de l'attribut.
/// </summary>
internal sealed class ContexteLocalisation
{
    private readonly SyntaxToken _token;
    private readonly AttributeSyntax _attribut;
    private readonly string[]? _lignesSource;
    private readonly int[]? _debutsLignes;

    internal ContexteLocalisation(SyntaxToken token, AttributeSyntax attribut)
    {
        _token = token;
        _attribut = attribut;

        var texte = token.Text;
        if (texte.StartsWith("\"\"\"", StringComparison.Ordinal) && texte.IndexOf('\n') >= 0)
        {
            _lignesSource = texte.Split('\n');
            _debutsLignes = new int[_lignesSource.Length];
            var debut = token.SpanStart;
            for (var i = 0; i < _lignesSource.Length; i++)
            {
                _debutsLignes[i] = debut;
                debut += _lignesSource[i].Length + 1;
            }
        }
    }

    internal Location Emplacement(LigneScenario? ligne)
    {
        if (ligne is null)
        {
            return _attribut.GetLocation();
        }

        if (_lignesSource is null || _debutsLignes is null || ligne.Numero >= _lignesSource.Length)
        {
            return _token.GetLocation();
        }

        var source = _lignesSource[ligne.Numero].TrimEnd('\r');
        if (source.EndsWith("\"\"\"", StringComparison.Ordinal))
        {
            source = source.Substring(0, source.Length - 3);
        }

        var espaces = source.Length - source.TrimStart(' ', '\t').Length;
        var longueur = source.Length - espaces;
        var span = new TextSpan(_debutsLignes[ligne.Numero] + espaces, longueur);

        return Location.Create(_token.SyntaxTree!, span);
    }
}
