# Comportement de ScenarioAssert.Verify à l'exécution

Type: grilling
Status: resolved
Blocked by: 02

## Question

Que fait `ScenarioAssert.Verify` quand le test s'exécute ?

- Exécuter seulement l'assertion (simple ancre statique pour l'analyzer) ?
- Enrichir l'échec avec la Clé et la description du Résultat attendu ? Comment `Verify` retrouve-t-il cette description : données générées, réflexion sur l'attribut de la méthode appelante, ou description passée en argument ?
- S'arrêter au premier échec, ou continuer et rapporter tous les Résultats attendus en échec (soft assertions) ?
- Prise en charge des assertions `async` (`Func<Task>`) ?

## Answer

- `ScenarioAssert.Verify(string cle, Action assertion)` exécute l'assertion, et c'est tout : l'exception remonte telle quelle, sans enveloppe ni enrichissement (Clé, description). Envelopper changerait la classification de l'échec par certains frameworks (agnosticisme) ; la pile d'appels pointe déjà la ligne `nameof(…_Cles.Cle)`.
- Arrêt au premier échec ; pas d'assertions cumulées (elles exigeraient une portée propre au framework).
- Async : méthode distincte `Task VerifyAsync(string cle, Func<Task> assertion)`, reconnue par l'analyzer comme `Verify`.
