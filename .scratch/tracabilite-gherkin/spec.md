# Spec : Traçabilité Gherkin ↔ tests .NET

Vocabulaire : [GLOSSARY.md](../../GLOSSARY.md) (Scénario, Résultat attendu, Clé, Vérification, Clés générées). Décisions détaillées : tickets de [map.md](map.md). Faits Roslyn validés : [prototypes/README.md](prototypes/README.md).

## 1. But et contexte

Vérifier **à la compilation** que chaque Résultat attendu d'un Scénario Gherkin, recopié dans un attribut sur une méthode de test, est couvert par au moins une Vérification dans cette méthode.

Le composant est le garde-fou d'une chaîne : un analyste écrit le Gherkin, un skill (agent) écrit le test, `dotnet build` lance l'analyzer, et l'agent corrige jusqu'à ce que le build passe. Le premier lecteur des diagnostics est un agent qui lit la sortie CLI.

Ce n'est pas un framework BDD. Le composant est agnostique du framework de test.

## 2. Livrables et cibles

| Projet | Cible | Contenu |
|---|---|---|
| Runtime | `net10.0`, C# 14 | `TestScenarioAttribute`, `ScenarioAssert` |
| Analyzer | `netstandard2.0`, `Microsoft.CodeAnalysis.CSharp` 5.6.0 (`PrivateAssets=all`) | parser, générateur incrémental, analyzer (un seul assembly) |
| Tests | xUnit v3 | tests du parser, du générateur et de l'analyzer |
| Exemple | `net10.0` | projet consommateur, test de bout en bout |

Pas de NuGet. Consommation par `ProjectReference` : runtime en référence normale ; analyzer avec `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.

## 3. Runtime

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestScenarioAttribute(string scenario) : Attribute
{
    public string Scenario { get; } = scenario;
}

public static class ScenarioAssert
{
    public static void Verify(string cle, Action assertion) => assertion();
    public static Task VerifyAsync(string cle, Func<Task> assertion) => assertion();
}
```

- `Verify` et `VerifyAsync` exécutent l'assertion, et rien d'autre : l'exception remonte telle quelle (pas d'enveloppe, pas de Clé ni de description ajoutées), arrêt au premier échec.
- Raison : envelopper l'exception changerait la classification de l'échec par certains frameworks ; la pile d'appels pointe déjà la ligne de la Vérification.

## 4. Grammaire du Scénario

Le texte de l'attribut fait foi. Il doit être un **littéral** écrit dans l'attribut (raw string `"""…"""` recommandé), sinon SCN012.

### Mots-clés

Insensibles à la casse et aux accents ; espaces multiples réduits à un ; espace facultatif avant `:` ; `QUE`/`QU'` facultatif.

| Rôle | FR | EN |
|---|---|---|
| En-tête | `Fonctionnalité :` | `Feature:` |
| En-tête | `Scénario :` | `Scenario:` |
| Narration (ignorée) | `En tant que`, `Je veux`, `Afin de` | `As a`, `I want`, `So that` |
| Contexte | `Soit`, `Étant donné [que\|qu']` | `Given` |
| Action | `Quand`, `Lorsque`, `Lorsqu'` | `When` |
| Résultat | `Alors` | `Then` |
| Prolongement | `Et [que\|qu']`, `Mais [que\|qu']` | `And`, `But` |

- Langue détectée d'après le premier mot-clé ; une ligne dans l'autre langue → SCN009.
- `Mais`/`But` équivaut à `Et`/`And`.

### Lignes

- Lignes vides, indentation et commentaires `#` : ignorés.
- Narration et texte libre : permis uniquement entre l'en-tête `Scénario` et la première étape.
- Tableaux `| … |` rattachés à une étape : acceptés et ignorés.
- Docstrings et toute autre ligne : SCN008.

### Résultats attendus et Clés

