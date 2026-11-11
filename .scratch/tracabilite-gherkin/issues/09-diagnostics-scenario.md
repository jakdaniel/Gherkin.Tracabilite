# 09: Diagnostics de Scénario (SCN003 à SCN009, SCN012)

**What to build:** l'analyzer lit le littéral de l'attribut `[TestScenario]`, applique le parser et rapporte les erreurs de Scénario avec les messages exacts de la spec §8, positionnées sur la ligne fautive du raw string (spec §7).

**Blocked by:** 08

**Status:** resolved

- [ ] SCN003, SCN004, SCN005, SCN006, SCN007, SCN008, SCN009 et SCN012 : code, emplacement et message vérifiés par tests en mémoire (`CSharpCompilation` + `CompilationWithAnalyzers`)
- [ ] Localisation exacte dans un raw string indenté, y compris avec `"""` sur la ligne de l'attribut
- [ ] Tous en `error` par défaut, catégorie `Tracabilite`, rétrogradables par `.editorconfig`
- [ ] `GeneratedCodeAnalysisFlags.None` : rien n'est rapporté sur les `.g.cs`
- [ ] `Scenario :` sans accent suivi d'étapes françaises ne déclenche pas SCN009

## Answer

Implémenté sur `integration/tracabilite-gherkin` (revue de code appliquée). Voir `git log`.
