# 10: Générateur de Clés (+ SCN011, SCN013)

**What to build:** le générateur incrémental (`ForAttributeWithMetadataName`) émet `{Methode}_Cles` avec une constante par Clé valide, dans une déclaration partielle qui reproduit namespace et types englobants (spec §5). L'analyzer rapporte SCN011 (type non `partial`) et SCN013 (surcharge).

**Blocked by:** 08

**Status:** ready-for-agent

- [ ] `nameof({Methode}_Cles.{Cle})` compile dans la méthode de test
- [ ] Hint name `{NomDeMetadonnees}.{Methode}.g.cs` : deux classes de même nom dans deux namespaces avec une méthode de même nom donnent deux fichiers distincts, sans erreur
- [ ] Classes génériques, `record`, `struct`, `record struct` et types imbriqués reproduits correctement
- [ ] Les Clés valides sont émises même si le Scénario a d'autres défauts (SCN003 à SCN009)
- [ ] Rien n'est émis pour SCN011, SCN012 et SCN013
- [ ] SCN011 pointe le premier type non `partial` en partant de l'extérieur, avec la précision du message
- [ ] Le générateur ne rapporte aucun diagnostic
