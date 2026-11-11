namespace Gherkin.Tracabilite.Exemple.Domaine;

public interface IDepotTaches
{
    Task EnregistrerAsync(Tache tache);

    Task<IReadOnlyList<Tache>> ObtenirToutesAsync();
}

public sealed class DepotTachesMemoire : IDepotTaches
{
    private readonly List<Tache> _taches = [];

    public Task EnregistrerAsync(Tache tache)
    {
        _taches.Add(tache);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Tache>> ObtenirToutesAsync() =>
        Task.FromResult<IReadOnlyList<Tache>>(_taches.AsReadOnly());
}

public sealed class ServiceTaches(IDepotTaches depot)
{
    public async Task<Tache> CreerAsync(string titre)
    {
        var tache = Tache.Nouvelle(titre);
        await depot.EnregistrerAsync(tache);
        return tache;
    }
}
