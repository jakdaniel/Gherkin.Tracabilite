# Valider par un prototype les faits Roslyn non confirmés

Type: task
Status: resolved
Blocked by: 01

## Question

La recherche [Faisabilité Roslyn : lire le Scénario, localiser les diagnostics, symboles générés](01-faisabilite-roslyn.md) laisse quatre faits non confirmés. Monter une solution jetable minimale (hors du code final, dans `.scratch/tracabilite-gherkin/prototypes/`) avec un analyzer et un générateur `netstandard2.0`, consommés par `ProjectReference` depuis un projet `net10.0`, pour établir :

1. **Sortie CLI** : les diagnostics d'un analyzer branché par `ProjectReference` (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`) apparaissent-ils dans la sortie de `dotnet build`, et une sévérité `error` (par défaut ou via `.editorconfig`) fait-elle échouer le build ?
2. **Symboles générés** : une constante générée dans une classe de test `partial` est-elle résolue par `nameof(...)` dans la méthode de test, et l'analyzer voit-il ce symbole et la source générée dans la même compilation ?
3. **Localisation** : peut-on placer un diagnostic sur la ligne exacte d'un Résultat attendu dans un raw string literal `"""…"""` indenté, et l'emplacement affiché par `dotnet build` (fichier, ligne, colonne) est-il juste ?
4. **Code généré** : les fichiers `.g.cs` sont-ils exclus de l'analyse avec `GeneratedCodeAnalysisFlags.None` ?

La réponse consigne les faits constatés (versions du SDK et de Roslyn, extraits de la sortie de build) et l'emplacement du prototype. Pas de décision : c'est le rôle des tickets qui en dépendent.

## Answer

Prototype : `F:\Repos\Gherkin\.scratch\tracabilite-gherkin\prototypes\` (détails, extraits et variantes dans `prototypes\README.md`). Analyzer et générateur : `ScnProto.Analyzer` (netstandard2.0, Microsoft.CodeAnalysis.CSharp 5.6.0). Consommateur : `ScnProto.Consumer` (net10.0, C# 14, `ProjectReference` `OutputItemType="Analyzer"`). SDK 10.0.302, Roslyn du SDK `5.600.26.33009`.

1. **Sortie CLI : oui.** Les diagnostics apparaissent dans `dotnet build`, format `fichier(ligne,colonne): error SCN001: message [projet.csproj]`. Une sévérité `error` par défaut fait échouer le build (exit 1, `Build FAILED`). Un `.editorconfig` `dotnet_diagnostic.SCN001.severity = warning` le fait passer (exit 0). `dotnet_diagnostic.SCN002.severity = error` sur un descripteur `warning` le fait échouer. La sortie est en français sur cette machine, et chaque diagnostic est répété dans le résumé final.
2. **Symboles générés : oui.** `nameof(TacheEnregistree)` compile vers la constante `public const string` émise dans `obj\...\generated\...\TracabiliteTests.g.cs`. Le SemanticModel de l'analyzer résout l'argument (`symbole=Demo.TracabiliteTests.TacheEnregistree, kind=Field`), donne la valeur constante (`constante=TacheEnregistree`) et la déclaration dans le `.g.cs`.
3. **Localisation : oui, pour les deux formes.** Position calculée sur le texte brut du token, avec `Location.Create(tree, span)`. Les `ALORS` sont signalés en (9,11), (10,11), (22,11), (11,11), colonnes réelles du fichier, vérifiées par `grep -n`. Le cas `"""` sur la ligne de l'attribut (`EnregistrerTache`) et le cas `"""` sur sa propre ligne (`SupprimerTache`) donnent tous deux les bonnes positions.
4. **Code généré : `None` exclut le `.g.cs`.** Aucun diagnostic sur le `.g.cs` avec `GeneratedCodeAnalysisFlags.None` (ni SCN902 sur la déclaration, ni SCN903 sur l'arbre). Avec `ReportDiagnostics`, `warning SCN902` apparaît sur `TracabiliteTests.g.cs(2,22)`. Avec `Analyze`, rien n'est rapporté. Le `.g.cs` n'est donc pas rapporté à l'utilisateur sous `None`.

Pièges : `record` interdit sur netstandard2.0 (`IsExternalInit`, CS0518). Warnings `RS1036`, `RS2008`, `RS1026` sur l'analyzer lui-même, sans effet sur le build. Après modification de l'analyzer, `dotnet build-server shutdown` avant de builder (utilisé ici ; l'effet sans shutdown n'a pas été testé). Build avec `--no-incremental` pour prendre en compte un changement d'`.editorconfig`.
