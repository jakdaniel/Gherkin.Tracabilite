# README d'origine (transcrit des captures d'écran)

Source brute de l'idée, conservée telle quelle pour référence. Les décisions de la carte priment sur ce texte (voir `map.md`).

---

Je veux créer une bibliothèque .NET 10 composée d'un Attribut personnalisé, d'un Source Generator Roslyn et d'un Analyzer Roslyn.
L'objectif n'est PAS de créer un framework BDD comme Reqnroll ou SpecFlow.
L'objectif est de permettre la traçabilité entre un scénario Gherkin fourni par un analyste fonctionnel et un test xUnit, tout en validant à la compilation que tous les résultats attendus du scénario sont couverts par le test.

## Exemple d'utilisation

```csharp
[TestScenario("""
Fonctionnalité : Tâche
Scénario : Création d'une tâche avec les seules informations obligatoires
EN TANT QUE aiguilleur
JE VEUX créer une tâche en ne fournissant que les informations obligatoires
AFIN DE bénéficier de la politique de reprise standard sans avoir à la préciser
ÉTANT DONNÉ QUE la communication « C-101 » existe dans le dépôt des communications
ET QUE je fournis uniquement l'IDCom, le Worker « IGDE » et les Parametres
ET QUE le NbIterationRestante, le NbErreursMax et le DelaiEntreErreurs ne sont pas fournis
LORSQUE je demande la création de la tâche
ALORS[TacheEnregistree] la tâche est enregistrée dans le dépôt des tâches
ET[NbIterationRestanteParDefaut] le NbIterationRestante enregistré est 5
ET[NbErreursMaxParDefaut] le NbErreursMax enregistré est 5
ET[DelaiEntreErreursParDefaut] le DelaiEntreErreurs enregistré est 10
ET[StatutAFaire] le Statut de la tâche est « AFAIRE »
ET[IdTacheRetourne] l'IDTache est retourné à l'appelant
""")]
[Fact]
public async Task CreerTache_AvecInformationsObligatoires()
{
    ScenarioAssert.Verify(nameof(TacheEnregistree),
        () => tache.Should().NotBeNull());
    ScenarioAssert.Verify(nameof(NbIterationRestanteParDefaut),
        () => tache.NbIterationRestante.Should().Be(5));
    ScenarioAssert.Verify(nameof(NbErreursMaxParDefaut),
        () => tache.NbErreursMax.Should().Be(5));
    ScenarioAssert.Verify(nameof(DelaiEntreErreursParDefaut),
        () => tache.DelaiEntreErreurs.Should().Be(10));
    ScenarioAssert.Verify(nameof(StatutAFaire),
        () => tache.Statut.Should().Be(StatutTache.AFaire));
    ScenarioAssert.Verify(nameof(IdTacheRetourne),
        () => response.IdTache.Should().NotBeEmpty());
}
```

# Exigences

## Attribut

Créer un attribut : `[TestScenario(string scenario)]`. L'attribut doit stocker le texte complet du scénario.

## Parsing

Le parser doit extraire : Fonctionnalité, Scénario, ÉTANT DONNÉ QUE, ET QUE, LORSQUE, ALORS, ET.
Pour les clauses ALORS et ET qui suivent un ALORS, extraire : la clé fonctionnelle entre crochets, la description textuelle.

Exemple : `ALORS[TacheEnregistree] la tâche est enregistrée` devient
`ExpectedResult { Key = "TacheEnregistree", Description = "la tâche est enregistrée" }`.

## ScenarioAssert

Créer une API : `ScenarioAssert.Verify(string key, Action assertion)`.
Cette méthode exécute simplement l'assertion. L'intérêt principal est de fournir un point d'ancrage statique pour l'analyse Roslyn.

## Analyzer Roslyn

Créer un analyzer qui :
1. Localise les méthodes de test ayant l'attribut TestScenario.
2. Parse le scénario.
3. Récupère toutes les clés attendues.
4. Analyse le corps du test.
5. Récupère toutes les clés utilisées dans ScenarioAssert.Verify.

### Diagnostic SCN001
Déclencher si une clé présente dans le scénario n'est pas vérifiée.
`SCN001 Le résultat attendu 'IdTacheRetourne' est défini dans le scénario mais n'est pas vérifié dans le test.`

### Diagnostic SCN002
Déclencher si une clé est vérifiée dans le test mais absente du scénario.
`SCN002 Le résultat 'DateCreationValidee' est vérifié dans le test mais n'existe pas dans le scénario.`

## Code Fix
Ajouter un CodeFix pour SCN001 qui génère automatiquement
`ScenarioAssert.Verify(nameof(IdTacheRetourne), () => throw new NotImplementedException());`
à l'emplacement approprié dans le test.

## Source Generator
Créer un générateur optionnel capable de générer un squelette de test à partir du scénario
(`[TestScenario("...")] public partial Task X();` → méthode `[Fact]` avec commentaires Arrange/Given/Act/When/Assert et un `Verify(..., () => throw new NotImplementedException())` par clé).

## Contraintes
- C# 13
- .NET 9
- xUnit v3
- Incremental Generator
- Analyzer Roslyn moderne
- Aucune dépendance à SpecFlow, Reqnroll ou Gherkin officiel
- Architecture simple et maintenable
- Tests unitaires complets pour le parser, l'analyzer et le source generator

Fournir l'arborescence complète du projet, le code de production et les tests unitaires.