- Un Résultat attendu est une ligne `ALORS`, ou une ligne `ET`/`MAIS` qui suit (directement ou via d'autres `ET`/`MAIS`) un `ALORS`. Plusieurs blocs `LORSQUE → ALORS` sont permis ; chaque `ALORS` ouvre un bloc.
- Syntaxe : `MotClé[Cle] description` ou `MotClé [Cle] description`, puis au moins un espace et une description non vide.
- Une Clé est un identifiant C# valide (lettres Unicode permises), sans `@` ni mot réservé ; comparaison sensible à la casse ; unique dans le Scénario.
- Résultat attendu sans Clé → SCN003 ; Clé en double → SCN004 ; Clé invalide → SCN005 ; Clé sur une autre étape → SCN006.

### Structure

- `Fonctionnalité` facultative ; exactement une ligne `Scénario` ; au moins un `ALORS` ; pas d'`ET`/`MAIS` en première étape ; description non vide après une Clé. Sinon SCN007.
- Aucun ordre imposé entre `ÉTANT DONNÉ`, `LORSQUE` et `ALORS` au-delà de ces règles.

Hors périmètre : `Plan du scénario`/`Scenario Outline`, `Exemples`, `Contexte`/`Background`, tags `@`, fichiers `.feature`.

## 5. Clés générées

Pour chaque méthode `[TestScenario]`, le générateur (`ForAttributeWithMetadataName`) émet dans le fichier `{Classe}.{Methode}.g.cs` :

```csharp
partial class {Classe}
{
    public static class {Methode}_Cles
    {
        public const string {Cle} = "{Cle}";
        // une constante par Clé valide
    }
}
```

(namespace et classes englobantes reproduits ; le nom `{Methode}` seul est impossible, il entre en conflit avec la méthode : CS0102.)

- Le générateur émet les Clés valides même si le Scénario a d'autres défauts (SCN003 à SCN009), pour éviter une cascade de CS0103.
- Il n'émet rien si la classe n'est pas `partial` (SCN011), si le Scénario n'est pas un littéral (SCN012) ou pour des méthodes `[TestScenario]` surchargées (SCN013).
- Le générateur ne rapporte aucun diagnostic.

## 6. Vérifications

```csharp
ScenarioAssert.Verify(nameof(CreerTache_AvecInformationsObligatoires_Cles.TacheEnregistree),
    () => tache.Should().NotBeNull());
```

- L'argument `cle` doit être une constante du type `{Methode}_Cles` de **la méthode qui contient l'appel** (`nameof(...)` ou la constante directement). Chaîne littérale, Clé d'un autre Scénario ou valeur non constante → SCN002.
- Comptent les Vérifications écrites dans le corps de la méthode, y compris dans des lambdas, `if`, boucles et fonctions locales (sans vérifier que la fonction locale est appelée). Pas d'analyse inter-procédurale.
- Plusieurs Vérifications d'une même Clé sont permises.
- `Verify`/`VerifyAsync` dans une méthode sans `[TestScenario]` → SCN010.

## 7. Analyzer

- Seul rapporteur de diagnostics. `EnableConcurrentExecution()`, `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`.
- Parse le Scénario lui-même (même parser que le générateur).
- Localisation d'une ligne du Scénario : calculée depuis le texte brut du token du littéral (`Location.Create(tree, span)`), validée par le prototype, y compris quand `"""` est sur la ligne de l'attribut.

## 8. Diagnostics

Tous en `error` par défaut, catégorie `Tracabilite`, réglables par `dotnet_diagnostic.SCNxxx.severity` dans `.editorconfig`. Messages en français ; chacun dit quoi, où et comment corriger.

| Code | Condition | Emplacement | Message |
|---|---|---|---|
| SCN001 | Clé du Scénario sans Vérification | ligne du Résultat attendu | `Le Résultat attendu [{Cle}] (« {Description} ») du Scénario de '{Classe}.{Methode}' n'a aucune Vérification. Ajoutez dans la méthode : ScenarioAssert.Verify(nameof({Methode}_Cles.{Cle}), () => …);` |
| SCN002 | Argument de `Verify` qui n'est pas une Clé générée de ce Scénario | argument | `L'argument de Verify doit être une Clé du Scénario de '{Methode}', écrite nameof({Methode}_Cles.<Clé>). Reçu : {argument} ({cause}). Clés disponibles : {liste}.` — cause : `chaîne littérale` / `Clé d'un autre Scénario` / `valeur non constante` |
| SCN003 | Résultat attendu sans Clé | ligne | `Le Résultat attendu « {ligne} » n'a pas de Clé. Ajoutez un identifiant entre crochets après le mot-clé, par exemple : {MotCle}[NomDeLaCle] {Description}.` |
| SCN004 | Clé en double | ligne de la seconde | `La Clé [{Cle}] apparaît plusieurs fois dans le Scénario de '{Methode}'. Chaque Clé doit être unique : renommez celle-ci.` |
| SCN005 | Clé invalide | ligne | `La Clé [{Cle}] n'est pas un identifiant C# valide ou est un mot réservé. Utilisez lettres, chiffres et _, sans commencer par un chiffre.` |
| SCN006 | Clé sur une étape qui n'est pas un Résultat attendu | ligne | `La Clé [{Cle}] est posée sur une étape {MotCle} : seules les lignes ALORS/THEN et les ET/MAIS/AND/BUT qui les suivent portent une Clé. Retirez-la.` |
| SCN007 | Structure invalide | ligne fautive, sinon attribut | `Scénario de '{Methode}' mal structuré : {cause}.` — cause : `aucune ligne Scénario` / `plusieurs lignes Scénario` / `aucun ALORS` / `ET ou MAIS sans étape précédente` / `description vide après la Clé` |
| SCN008 | Ligne non reconnue | ligne | `Ligne non reconnue dans le Scénario de '{Methode}' : « {ligne} ». Mots-clés attendus : {liste de la langue détectée}.` |
| SCN009 | Langue mélangée | première ligne dans l'autre langue | `Le Scénario de '{Methode}' est en {langue} (d'après « {premier mot-clé} ») mais cette ligne est en {autre langue} : « {ligne} ». Une seule langue par Scénario.` |
| SCN010 | `Verify` hors d'une méthode `[TestScenario]` | appel | `ScenarioAssert.Verify est appelé dans '{Methode}', qui n'a pas d'attribut [TestScenario]. Ajoutez l'attribut ou retirez l'appel.` |
| SCN011 | Classe de test non `partial` | attribut | `La classe '{Classe}' doit être déclarée partial pour recevoir les Clés générées du Scénario de '{Methode}'.` |
| SCN012 | Scénario qui n'est pas un littéral | attribut | `Le Scénario de '{Methode}' doit être un littéral écrit dans l'attribut (raw string """…""" recommandé), pas une constante ni une expression.` |
| SCN013 | Surcharge de méthodes `[TestScenario]` | attribut | `Plusieurs méthodes '{Methode}' portent [TestScenario] dans '{Classe}'. Donnez à chacune un nom unique.` |

