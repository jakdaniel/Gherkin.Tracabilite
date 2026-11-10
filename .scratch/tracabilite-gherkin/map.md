# Carte : Traçabilité Gherkin ↔ tests .NET

Label: wayfinder:map

## Destination

Une **spec complète et tranchée** du composant de traçabilité (attribut `TestScenario`, parser, `ScenarioAssert.Verify`, analyzer Roslyn) : un agent ou un dev peut l'implémenter sans avoir à prendre de décision.

## Notes

- Domaine : outillage de test .NET / Roslyn. Vocabulaire dans [GLOSSARY.md](../../GLOSSARY.md) (Scénario, Résultat attendu, Clé, Vérification) : un seul terme par concept.
- Source brute de l'idée : [readme-origine.md](readme-origine.md). Les décisions ci-dessous priment sur lui.
- Skills à consulter : `grilling` + `domain-modeling` pour les tickets grilling ; `research` pour les tickets research.
- **Contexte d'usage** : le composant est le **garde-fou** d'une chaîne de création de tests. Un analyste écrit un Gherkin ; un skill (agent) écrit le test, y recopie le Scénario et **y ajoute les Clés** ; le build lance l'analyzer ; l'agent corrige jusqu'à ce que le build passe. Le premier lecteur des diagnostics est donc **un agent qui lit la sortie du build**, pas un humain dans un IDE.
- **Cadrage déjà tranché lors de la création de la carte** :
  - Langues : français **et** anglais ; une langue par Scénario, détectée d'après le premier mot-clé ; `MAIS`/`But` équivaut à `ET`/`And`.
  - Un Résultat attendu sans Clé → diagnostic ; Clé en double dans un Scénario → erreur ; double Vérification d'une même Clé → permise.
  - Analyse limitée aux Vérifications écrites directement dans le corps de la méthode de test (lambdas, `if` et boucles compris), sans analyse inter-procédurale.
  - Diagnostics en **erreur** par défaut, réglables via `.editorconfig`, avec des messages **auto-suffisants** pour un agent.
  - Scénario **inline** dans l'attribut uniquement ; c'est le texte de l'attribut qui fait foi.
  - Agnostique du framework de test (xUnit v3 sert seulement aux tests du composant).
  - Cibles : runtime `net10.0` / C# 14 ; analyzer `netstandard2.0`.
  - **Pas de NuGet** : consommation par `ProjectReference` (`OutputItemType="Analyzer"` pour l'analyzer). Projets : runtime, analyzer, tests xUnit v3, exemple consommateur.

## Decisions so far

<!-- une ligne par ticket résolu -->
- [Faisabilité Roslyn : lire le Scénario, localiser les diagnostics, symboles générés](issues/01-faisabilite-roslyn.md) : un générateur peut fournir des symboles `nameof` sous conditions (noms déterministes, identifiants C# valides, classe `partial` si augmentation) ; la localisation dans un raw string et la sortie CLI d'un `ProjectReference` restent à prototyper.
- [Valider par un prototype les faits Roslyn non confirmés](issues/06-validation-roslyn-par-prototype.md) : prototype jetable (`prototypes/`) : les diagnostics d'un `ProjectReference` analyzer sortent dans `dotnet build` (une `error` fait échouer le build, un `.editorconfig` la rétrograde), les constantes générées sont résolues par `nameof` et visibles de l'analyzer, les positions dans un raw string indenté sont exactes (y compris `"""` sur la ligne de l'attribut), et `GeneratedCodeAnalysisFlags.None` exclut le `.g.cs`.
- [Comment une Vérification référence-t-elle une Clé ?](issues/02-referencement-des-cles.md) : constantes générées dans `{Methode}_Cles`, imbriqué dans une classe de test `partial` ; `Verify` n'accepte que les Clés de son propre Scénario ; une Clé est un identifiant C# valide.
- [Grammaire exacte du Scénario](issues/03-grammaire-du-scenario.md) : sous-ensemble Gherkin FR/EN tolérant sur la forme (casse, accents, `QUE` facultatif) ; Résultats attendus = `ALORS` et les `ET`/`MAIS` qui le suivent ; un seul `Scénario`, au moins un `ALORS`, pas d'ordre imposé.
- [Comportement de ScenarioAssert.Verify à l'exécution](issues/04-comportement-de-verify.md) : exécution pure, sans enveloppe ni enrichissement, arrêt au premier échec ; `VerifyAsync` séparé pour les assertions async.
- [Catalogue des diagnostics](issues/05-catalogue-des-diagnostics.md) : SCN001 à SCN013, tous en erreur, rapportés par l'analyzer seul, avec des messages qui disent quoi, où et comment corriger.
- Contrat avec le skill et stratégie de test : tranchés par grilling, consignés dans [spec.md](spec.md).

## Not yet specified

(rien : la spec est assemblée dans [spec.md](spec.md).)

## Out of scope

- Source Generator de squelette de test (`partial` → `[Fact]` pré-rempli) : c'est le skill qui écrit le test.
- CodeFix pour SCN001 : un agent ne l'utilise pas ; on pourra l'ajouter plus tard pour les humains.
- Paquet NuGet.
- Le skill de création de tests lui-même (effort séparé ; seul le contrat est dans le périmètre).
- Détection d'écart entre le Scénario de l'attribut et le Gherkin d'origine de l'analyste.
- Fichiers `.feature` externes ; `Plan du scénario`/`Scenario Outline`, `Exemples`, `Contexte`/`Background`, tags `@`.
- Analyse inter-procédurale (Vérifications placées dans des helpers).
