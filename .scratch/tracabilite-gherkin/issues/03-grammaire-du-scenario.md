# Grammaire exacte du Scénario

Type: grilling
Status: resolved
Blocked by: (aucun)

## Question

Quelles sont les règles exactes que le parser applique au texte d'un Scénario ?

- Table des mots-clés FR/EN et variantes acceptées : casse, accents (`ÉTANT DONNÉ QUE` / `Etant donné que`), `ET QUE` vs `ET`, `EN TANT QUE`/`JE VEUX`/`AFIN DE` (narration ignorée ?), `Fonctionnalité :` avec ou sans espace avant les deux-points.
- Syntaxe d'une Clé : crochets collés au mot-clé (`ALORS[Cle]`) ou avec un espace, caractères autorisés (voir aussi la contrainte issue du ticket « Comment une Vérification référence-t-elle une Clé ? »).
- Quelles lignes sont des Résultats attendus : `ALORS`, et les `ET`/`MAIS` qui suivent un `ALORS` ; que faire d'une Clé posée sur une ligne `ÉTANT DONNÉ`/`LORSQUE` ?
- Lignes vides, indentation, commentaires `#`, lignes inconnues, plusieurs `Scénario` dans un même attribut.
- Ce qui rend un Scénario « mal formé » (pas d'`ALORS`, pas de `Scénario`, langue mélangée).

## Answer

- **Tolérance** : mots-clés insensibles à la casse et aux accents, espaces multiples réduits, espace facultatif avant `:`.
- **Mots-clés** — FR : `Fonctionnalité`, `Scénario`, `Soit` / `Étant donné [que|qu']`, `Quand` / `Lorsque` / `Lorsqu'`, `Alors`, `Et [que|qu']`, `Mais [que|qu']` ; EN : `Feature`, `Scenario`, `Given`, `When`, `Then`, `And`, `But`. `QUE` facultatif partout.
- **Narration** (`EN TANT QUE` / `JE VEUX` / `AFIN DE`, `As a` / `I want` / `So that`) : reconnue et ignorée, entre l'en-tête `Scénario` et la première étape ; ce même intervalle admet du texte libre.
- **Clé** : `MotClé[Cle] description` ou `MotClé [Cle] description` ; au moins un espace puis une description non vide (sinon SCN007).
- **Résultats attendus** : `ALORS` et chaque `ET`/`MAIS` qui le suit jusqu'à une étape d'un autre type ; plusieurs blocs `LORSQUE → ALORS` permis. Clé sur une autre étape → SCN006.
- **Lignes annexes** : lignes vides, indentation et commentaires `#` ignorés ; tableaux `| … |` acceptés et ignorés ; docstrings et toute autre ligne → SCN008.
- **Structure** : `Fonctionnalité` facultative ; exactement un `Scénario` ; au moins un `ALORS` ; `ET`/`MAIS` en première étape interdit (SCN007) ; langue mélangée → SCN009. Aucun ordre imposé entre `ÉTANT DONNÉ`, `LORSQUE` et `ALORS` au-delà.
- Le Scénario doit être un littéral écrit dans l'attribut (sinon SCN012).