## 9. Contrat avec le skill

Le skill produit :
1. une classe de test `partial` ;
2. `[TestScenario("""…""")]` avec le Scénario de l'analyste recopié tel quel ;
3. une Clé sur chaque Résultat attendu, en PascalCase tirée de la description (convention recommandée, non imposée : seul SCN005 s'applique) ;
4. une Vérification `ScenarioAssert.Verify[Async](nameof({Methode}_Cles.{Cle}), …)` par Clé ;
5. la boucle « `dotnet build` → lire les `error SCNxxx` → corriger » jusqu'à zéro erreur.

Le composant promet : **zéro diagnostic SCN signifie que chaque Résultat attendu a au moins une Vérification** dans le corps de sa méthode de test.

## 10. Stratégie de test

- **Parser** : tests unitaires purs, un cas par règle de grammaire (section 4).
- **Générateur et analyzer** : tests en mémoire (`CSharpCompilation` + `GeneratorDriver` + `CompilationWithAnalyzers`), un cas par diagnostic vérifiant code, emplacement et message, et des cas pour le code généré. Sans `Microsoft.CodeAnalysis.Testing`.
- **Projet exemple** : compile sans aucun diagnostic ; sert de test de bout en bout du branchement par `ProjectReference`.

Pièges connus (prototype) : pas de `record` en `netstandard2.0` (CS0518) ; `dotnet build-server shutdown` après modification de l'analyzer ; `--no-incremental` pour prendre en compte un changement d'`.editorconfig`.

## 11. Hors périmètre

Générateur de squelette de test ; CodeFix ; paquet NuGet ; le skill lui-même ; détection d'écart avec le Gherkin d'origine ; analyse inter-procédurale ; constructions Gherkin listées en section 4.
