# Faisabilité Roslyn : lire le Scénario, localiser les diagnostics, symboles générés

Ticket : [01-faisabilite-roslyn.md](../issues/01-faisabilite-roslyn.md)

Ce document recense des faits sourcés et des incertitudes. Il ne prend aucune décision de conception.

## Sources utilisées

- [S1] Incremental generators (dotnet/roslyn) : https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
- [S2] Incremental generators, cookbook (dotnet/roslyn) : https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md
- [S3] Source generators, vue d'ensemble (Microsoft Learn) : https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
- [S4] Tutoriel : premier analyzer et code fix (Microsoft Learn) : https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
- [S5] Raw string literals, `"""` (Microsoft Learn) : https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string
- [S6] Configure code analysis rules (Microsoft Learn) : https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options
- [S7] Analyzers Roslyn, vue d'ensemble Visual Studio (Microsoft Learn) : https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview
- [S8] Analyzer Actions Semantics (dotnet/roslyn) : https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md
- [S9] Référencer un analyzer depuis un projet (meziantou.net, source secondaire) : https://meziantou.net/referencing-an-analyzer-from-a-project.htm
- [S10] Vérification locale : SDK .NET 10.0.302 installé, `C:\Program Files\dotnet\sdk\10.0.302\Roslyn\bincore\Microsoft.CodeAnalysis.CSharp.dll`, version de fichier `5.600.26.33009` (lecture directe, pas de prototype)

Pages non récupérées (HTTP 404 au moment de la recherche) : `Analyzer Configuration.md` et `Modifying analyzer behavior.md` (dotnet/roslyn), `Analyzers Architecture.md`, et `raw-string.md` côté dotnet/csharplang (la page Learn [S5] a été utilisée à sa place).

Aucun prototype n'a été réalisé.

---

## Q1. Un analyzer peut-il lire un raw string literal et localiser un diagnostic sur une ligne précise ?

**Lecture de la valeur.** Le tutoriel [S4] montre qu'un analyzer obtient la valeur constante d'une expression via `context.SemanticModel.GetConstantValue(expr, ct)`, qui renvoie un `Optional<object>`. Un argument d'attribut littéral (`"""..."""`) est une expression constante ; il suit donc ce chemin. Le texte d'attribut est accessible par la syntaxe (`LiteralExpressionSyntax`) ou par la constante sémantique. Le fait que la valeur soit une `string` avec `ValueText` / valeur constante est standard, mais la page [S4] ne le détaille pas pour les raw strings.

