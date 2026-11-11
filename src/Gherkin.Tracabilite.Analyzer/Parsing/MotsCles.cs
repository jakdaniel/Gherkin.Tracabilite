using System.Globalization;
using System.Text;

namespace Gherkin.Tracabilite.Analyzer.Parsing;

/// <summary>Un mot-clé Gherkin, sous forme repliée (majuscules, sans accents, espaces réduits).</summary>
internal sealed class MotCle
{
    internal MotCle(string forme, Role role, Langue? langue, bool ambigu, bool exigeDeuxPoints)
    {
        Forme = forme;
        Role = role;
        Langue = langue;
        Ambigu = ambigu;
        ExigeDeuxPoints = exigeDeuxPoints;
    }

    internal string Forme { get; }

    internal Role Role { get; }

    /// <summary>Langue du mot ; null pour un mot ambigu sans langue propre (Scenario).</summary>
    internal Langue? Langue { get; }

    /// <summary>Mot ambigu (Scenario, But) : ne sert pas à la détection de la langue.</summary>
    internal bool Ambigu { get; }

    /// <summary>En-tête : le mot-clé doit être suivi de « : ».</summary>
    internal bool ExigeDeuxPoints { get; }

    /// <summary>Forme se terminant par une apostrophe (« Lorsqu' », « Et qu' ») : pas de contrainte de mot entier après.</summary>
    internal bool FinitParApostrophe => Forme[Forme.Length - 1] == '\'';
}

/// <summary>Résultat de la reconnaissance du mot-clé en tête d'une ligne.</summary>
internal sealed class MotReconnu
{
    internal MotReconnu(MotCle mot, string motCle, string reste)
    {
        Mot = mot;
        MotCle = motCle;
        Reste = reste;
    }

    internal MotCle Mot { get; }

    /// <summary>Mot-clé tel qu'écrit dans la ligne, sans le « : » des en-têtes.</summary>
    internal string MotCle { get; }

    /// <summary>Texte brut qui suit le mot-clé (et le « : » des en-têtes).</summary>
    internal string Reste { get; }
}

/// <summary>Table des mots-clés FR et EN de la spec §4, et reconnaissance en tête de ligne.</summary>
internal static class MotsCles
{
    private static readonly IReadOnlyList<MotCle> Table = Construire();

    // Triés par longueur décroissante : la forme la plus longue l'emporte (« ETANT DONNE QUE » avant « ETANT DONNE »).
    private static readonly IReadOnlyList<MotCle> Triees = Table.OrderByDescending(m => m.Forme.Length).ToList();

    private static readonly IReadOnlyList<string> AttendusFr = new[]
    {
        "Fonctionnalité :", "Scénario :", "Exemple :", "En tant que", "Je veux", "Afin de",
        "Soit", "Étant donné", "Sachant que", "Quand", "Lorsque", "Alors", "Donc", "Et", "Mais",
    };

    private static readonly IReadOnlyList<string> AttendusEn = new[]
    {
        "Feature:", "Scenario:", "Example:", "As a", "I want", "So that",
        "Given", "When", "Then", "And", "But",
    };

    private static readonly Dictionary<char, string> SansAccent = ConstruireSansAccent();

    /// <summary>Mots-clés attendus pour une langue ; les deux listes si la langue est inconnue.</summary>
    internal static IReadOnlyList<string> Attendus(Langue? langue)
    {
        var resultat = new List<string>();
        if (langue != Langue.Anglais)
        {
            resultat.AddRange(AttendusFr);
        }

        if (langue != Langue.Francais)
        {
            resultat.AddRange(AttendusEn);
        }

        return resultat;
    }

    /// <summary>
    /// Reconnaît le mot-clé en tête de la ligne. Les mots ambigus ne sont pas utilisés pour la détection
    /// (<paramref name="detection"/>) ; <c>But</c> n'est reconnu que dans un Scénario en anglais.
    /// </summary>
    internal static MotReconnu? Reconnaitre(string contenu, bool detection, Langue? langue)
    {
        var forme = Plier(contenu, out var index);
        if (forme.Length == 0)
        {
            return null;
        }

        foreach (var mot in Triees)
        {
            if (mot.Ambigu)
            {
                if (detection)
                {
                    continue;
                }

                if (mot.Langue != null && mot.Langue != langue)
                {
                    continue;
                }
            }

            if (!forme.StartsWith(mot.Forme, StringComparison.Ordinal))
            {
                continue;
            }

            var n = mot.Forme.Length;
            if (!mot.FinitParApostrophe && n < forme.Length && char.IsLetterOrDigit(forme[n]))
            {
                continue;
            }

            var debutReste = n;
            if (mot.ExigeDeuxPoints)
            {
                var k = n;
                while (k < forme.Length && forme[k] == ' ')
                {
                    k++;
                }

                if (k >= forme.Length || forme[k] != ':')
                {
                    continue;
                }

                debutReste = k + 1;
            }

            var finMot = n < forme.Length ? index[n] : contenu.Length;
            var motCle = contenu.Substring(index[0], finMot - index[0]).Trim();
            var debutRaw = debutReste < forme.Length ? index[debutReste] : contenu.Length;
            return new MotReconnu(mot, motCle, contenu.Substring(debutRaw));
        }

        return null;
    }

