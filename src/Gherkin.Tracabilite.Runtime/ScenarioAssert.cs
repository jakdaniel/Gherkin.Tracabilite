namespace Gherkin.Tracabilite.Runtime;

public static class ScenarioAssert
{
    // Exécute l'assertion et rien d'autre : l'exception remonte telle quelle.
    public static void Verify(string cle, Action assertion) => assertion();

    public static Task VerifyAsync(string cle, Func<Task> assertion) => assertion();
}
