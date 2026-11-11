using Gherkin.Tracabilite.Runtime;

namespace Gherkin.Tracabilite.Exemple;

public partial class ExempleTests
{
    [TestScenario("""
        Scénario : Enregistrer une tâche
            Soit une tâche à créer
            Quand la tâche est enregistrée
            Alors[TacheEnregistree] la tâche est enregistrée
        """)]
    public void EnregistrerTache()
    {
        var taches = new List<string>();
        taches.Add("Tâche à créer");

        ScenarioAssert.Verify(nameof(EnregistrerTache_Cles.TacheEnregistree), () =>
        {
            if (taches.Count != 1)
            {
                throw new InvalidOperationException("La tâche n'est pas enregistrée.");
            }
        });
    }
}