    /// <summary>
    /// Forme repliée d'une ligne : sans espace de tête, majuscules, sans accents, espaces multiples réduits à un,
    /// apostrophe typographique ramenée à « ' ». <paramref name="index"/> donne, pour chaque caractère replié,
    /// l'index correspondant dans le texte source.
    /// </summary>
    private static string Plier(string source, out int[] index)
    {
        var sb = new StringBuilder(source.Length);
        var idx = new List<int>(source.Length);
        var debut = true;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (char.IsWhiteSpace(c))
            {
                if (!debut)
                {
                    sb.Append(' ');
                    idx.Add(i);
                    debut = true;
                }

                continue;
            }

            debut = false;
            if (c == '’')
            {
                c = '\'';
            }

            if (c < 128)
            {
                sb.Append(char.ToUpperInvariant(c));
                idx.Add(i);
            }
            else if (SansAccent.TryGetValue(c, out var sans))
            {
                foreach (var s in sans)
                {
                    sb.Append(s);
                    idx.Add(i);
                }
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                // Marque combinante : retirée avec la lettre de base.
            }
            else
            {
                sb.Append(char.ToUpperInvariant(c));
                idx.Add(i);
            }
        }

        index = idx.ToArray();
        return sb.ToString();
    }

    private static Dictionary<char, string> ConstruireSansAccent()
    {
        var d = new Dictionary<char, string>();
        void Ajouter(string accentues, string sans)
        {
            foreach (var c in accentues)
            {
                d[c] = sans;
            }
        }

        Ajouter("àâäÀÂÄ", "A");
        Ajouter("éèêëÉÈÊË", "E");
        Ajouter("îïÎÏ", "I");
        Ajouter("ôöÔÖ", "O");
        Ajouter("ùûüÙÛÜ", "U");
        Ajouter("çÇ", "C");
        Ajouter("ÿŸ", "Y");
        Ajouter("œŒ", "OE");
        Ajouter("æÆ", "AE");
        return d;
    }

    private static List<MotCle> Construire()
    {
        var l = new List<MotCle>();

        // En-têtes
        l.Add(new MotCle("FONCTIONNALITE", Role.Fonctionnalite, Langue.Francais, false, true));
        l.Add(new MotCle("FEATURE", Role.Fonctionnalite, Langue.Anglais, false, true));
        l.Add(new MotCle("SCENARIO", Role.Scenario, null, true, true));
        l.Add(new MotCle("EXEMPLE", Role.Scenario, Langue.Francais, false, true));
        l.Add(new MotCle("EXAMPLE", Role.Scenario, Langue.Anglais, false, true));

        // Narration (ignorée)
        Ajouter(l, "EN TANT QUE", Role.Narration, Langue.Francais);
        Ajouter(l, "JE VEUX", Role.Narration, Langue.Francais);
        Ajouter(l, "AFIN DE", Role.Narration, Langue.Francais);
        Ajouter(l, "AS A", Role.Narration, Langue.Anglais);
        Ajouter(l, "I WANT", Role.Narration, Langue.Anglais);
        Ajouter(l, "SO THAT", Role.Narration, Langue.Anglais);

        // Contexte
        Ajouter(l, "SOIT", Role.Contexte, Langue.Francais);
        foreach (var donne in new[] { "ETANT DONNE", "ETANT DONNEE", "ETANT DONNES", "ETANT DONNEES" })
        {
            AjouterAvecQue(l, donne, Role.Contexte, Langue.Francais);
        }

        AjouterAvecQue(l, "SACHANT", Role.Contexte, Langue.Francais);
        Ajouter(l, "GIVEN", Role.Contexte, Langue.Anglais);

        // Action
        Ajouter(l, "QUAND", Role.Action, Langue.Francais);
        Ajouter(l, "LORSQUE", Role.Action, Langue.Francais);
        Ajouter(l, "LORSQU'", Role.Action, Langue.Francais);
        Ajouter(l, "WHEN", Role.Action, Langue.Anglais);

        // Résultat
        Ajouter(l, "ALORS", Role.Resultat, Langue.Francais);
        Ajouter(l, "DONC", Role.Resultat, Langue.Francais);
        Ajouter(l, "THEN", Role.Resultat, Langue.Anglais);

        // Prolongement
        AjouterAvecQue(l, "ET", Role.Prolongement, Langue.Francais);
        AjouterAvecQue(l, "MAIS", Role.Prolongement, Langue.Francais);
        Ajouter(l, "AND", Role.Prolongement, Langue.Anglais);
        l.Add(new MotCle("BUT", Role.Prolongement, Langue.Anglais, true, false));

        return l;
    }

    private static void Ajouter(List<MotCle> l, string forme, Role role, Langue langue)
    {
        l.Add(new MotCle(forme, role, langue, false, false));
    }

    private static void AjouterAvecQue(List<MotCle> l, string forme, Role role, Langue langue)
    {
        Ajouter(l, forme, role, langue);
        Ajouter(l, forme + " QUE", role, langue);
        Ajouter(l, forme + " QU'", role, langue);
    }
}
