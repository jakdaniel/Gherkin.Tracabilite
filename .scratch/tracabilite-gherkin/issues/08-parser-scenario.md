# 08: Parser de Scénario (grammaire FR/EN)

**What to build:** le parser du texte d'un Scénario (spec §4), partagé par le générateur et l'analyzer. Il produit un modèle (lignes, mots-clés, langue, Résultats attendus, Clés, positions relatives au texte) et la liste typée des erreurs de grammaire, sans rapporter de diagnostic lui-même.

**Blocked by:** 07

**Status:** ready-for-agent

- [ ] Mots-clés FR et EN, insensibles à la casse et aux accents, espaces réduits, `:` avec ou sans espace, `QUE`/`QU'` facultatif, mot entier (`Exemples` ≠ `Exemple`)
- [ ] Langue du premier mot-clé non ambigu ; `Scenario` et `But` ambigus ; `But` en Scénario français = texte libre dans la narration
- [ ] Résultats attendus = `ALORS`/`DONC`/`THEN` et les `ET`/`MAIS`/`AND`/`BUT` qui les suivent ; plusieurs blocs permis
- [ ] Clé `Mot[Cle] desc` et `Mot [Cle] desc` ; validité, unicité, `{Methode}_Cles` interdit
- [ ] Narration, commentaires `#`, lignes vides, tableaux ignorés ; docstrings et lignes inconnues en erreur
- [ ] Structure : un seul `Scénario`, au moins un `ALORS`, pas de `ET`/`MAIS` en première étape, description non vide après la Clé
- [ ] Un test unitaire par règle de la section 4, dont les cas obligatoires de la section 10 concernant le parser
