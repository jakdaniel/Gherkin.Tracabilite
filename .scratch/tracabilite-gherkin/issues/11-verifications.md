# 11: Vérifications (SCN001, SCN002, SCN010)

**What to build:** l'analyzer détecte les appels `Verify`/`VerifyAsync` dans le corps d'une méthode `[TestScenario]` (lambdas, `if`, boucles, fonctions locales) et contrôle la couverture des Clés (spec §6 et §8).

**Blocked by:** 09, 10

**Status:** ready-for-agent

- [ ] SCN001 sur la ligne du Résultat attendu dont la Clé n'a aucune Vérification
- [ ] SCN002 : chaîne littérale, Clé d'un autre Scénario ou valeur non constante, avec la cause et la liste des Clés disponibles dans le message
- [ ] SCN010 : `Verify`/`VerifyAsync` dans une méthode sans `[TestScenario]`
- [ ] Plusieurs Vérifications d'une même Clé acceptées
- [ ] `nameof(...)` et constante directe acceptés pour l'argument `cle`
- [ ] Tests en mémoire couvrant code, emplacement et message de chaque diagnostic
