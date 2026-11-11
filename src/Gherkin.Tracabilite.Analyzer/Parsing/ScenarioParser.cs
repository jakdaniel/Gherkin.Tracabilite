using Gherkin.Tracabilite.Analyzer.Generation;

namespace Gherkin.Tracabilite.Analyzer.Parsing;

/// <summary>
/// Parser du texte d'un Scénario (spec §4), partagé par le générateur et l'analyzer.
/// Pur : retourne un modèle et la liste typée des erreurs de grammaire, sans diagnostic.
/// </summary>
internal static class ScenarioParser
{
    /// <param name="texte">Texte du Scénario, tel que écrit dans l'attribut.</param>
    /// <param name="nomMethode">Nom de la méthode de test, pour détecter une Clé égale à <c>{Methode}_Cles</c>.</param>
    internal static AnalyseScenario Analyser(string texte, string nomMethode)
    {
        return new Analyseur(texte ?? string.Empty, nomMethode ?? string.Empty).Executer();
    }

    private enum TypeLigne
    {
        Ignoree,
        Tableau,
        DebutDocstring,
        Candidate,
    }

    private sealed class Analyseur
    {
        private readonly string _nomMethode;
        private readonly List<LigneScenario> _lignes = new List<LigneScenario>();
        private readonly List<TypeLigne> _types = new List<TypeLigne>();
        private readonly List<ErreurScenario> _erreurs = new List<ErreurScenario>();
        private readonly List<Etape> _etapes = new List<Etape>();
        private readonly List<string> _clesValides = new List<string>();
        private readonly HashSet<string> _clesVues = new HashSet<string>(StringComparer.Ordinal);

        private Langue? _langue;
        private string? _motCleLangue;
        private bool _melangeSignale;
        private bool _fonctionnaliteVue;
        private bool _scenarioVu;
        private bool _etapeVue;
        private bool _resultatVu;
        private Role _dernierRoleEtape = Role.Ignoree;

        internal Analyseur(string texte, string nomMethode)
        {
            _nomMethode = nomMethode;
            Decouper(texte);
            Classer();
        }

        internal AnalyseScenario Executer()
        {
            DetecterLangue();
            for (var i = 0; i < _lignes.Count; i++)
            {
                Traiter(_lignes[i], _types[i]);
            }

            VerifierStructure();

            return new AnalyseScenario(_lignes, _etapes, _erreurs, _clesValides, _langue, _motCleLangue);
        }

        private void Decouper(string texte)
        {
            var debut = 0;
            var numero = 1;
            for (var i = 0; i <= texte.Length; i++)
            {
                if (i == texte.Length || texte[i] == '\n')
                {
                    var fin = i;
                    if (fin > debut && texte[fin - 1] == '\r')
                    {
                        fin--;
                    }

                    _lignes.Add(new LigneScenario(numero, debut, texte.Substring(debut, fin - debut)));
                    debut = i + 1;
                    numero++;
                }
            }
        }

        private void Classer()
        {
            var dansDocstring = false;
            foreach (var ligne in _lignes)
            {
                var t = ligne.Contenu.Trim();
                if (dansDocstring)
                {
                    if (t.StartsWith("\"\"\"", StringComparison.Ordinal))
                    {
                        dansDocstring = false;
                    }

                    _types.Add(TypeLigne.Ignoree);
                }
                else if (t.Length == 0 || t[0] == '#')
                {
                    _types.Add(TypeLigne.Ignoree);
                }
                else if (t[0] == '|')
                {
                    _types.Add(TypeLigne.Tableau);
                }
                else if (t.StartsWith("\"\"\"", StringComparison.Ordinal))
                {
                    _types.Add(TypeLigne.DebutDocstring);
                    var uneSeuleLigne = t.Length >= 6 && t.EndsWith("\"\"\"", StringComparison.Ordinal);
                    dansDocstring = !uneSeuleLigne;
                }
                else
                {
                    _types.Add(TypeLigne.Candidate);
                }
            }
        }

        private void DetecterLangue()
        {
            for (var i = 0; i < _lignes.Count; i++)
            {
                if (_types[i] != TypeLigne.Candidate)
                {
                    continue;
                }

                var reconnu = MotsCles.Reconnaitre(_lignes[i].Contenu, true, null);
                if (reconnu != null)
                {
                    _langue = reconnu.Mot.Langue;
                    _motCleLangue = reconnu.MotCle;
                    return;
                }
            }
        }

