# Comment une Vérification référence-t-elle une Clé ?

Type: grilling
Status: resolved
Blocked by: 01, 06

## Question

Dans le README, `ScenarioAssert.Verify(nameof(TacheEnregistree), …)` ne compile pas faute de symbole `TacheEnregistree`. Comment une Vérification doit-elle désigner sa Clé ?

- (a) Un générateur produit des constantes à partir du Scénario (`nameof`/constante, sûr au refactoring, autocomplétion), ce qui ramène un générateur dans le périmètre et peut imposer des contraintes (classe `partial`…).
- (b) Des chaînes littérales `"TacheEnregistree"`, validées par l'analyzer seul.
- (c) Des constantes déclarées à la main.

Doit aussi trancher : que faire d'une Clé passée de façon non constante (variable, expression) ? Et quelles contraintes de forme pèsent sur une Clé (identifiant C# valide ?) ? Tenir compte du fait que c'est un agent qui écrit le test.

## Answer

- **Option (a)** : un générateur émet, pour chaque méthode `[TestScenario]`, `public static class {Methode}_Cles { public const string {Cle} = "{Cle}"; }` imbriqué dans la classe de test (fichier `{Classe}.{Methode}.g.cs`). Le nom `{Methode}` seul est impossible (CS0102 avec la méthode).
- La classe de test doit être `partial` (sinon SCN011). Deux méthodes `[TestScenario]` de même nom dans une classe → SCN013, rien n'est généré pour elles.
- Une Vérification s'écrit `ScenarioAssert.Verify(nameof({Methode}_Cles.{Cle}), …)` (ou la constante directement).
- `Verify` n'accepte que les constantes du type `_Cles` de **sa** méthode : chaîne littérale, Clé d'un autre Scénario ou valeur non constante → SCN002.
- Forme d'une Clé : identifiant C# valide (Unicode permis), sans `@` ni mot réservé (sinon SCN005), comparée en tenant compte de la casse.
- Le générateur émet les Clés valides même si le Scénario a d'autres défauts (SCN003 à SCN009) ; il n'émet rien seulement dans les cas SCN011 à SCN013.
