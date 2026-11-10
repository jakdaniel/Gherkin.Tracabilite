# Catalogue des diagnostics

Type: grilling
Status: resolved
Blocked by: 02, 03, 06

## Question

Quelle est la liste définitive des diagnostics, avec pour chacun : code, sévérité par défaut, condition de déclenchement, emplacement (méthode, ligne du Scénario, appel `Verify`) et **message exact**, auto-suffisant pour un agent qui lit la sortie de `dotnet build` ?

Candidats : SCN001 Clé non vérifiée ; SCN002 Vérification d'une Clé absente du Scénario ; Résultat attendu sans Clé ; Clé en double ; langue mélangée ; Scénario mal formé ; Clé non constante dans `Verify` ; `Verify` hors d'une méthode `[TestScenario]`.

## Answer

Tous en `error` par défaut, catégorie `Tracabilite`, réglables par `dotnet_diagnostic.SCNxxx.severity`. Seul l'analyzer rapporte ; messages en français, qui disent quoi, où et comment corriger.

| Code | Condition | Emplacement | Message |
|---|---|---|---|
| SCN001 | Clé du Scénario sans Vérification | ligne du Résultat attendu | `Le Résultat attendu [{Cle}] (« {Description} ») du Scénario de '{Classe}.{Methode}' n'a aucune Vérification. Ajoutez dans la méthode : ScenarioAssert.Verify(nameof({Methode}_Cles.{Cle}), () => …);` |
| SCN002 | Argument de `Verify` qui n'est pas une Clé générée de ce Scénario | argument | `L'argument de Verify doit être une Clé du Scénario de '{Methode}', écrite nameof({Methode}_Cles.<Clé>). Reçu : {argument} ({cause}). Clés disponibles : {liste}.` (cause : chaîne littérale / Clé d'un autre Scénario / valeur non constante) |
| SCN003 | Résultat attendu sans Clé | ligne | `Le Résultat attendu « {ligne} » n'a pas de Clé. Ajoutez un identifiant entre crochets après le mot-clé, par exemple : {MotCle}[NomDeLaCle] {Description}.` |
| SCN004 | Clé en double | ligne de la seconde | `La Clé [{Cle}] apparaît plusieurs fois dans le Scénario de '{Methode}'. Chaque Clé doit être unique : renommez celle-ci.` |
| SCN005 | Clé invalide | ligne | `La Clé [{Cle}] n'est pas un identifiant C# valide ou est un mot réservé. Utilisez lettres, chiffres et _, sans commencer par un chiffre.` |
| SCN006 | Clé sur une étape qui n'est pas un Résultat attendu | ligne | `La Clé [{Cle}] est posée sur une étape {MotCle} : seules les lignes ALORS/THEN et les ET/MAIS/AND/BUT qui les suivent portent une Clé. Retirez-la.` |
| SCN007 | Structure invalide | ligne fautive, sinon attribut | `Scénario de '{Methode}' mal structuré : {cause}.` (aucune ligne Scénario / plusieurs lignes Scénario / aucun ALORS / ET ou MAIS sans étape précédente / description vide après la Clé) |
| SCN008 | Ligne non reconnue | ligne | `Ligne non reconnue dans le Scénario de '{Methode}' : « {ligne} ». Mots-clés attendus : {liste de la langue détectée}.` |
| SCN009 | Langue mélangée | première ligne dans l'autre langue | `Le Scénario de '{Methode}' est en {langue} (d'après « {premier mot-clé} ») mais cette ligne est en {autre langue} : « {ligne} ». Une seule langue par Scénario.` |
| SCN010 | `Verify` hors d'une méthode `[TestScenario]` | appel | `ScenarioAssert.Verify est appelé dans '{Methode}', qui n'a pas d'attribut [TestScenario]. Ajoutez l'attribut ou retirez l'appel.` |
| SCN011 | Classe de test non `partial` | attribut | `La classe '{Classe}' doit être déclarée partial pour recevoir les Clés générées du Scénario de '{Methode}'.` |
| SCN012 | Scénario qui n'est pas un littéral | attribut | `Le Scénario de '{Methode}' doit être un littéral écrit dans l'attribut (raw string """…""" recommandé), pas une constante ni une expression.` |
| SCN013 | Surcharge de méthodes `[TestScenario]` | attribut | `Plusieurs méthodes '{Methode}' portent [TestScenario] dans '{Classe}'. Donnez à chacune un nom unique.` |

Les Vérifications comptent dans tout le corps de la méthode : lambdas, `if`, boucles et fonctions locales (sans vérifier que la fonction locale est appelée).
