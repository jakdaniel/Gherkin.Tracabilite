using System.Globalization;

namespace Gherkin.Tracabilite.Analyzer.Parsing;

/// <summary>Validation d'une Clé comme identifiant C# (spec §4).</summary>
internal static class IdentifiantCSharp
{
    // Mots réservés C# (les mots-clés contextuels restent permis).
    private static readonly HashSet<string> MotsReserves = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    /// <summary>
    /// Vrai si <paramref name="s"/> est composé de lettres Unicode, chiffres et « _ », sans commencer par un chiffre.
    /// Les caractères hors BMP (paires de surrogates) sont refusés : Roslyn ne les accepte pas dans un identifiant (CS1056),
    /// donc une Clé qui en contient produirait un code généré non compilable.
    /// </summary>
    internal static bool EstIdentifiant(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsSurrogate(s[i]))
            {
                return false;
            }

            var categorie = CharUnicodeInfo.GetUnicodeCategory(s, i);
            var lettre = categorie == UnicodeCategory.UppercaseLetter
                || categorie == UnicodeCategory.LowercaseLetter
                || categorie == UnicodeCategory.TitlecaseLetter
                || categorie == UnicodeCategory.ModifierLetter
                || categorie == UnicodeCategory.OtherLetter
                || categorie == UnicodeCategory.LetterNumber;
            var souligne = s[i] == '_';

            if (i == 0)
            {
                if (!lettre && !souligne)
                {
                    return false;
                }
            }
            else
            {
                var chiffre = categorie == UnicodeCategory.DecimalDigitNumber;
                var marque = categorie == UnicodeCategory.NonSpacingMark
                    || categorie == UnicodeCategory.SpacingCombiningMark;
                if (!lettre && !chiffre && !souligne && !marque)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool EstMotReserve(string s) => MotsReserves.Contains(s);
}
