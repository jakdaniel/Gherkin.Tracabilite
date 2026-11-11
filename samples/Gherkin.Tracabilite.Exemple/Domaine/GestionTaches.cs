namespace Gherkin.Tracabilite.Exemple.Domaine;

public sealed class GestionTaches
{
    private readonly List<Tache> _taches = [];

    public IReadOnlyList<Tache> Taches => _taches;

    public Tache Creer(string titre)
    {
        var tache = Tache.Nouvelle(titre);
        _taches.Add(tache);
        return tache;
    }

    public void Terminer(Guid id)
    {
        var index = _taches.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Aucune tâche d'identifiant {id}.");
        }

        _taches[index] = _taches[index] with { Etat = EtatTache.Terminee };
    }
}
