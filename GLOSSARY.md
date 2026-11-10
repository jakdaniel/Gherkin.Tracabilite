# Traçabilité Gherkin

Lien vérifiable, à la compilation, entre un scénario Gherkin rédigé par un analyste fonctionnel et le test automatisé qui le couvre.

## Language

**Scénario** :
Texte Gherkin (en français ou en anglais) attaché à une méthode de test, qui décrit un comportement attendu.
_Avoid_: feature, spec, cas de test

**Résultat attendu** :
Ligne `ALORS` / `THEN`, ou `ET` / `MAIS` / `AND` / `BUT` qui la suit, portant une Clé ; c'est l'unité de traçabilité.
_Avoid_: ExpectedResult, assertion attendue, critère

**Clé** :
Identifiant entre crochets qui nomme un Résultat attendu de façon unique dans son Scénario (ex. `TacheEnregistree`).
_Avoid_: clé fonctionnelle, tag, id

**Vérification** :
Appel dans le test qui déclare couvrir une Clé en lui associant une assertion.
_Avoid_: check, assert, verify (comme nom)

**Clés générées** :
Classe `{Methode}_Cles` produite par le générateur dans la classe de test, avec une constante par Clé du Scénario ; c'est la seule façon pour une Vérification de désigner une Clé.
_Avoid_: constantes, symboles générés, type de clés
