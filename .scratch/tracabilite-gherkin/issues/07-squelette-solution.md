# 07: Squelette de solution et branchement ProjectReference

**What to build:** une solution .NET qui compile de bout en bout : `global.json` (SDK 10.0.302, `rollForward: latestFeature`), projets runtime (`net10.0`), analyzer (`netstandard2.0`, `Microsoft.CodeAnalysis.CSharp` 5.6.0, `PrivateAssets=all`), tests xUnit v3 et exemple consommateur. Le runtime expose `TestScenarioAttribute` et `ScenarioAssert.Verify`/`VerifyAsync` (spec §3). L'analyzer, vide pour l'instant, est branché par `ProjectReference` (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`) et respecte spec §2 : `EnforceExtendedAnalyzerRules`, `IsRoslynComponent`, `AnalyzerReleases.Shipped/Unshipped.md` en `AdditionalFiles`, `EnableConcurrentExecution()`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `dotnet build` de la solution réussit sans aucun warning, y compris RS1036, RS2008 et RS1026 sur l'analyzer
- [ ] L'exemple compile en `TreatWarningsAsErrors`
- [ ] `Verify` et `VerifyAsync` exécutent l'assertion et laissent remonter l'exception telle quelle (tests xUnit)
- [ ] Le projet de tests référence l'analyzer et le runtime
