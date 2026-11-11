; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SCN003 | Tracabilite | Error | Résultat attendu sans Clé
SCN004 | Tracabilite | Error | Clé en double dans un Scénario
SCN005 | Tracabilite | Error | Clé invalide
SCN006 | Tracabilite | Error | Clé sur une étape qui n'est pas un Résultat attendu
SCN007 | Tracabilite | Error | Structure du Scénario invalide
SCN008 | Tracabilite | Error | Ligne non reconnue dans un Scénario
SCN009 | Tracabilite | Error | Langue mélangée dans un Scénario
SCN012 | Tracabilite | Error | Scénario qui n'est pas un littéral
SCN011 | Tracabilite | Error | Classe de test ou type englobant non partial
SCN013 | Tracabilite | Error | Surcharge de méthode [TestScenario]
SCN001 | Tracabilite | Error | Résultat attendu sans Vérification
SCN002 | Tracabilite | Error | Argument de Verify qui n'est pas une Clé du Scénario
SCN010 | Tracabilite | Error | Vérification hors d'une méthode [TestScenario]
