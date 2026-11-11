# 12: Assertions qui ne vérifient rien (SCN014 à SCN016)

**What to build:** l'analyzer refuse les trois formes d'assertion qui compilent mais ne vérifient rien (spec §6). Une Vérification fautive ne compte pas pour SCN001.

**Blocked by:** 11

**Status:** ready-for-agent

- [ ] SCN014 : lambda `async` passé à `Verify`
- [ ] SCN015 : `VerifyAsync` en instruction-expression ou affecté à `_`
- [ ] SCN015 non déclenché pour `await`, variable, `Task.WhenAll`, `return`
- [ ] SCN016 : `() => { }`, `async () => { }`, `() => Task.CompletedTask`
- [ ] Si l'argument `cle` est invalide (SCN002), `{Cle}` est remplacé par le texte de l'argument dans les messages
- [ ] Une Vérification refusée par SCN014, SCN015 ou SCN016 laisse la Clé sans Vérification : SCN001 est rapporté
