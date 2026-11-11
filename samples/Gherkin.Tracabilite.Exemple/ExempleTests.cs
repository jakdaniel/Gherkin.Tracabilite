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
    }
}