**Règles de valeur d'un raw string multi-ligne [S5].**
- Les délimiteurs d'ouverture et de fermeture doivent être seuls sur leur ligne (hors espaces).
- Les espaces situés à gauche des `"""` de fermeture sont retirés de **toutes** les lignes du littéral.
- Les espaces suivant l'ouverture sur la même ligne sont ignorés.
- Les lignes blanches (espaces seuls) sont incluses.
- Le saut de ligne qui précède les `"""` de fermeture n'est pas inclus dans la valeur.
- Si les espaces de fermeture sont mal alignés sur une ligne de contenu, le littéral est invalide (les espaces doivent correspondre à l'octet près : espace ≠ tabulation).

**Conséquence pour la localisation (inférence, non sourcée).** La valeur obtenue par le compilateur est donc le contenu sans indentation. Elle est alignée ligne à ligne sur les lignes de contenu du littéral source, mais les colonnes ne correspondent plus : la colonne dans la valeur n'est pas la colonne dans le fichier. Pour localiser une ligne de Résultat attendu, la voie la plus sûre est de partir du **texte brut du token** (`SyntaxToken.Text` du littéral), de compter les sauts de ligne pour trouver le décalage (`span.Start + offset`) de la ligne voulue, et de créer la `Location` avec ce `TextSpan` (`Location.Create(tree, span)`). Les raw strings n'ont pas d'échappement, donc le texte brut correspond caractère pour caractère au contenu, hors indentation. Cette voie est une inférence à partir de [S5] ; **elle n'est pas démontrée par une source primaire et doit être validée par prototype.**

**Diagnostic sur une ligne précise.** [S4] montre `context.ReportDiagnostic(Diagnostic.Create(Rule, location, ...))` avec une `Location` de nœud. Créer une `Location` sur un sous-intervalle est standard dans l'API Roslyn, mais aucune page de [S1] à [S8] ne le montre sur un littéral multi-ligne.

**Incertitudes.** Pas de prototype. Points à valider : (a) la correspondance numéro de ligne du Scénario / numéro de ligne du fichier, en particulier si le littéral commence sur la ligne de l'attribut ; (b) le comportement d'un `Location` sur une portion de token (affichage correct dans `dotnet build`).

---

## Q2. Un générateur peut-il produire des symboles utilisables par `nameof` ?

**Ce que disent les sources.**
- Recommandation : « We highly recommend that all generator authors that need to inspect syntax do so by using a marker attribute » ; `ForAttributeWithMetadataName` est « at least 99x more efficient » que `CreateSyntaxProvider` [S2].
- `AddSource(hintName, source)` ajoute un fichier ; la page [S2] précise que le nom doit inclure un chemin pour éviter les doublons.
- Les sources émises par un générateur sont ajoutées à la compilation que la phase standard observe ; un générateur ne voit pas les fichiers des autres générateurs [S1].
- Pour augmenter une classe utilisateur, la classe doit être `partial` [S2] (exemple : `public partial void UserMethod();`).
- Pour un type généré, [S2] recommande de faire écrire le code à l'utilisateur « as if the type was already present ». C'est le schéma le plus proche de notre cas : le test référence un symbole qui n'existe pas encore dans le source, et le générateur le fournit.
- `RegisterPostInitializationOutput` produit une source visible des autres transformations [S1].

**Conditions déduites (à valider).**
1. **Nommage déterministe** : les noms des symboles doivent se déduire uniquement du Scénario (les Clés). [S1] exige le déterminisme (« actively require this property to be true »).
2. **Identifiant C# valide** : une Clé doit être un identifiant C# pour devenir un membre ; sinon, pas de symbole. [S1]/[S2] ne traitent pas ce cas ; c'est une contrainte logique.
3. **Unicité par Scénario** : deux Clés identiques produisent deux membres du même nom, ce qui échoue à la compilation. Le composant doit gérer ce cas avant d'émettre.
4. **Type englobant** : si les symboles sont des membres de la classe de test, celle-ci doit être `partial` [S2]. Si les symboles sont dans un type séparé au niveau du namespace (ex. `Scenarios.TacheX.Cles.TacheEnregistree`), la classe de test n'a pas à être `partial`, mais le nom du type doit être connu de l'appelant (et donc être déterministe depuis le nom de la méthode ou de la classe).
5. **Lecture de l'argument** : [S2] montre la lecture d'arguments via `attribute.ConstructorArguments`, mais la section cite des arguments de type, pas de chaîne. Une chaîne se lit de la même façon (`TypedConstant.Value`), mais [S2] ne le dit pas explicitement.

**`nameof` et valeur constante.** `nameof(X)` s'évalue en `string` constante à la compilation. Un membre `public const string TacheEnregistree = "TacheEnregistree";` fournit une constante utilisable partout où une `string` constante est attendue. Cette forme est une inférence, non citée.

**Les analyzers voient-ils les sources générées dans la même compilation ?** Aucune page consultée ne le dit explicitement. [S1] indique que la sortie du générateur est ajoutée à la compilation observée par la phase standard. [S6] précise que les fichiers « generated code » (suffixes `.designer.cs` et `.generated.cs`, ou en-tête auto-généré) sont exclus des diagnostics d'analyse, et que `generated_code = true | false` permet de changer ce comportement. La règle s'applique aux diagnostics rapportés *dans* ces fichiers, pas à la visibilité des symboles. [S6] ne dit pas si `.g.cs` (fréquent pour les générateurs) entre dans cette liste. **Incertitude** : nous ne savons pas si l'analyzer peut résoudre `nameof(TacheEnregistree)` ou la constante générée au même moment. Il faut le valider par prototype.

**Attention au cascade d'erreurs (inférence).** Si le Scénario est mal formé, le générateur n'émet rien, et chaque `nameof` produit une erreur CS0103 en plus du diagnostic de l'analyzer. Le message de l'analyzer doit donc être le premier lu. Ce point est une inférence de conception, non sourcée.

---

## Q3. Configuration `.csproj` pour un analyzer référencé par `ProjectReference`

**Référencement.** Le schéma usuel est `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />` [S9, source secondaire ; une recherche a aussi confirmé ce schéma]. Le projet analyzer doit cibler `netstandard2.0` ; [S4] dit que « Analyzers should target .NET Standard 2.0 » car ils tournent en build en ligne de commande comme dans Visual Studio.

**Exécution pendant `dotnet build`.** Une source primaire établit que l'analyse de code est active par défaut pour les projets .NET 5+ [S6], et que les analyzers de la SDK se lancent au build. Pour un analyzer tiers référencé par `ProjectReference`, aucune source primaire consultée ne confirme qu'il s'exécute et que ses diagnostics sortent dans `dotnet build`. **[S7] contient une restriction préoccupante** : « Build-time errors and warnings from code analyzers are shown only if the analyzers are installed as a NuGet package. » Cette phrase vise les extensions VSIX, mais elle n'exclut pas le cas `ProjectReference`. **Point critique non tranché : à valider par prototype** (un petit projet analyzer + consommateur net10.0 + `dotnet build`).

**Sévérité.**
- Dans `.editorconfig` : `dotnet_diagnostic.<ID>.severity = error | warning | suggestion | silent | none | default` [S6].
- `error` : « Violations appear as build errors and cause builds to fail » [S6].
- Priorité : une entrée par identifiant de règle l'emporte sur une entrée par catégorie, qui l'emporte sur une entrée globale (`dotnet_analyzer_diagnostic.severity`) [S6].
- La sévérité par défaut vient du `DiagnosticDescriptor` de l'analyzer (non sourcé ici, connaissance générale).
- Le réglage `dotnet_analyzer_diagnostic.*` ne s'applique qu'aux règles activées par défaut [S6] : pour nos règles SCN, utiliser la forme `dotnet_diagnostic.SCNxxx.severity`.

**Sortie lisible par un agent.** [S7] montre une sortie de build en ligne de commande avec une violation d'analyzer (capture d'écran, pas de format texte). Le format attendu est `error SCN001: message [chemin.csproj]`, mais aucun document consulté ne le spécifie : **à vérifier par prototype.**