        private void Traiter(LigneScenario ligne, TypeLigne type)
        {
            switch (type)
            {
                case TypeLigne.Ignoree:
                    ligne.Role = Role.Ignoree;
                    return;
                case TypeLigne.Tableau:
                    ligne.Role = Role.Tableau;
                    return;
                case TypeLigne.DebutDocstring:
                    ligne.Role = Role.Inconnue;
                    _erreurs.Add(new LigneNonReconnue(ligne, _langue));
                    return;
            }

            var reconnu = MotsCles.Reconnaitre(ligne.Contenu, false, _langue);
            if (reconnu == null)
            {
                TraiterSansMotCle(ligne);
                return;
            }

            ligne.MotCle = reconnu.MotCle;

            // Un ALORS vu avant l'en-tête Scénario (SCN008) compte tout de même : seule l'absence de ligne Scénario est signalée.
            if (reconnu.Mot.Role == Role.Resultat)
            {
                _resultatVu = true;
            }

            if (!_melangeSignale && _langue != null && reconnu.Mot.Langue != null && reconnu.Mot.Langue != _langue)
            {
                _melangeSignale = true;
                _erreurs.Add(new LanguesMelangees(ligne, _langue.Value, _motCleLangue ?? string.Empty, reconnu.Mot.Langue.Value));
            }

            switch (reconnu.Mot.Role)
            {
                case Role.Fonctionnalite:
                    if (_fonctionnaliteVue || _etapeVue || _scenarioVu)
                    {
                        RejeterLigne(ligne);
                    }
                    else
                    {
                        _fonctionnaliteVue = true;
                        ligne.Role = Role.Fonctionnalite;
                    }

                    return;

                case Role.Scenario:
                    if (_scenarioVu)
                    {
                        _erreurs.Add(new StructureInvalide(ligne, CauseStructure.PlusieursLignesScenario));
                    }

                    _scenarioVu = true;
                    ligne.Role = Role.Scenario;
                    return;

                case Role.Narration:
                    if (_etapeVue)
                    {
                        RejeterLigne(ligne);
                    }
                    else
                    {
                        ligne.Role = Role.Narration;
                    }

                    return;

                default:
                    if (!_scenarioVu)
                    {
                        RejeterLigne(ligne);
                    }
                    else
                    {
                        TraiterEtape(ligne, reconnu);
                    }

                    return;
            }
        }

        private void TraiterSansMotCle(LigneScenario ligne)
        {
            if (_scenarioVu && !_etapeVue)
            {
                ligne.Role = Role.Texte;
            }
            else
            {
                RejeterLigne(ligne);
            }
        }

        private void RejeterLigne(LigneScenario ligne)
        {
            ligne.Role = Role.Inconnue;
            _erreurs.Add(new LigneNonReconnue(ligne, _langue));
        }

        private void TraiterEtape(LigneScenario ligne, MotReconnu reconnu)
        {
            var role = reconnu.Mot.Role;
            ligne.Role = role;

            bool estResultat;
            if (role == Role.Prolongement)
            {
                if (!_etapeVue)
                {
                    _erreurs.Add(new StructureInvalide(ligne, CauseStructure.EtOuMaisSansEtapePrecedente));
                }

                estResultat = _dernierRoleEtape == Role.Resultat;
            }
            else
            {
                estResultat = role == Role.Resultat;
                _dernierRoleEtape = role;
            }

            _etapeVue = true;

            DecouperCle(reconnu.Reste, out var cle, out var description, out var malforme);

            if (malforme)
            {
                _erreurs.Add(new LigneNonReconnue(ligne, _langue));
            }

            if (!estResultat)
            {
                if (cle != null)
                {
                    _erreurs.Add(new CleSurMauvaiseEtape(ligne, cle));
                }

                _etapes.Add(new Etape(ligne, cle, false, description, false));
                return;
            }

            if (cle == null)
            {
                _erreurs.Add(new ResultatSansCle(ligne));
                _etapes.Add(new Etape(ligne, null, false, description, true));
                return;
            }

            var cleValide = VerifierCle(ligne, cle);
            if (cleValide)
            {
                if (!_clesVues.Contains(cle))
                {
                    _clesVues.Add(cle);
                    _clesValides.Add(cle);
                }
                else
                {
                    _erreurs.Add(new CleEnDouble(ligne, cle));
                }
            }

            if (description.Length == 0)
            {
                _erreurs.Add(new StructureInvalide(ligne, CauseStructure.DescriptionVideApresCle));
            }

            _etapes.Add(new Etape(ligne, cle, cleValide, description, true));
        }

        /// <summary>Vérifie la validité d'une Clé et signale SCN005 si elle est invalide.</summary>
        private bool VerifierCle(LigneScenario ligne, string cle)
        {
            if (!IdentifiantCSharp.EstIdentifiant(cle))
            {
                _erreurs.Add(new CleInvalide(ligne, cle, CauseCleInvalide.NonIdentifiant));
                return false;
            }

            if (IdentifiantCSharp.EstMotReserve(cle))
            {
                _erreurs.Add(new CleInvalide(ligne, cle, CauseCleInvalide.MotReserve));
                return false;
            }

            if (string.Equals(cle, MethodesScenario.NomClasseCles(_nomMethode), StringComparison.Ordinal))
            {
                _erreurs.Add(new CleInvalide(ligne, cle, CauseCleInvalide.NomClasseGeneree));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Découpe le texte qui suit le mot-clé en Clé (entre crochets, après d'éventuels espaces) et description.
        /// <paramref name="malforme"/> est vrai si du texte suit le « ] » sans espace.
        /// </summary>
        private static void DecouperCle(string reste, out string? cle, out string description, out bool malforme)
        {
            malforme = false;
            var t = reste.TrimStart();
            if (t.Length == 0 || t[0] != '[')
            {
                cle = null;
                description = t.Trim();
                return;
            }

            var fin = t.IndexOf(']');
            if (fin < 0)
            {
                cle = t.Substring(1);
                description = string.Empty;
                return;
            }

            cle = t.Substring(1, fin - 1);
            var apres = t.Substring(fin + 1);
            if (apres.Length > 0 && !char.IsWhiteSpace(apres[0]))
            {
                malforme = true;
            }

            description = apres.Trim();
        }

        private void VerifierStructure()
        {
            if (!_scenarioVu)
            {
                _erreurs.Add(new StructureInvalide(null, CauseStructure.AucuneLigneScenario));
            }

            if (!_resultatVu)
            {
                _erreurs.Add(new StructureInvalide(null, CauseStructure.AucunAlors));
            }
        }
    }
}
