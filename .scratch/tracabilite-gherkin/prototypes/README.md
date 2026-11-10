# Prototype Roslyn (ticket 06) : jetable

Solution minimale pour valider les faits de la recherche 01. Hors du code final. Rien n'est commité.

## Structure

- `ScnProto.Analyzer/` : `netstandard2.0`, `IsRoslynComponent=true`, `Microsoft.CodeAnalysis.CSharp 5.6.0` (`PrivateAssets=all`).
  - `ScnGenerator.cs` : `IIncrementalGenerator` sur `ForAttributeWithMetadataName("Demo.TestScenarioAttribute")`. Émet, par classe, `public const string Cle = "Cle";` pour chaque `ALORS[Cle]` du Scénario.
  - `ScnAnalyzer.cs` : `SCN001` (error par défaut, Résultat attendu sans Clé), `SCN002` (sonde, position ALORS[Clé]), `SCN901` (sonde `nameof`), `SCN902` (sonde déclaration de type), `SCN903` (sonde arbre de syntaxe). Constante `GeneratedFlag` (actuellement `GeneratedCodeAnalysisFlags.None`).
- `ScnProto.Consumer/` : `net10.0`, `LangVersion 14`, `EmitCompilerGeneratedFiles=true`, `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`.
  - `TestScenarioAttribute.cs` : attribut `(string scenario)` défini côté consommateur.
  - `TracabiliteTests.cs` : deux méthodes. `EnregistrerTache` a `"""` sur la ligne de l'attribut ; `SupprimerTache` a `"""` sur sa propre ligne. `ALORS une erreur est levée` (sans Clé) est volontaire.

## Commandes

```
cd prototypes/ScnProto.Consumer
dotnet build --no-incremental
```

`--no-incremental` pour que l'`.editorconfig` ou le code de l'analyzer soit pris en compte. Après modification de l'analyzer, `dotnet build-server shutdown` avant de builder (le serveur VBCSCompiler garde les analyzers chargés ; le changement de flag Q4 a bien été observé après shutdown).

## Versions constatées

- SDK : `10.0.302` (`C:\Program Files\dotnet\sdk\10.0.302`).
- Roslyn du SDK : `Microsoft.CodeAnalysis.CSharp.dll`, FileVersion `5.600.26.33009`.
- Package analyzer : `Microsoft.CodeAnalysis.CSharp 5.6.0` (sur NuGet : 5.0.0, 5.3.0, 5.6.0, 5.9.0), FileVersion `5.600.26.26310`. Même ligne 5.6, build différent du SDK.
- Locale : sortie de build en français (`Erreur(s)`, `Avertissement(s)`, `Temps écoulé`).

## Pièges constatés

- Warnings de l'analyzer lui-même, bruyants mais sans effet : `RS1036` (EnforceExtendedAnalyzerRules), `RS2008` (release tracking), `RS1026` (concurrent execution). Non traités dans le prototype.
- `IsExternalInit` absent sur `netstandard2.0` : pas de `record` dans le générateur (CS0518). Utiliser une classe.
- Le message de build est répété dans le résumé final de MSBuild : chaque diagnostic apparaît deux fois.
- Le générateur déduplique les Clés par `Distinct()` au sein d'une classe. Choix de prototype, pas une décision.

## Extraits de sortie (build par défaut, aucun `.editorconfig`)

```
TracabiliteTests.cs(11,11): error SCN001: Résultat attendu sans Clé : « ALORS une erreur est levée » (ajouter [Clé] après ALORS) [...ScnProto.Consumer.csproj]
TracabiliteTests.cs(9,11): warning SCN002: SONDE ALORS[TacheEnregistree] : ligne 9, colonne 11, fichier ...\TracabiliteTests.cs [...]
TracabiliteTests.cs(15,18): warning SCN901: SONDE nameof(TacheEnregistree) : symbole=Demo.TracabiliteTests.TacheEnregistree, kind=Field, reason=None, constante=TacheEnregistree, declare dans=...\obj\Debug\net10.0\generated\ScnProto.Analyzer\ScnProto.Analyzer.ScnGenerator\TracabiliteTests.g.cs [...]
```

Build final : `EXIT=1` (SCN001 en error), `3 Erreur(s)` au run précédent avec `SCN002=error`.

## Variantes testées

| Variante | Résultat |
|---|---|
| Défaut (SCN001 error, pas d'editorconfig) | exit 1, `error SCN001` sur (11,11) |
| `.editorconfig` `dotnet_diagnostic.SCN001.severity = warning` | exit 0, `warning SCN001` sur (11,11) |
| `.editorconfig` SCN001=warning + SCN002=error | exit 1, `error SCN002` sur (9,11), (10,11), (22,11) |
| `GeneratedCodeAnalysisFlags.Analyze` | aucun diagnostic sur `.g.cs` (SCN902 et SCN903) |
| `GeneratedCodeAnalysisFlags.ReportDiagnostics` | `warning SCN902` sur `obj\...\TracabiliteTests.g.cs(2,22)` |
| `GeneratedCodeAnalysisFlags.None` (retenu) | aucun diagnostic sur `.g.cs` |

Le fichier `.editorconfig` de test a été supprimé après chaque variante. Les répertoires `bin/` et `obj/` restent (jetables).
