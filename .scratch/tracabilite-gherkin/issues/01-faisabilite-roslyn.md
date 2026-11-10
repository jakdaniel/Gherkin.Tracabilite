# Faisabilité Roslyn : lire le Scénario, localiser les diagnostics, symboles générés

Type: research
Status: resolved
Blocked by: (aucun)

## Question

Quels faits Roslyn (versions actuelles, analyzer `netstandard2.0` consommé par un projet `net10.0` / C# 14 via `ProjectReference`) conditionnent les décisions de cette carte ?

1. Un `DiagnosticAnalyzer` peut-il lire la valeur constante d'un argument d'attribut écrit en **raw string literal** multi-ligne (`"""…"""`) et **localiser un diagnostic sur une ligne précise** de ce littéral (la ligne d'un Résultat attendu), y compris compte tenu de l'indentation retirée par les raw strings ?
2. Un `IIncrementalGenerator` peut-il générer, à partir de l'argument de l'attribut, des **symboles (constantes, membres imbriqués) utilisables par `nameof(...)` ou comme constante** dans la méthode de test ? À quelles conditions (classe `partial`, type au niveau du namespace, nom déterministe) ? Les analyzers voient-ils les sources générées dans la même compilation ?
3. Que faut-il dans le `.csproj` consommateur pour qu'un analyzer (et éventuellement un générateur) référencé par `ProjectReference` s'exécute pendant `dotnet build`, et que ses diagnostics apparaissent dans la sortie CLI (lisible par un agent) avec le bon niveau de sévérité ?
4. Recommandations actuelles pour un analyzer « moderne » : `RegisterSymbolAction` vs `RegisterOperationAction`, concurrence, `EnforceExtendedAnalyzerRules`, versions de `Microsoft.CodeAnalysis` à cibler pour le SDK .NET 10.

Résultats attendus : des faits sourcés (documentation officielle, dépôt dotnet/roslyn), pas des décisions.

## Answer

- **Lecture et localisation (Q1)** : la valeur d'un raw string est dépourvue d'indentation ; le compilateur ne donne pas les colonnes source. Localiser une ligne se fait à partir du texte brut du token (décalage par comptage des sauts de ligne). C'est une inférence, non démontrée par une source primaire.
- **Symboles générés (Q2)** : un générateur peut fournir des symboles pour `nameof` si les noms sont déterministes, valides en C#, uniques par Scénario, et si le type cible est `partial` lorsqu'on augmente la classe de test. Les analyzers voient-ils ces symboles ? Non tranché par les sources.
- **Build (Q3)** : `ProjectReference` avec `OutputItemType="Analyzer"` et `ReferenceOutputAssembly="false"`, cible `netstandard2.0`. Sévérité via `dotnet_diagnostic.<ID>.severity`. Point critique non résolu : Microsoft indique que les diagnostics de build ne sont affichés que pour les analyzers en paquet NuGet ; à valider.
- **Recommandations (Q4)** : actions syntaxiques ou d'opération (opérations recommandées pour le sémantique) ; `EnableConcurrentExecution` est un opt-in. `EnforceExtendedAnalyzerRules` et `GeneratedCodeAnalysisFlags` non vérifiés. SDK 10.0.302 local : Roslyn 5.600.26.33009.
- **Incertitudes** : aucun prototype ; quatre points à valider (localisation dans un raw string, sortie CLI d'un ProjectReference, résolution des symboles générés, exclusion des `.g.cs`).
- Détail, sources et incertitudes : [research/01-faisabilite-roslyn.md](../research/01-faisabilite-roslyn.md)
