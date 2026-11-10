namespace Demo;

public partial class TracabiliteTests
{
    [TestScenario("""
        Scénario: Enregistrer une tâche
          Soit une liste vide
          Quand j'ajoute "a"
          ALORS[TacheEnregistree] la liste contient 1 élément
          ALORS[TitreVaut] le titre vaut "a"
          ALORS une erreur est levée
        """)]
    public void EnregistrerTache()
    {
        Verifier(nameof(TacheEnregistree));
        Verifier(nameof(TitreVaut));
    }

    [TestScenario(
        """
        Scénario: Supprimer une tâche
          ALORS[TacheSupprimee] la liste est vide
        """)]
    public void SupprimerTache()
    {
        Verifier(nameof(TacheSupprimee));
    }

    private static void Verifier(string cle) => System.Console.WriteLine(cle);
}
