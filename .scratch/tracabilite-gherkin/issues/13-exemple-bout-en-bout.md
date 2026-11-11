# 13: Exemple de bout en bout

**What to build:** le projet consommateur contient des Scénarios FR et EN réalistes dont chaque Clé a une vraie Vérification ; il sert de test du branchement par `ProjectReference` (spec §10).

**Blocked by:** 11, 12

**Status:** ready-for-agent

- [ ] `dotnet build` de la solution entière réussit sans aucun diagnostic SCN ni warning, y compris en `TreatWarningsAsErrors`
- [ ] Au moins un Scénario français, un anglais, un avec plusieurs blocs `LORSQUE → ALORS`, et un avec `VerifyAsync`
- [ ] Les tests de l'exemple s'exécutent et passent
- [ ] Retirer une Vérification de l'exemple fait échouer le build avec SCN001 (constaté une fois, non automatisé)