---

## Q4. Recommandations pour un analyzer moderne

**Actions d'analyse (sourcé).** [S4] utilise `RegisterSyntaxNodeAction(..., SyntaxKind.LocalDeclarationStatement)` et `RegisterSymbolAction(..., SymbolKind.NamedType)`. [S8] précise :
- une action syntaxique est invoquée « once per syntax node » pour les types de nœuds demandés ;
- une action de symbole est invoquée « once per complete semantic processing of a declaration of a type or type member » ;
- une action d'opération est invoquée « once per operation node » ;
- [S8] indique que les blocs d'opération sont recommandés pour l'analyse sémantique (« it is recommended that analyzers use Operation block »).

Pour notre cas (repérer des Vérifications dans le corps d'une méthode de test, sans analyse inter-procédurale), une action syntaxique sur les invocations `ScenarioAssert.Verify` puis une analyse sémantique du symbole appelé est cohérente avec [S4]. Le choix précis n'est pas tranché ici.

**Concurrence (sourcé pour l'essentiel).** [S8] : les actions ne s'exécutent pas en concurrence « unless » l'analyzer appelle `AnalysisContext.EnableConcurrentExecution` dans `Initialize`. L'appel est donc un opt-in ; il impose que les callbacks soient thread-safe. Aucune source primaire consultée ne le recommande ou le déconseille explicitement.

**`ConfigureGeneratedCodeAnalysis` / `GeneratedCodeAnalysisFlags`.** Non vérifié : la page primaire n'a pas été obtenue, et la recherche n'a donné que des sources secondaires. Les valeurs couramment citées (`None`, `Analyze`, `ReportDiagnostics`) sont à confirmer dans la référence API Microsoft.

**`EnforceExtendedAnalyzerRules`.** Non vérifié. Cette propriété MSBuild concerne les auteurs d'analyzers (paquet `Microsoft.CodeAnalysis.Analyzers`) ; aucune source consultée ne la décrit. À confirmer avant usage.

**Version de Roslyn à cibler.**
- Le SDK .NET 10.0.302 installé localement embarque Roslyn dans `sdk\10.0.302\Roslyn\bincore`, dont `Microsoft.CodeAnalysis.CSharp.dll` a pour version de fichier `5.600.26.33009` [S10].
- Une recherche a remonté un paquet `Microsoft.CodeAnalysis.Compilers 5.9.0` sur NuGet [recherche, non vérifiée].
- La bonne pratique « compiler l'analyzer contre une version de Roslyn au plus ancienne, pour rester compatible avec les hôtes plus récents » est une connaissance générale, **non sourcée ici**.
- Une recherche a suggéré `Microsoft.CodeAnalysis.CSharp 4.13.0` ; source secondaire, non retenue.

---

## Synthèse des incertitudes à lever par prototype

1. Un `Location` sur une ligne d'un raw string multi-ligne s'affiche-t-il au bon endroit dans `dotnet build` (Q1).
2. Un `ProjectReference` avec `OutputItemType="Analyzer"` produit-il des diagnostics visibles en sortie CLI, et avec quel format (Q3).
3. Un analyzer résout-il un symbole émis par un générateur dans la même compilation, pour `nameof` (Q2).
4. Un fichier `.g.cs` produit par un générateur est-il exclu des diagnostics d'analyzer (Q2, [S6]).
