namespace Gherkin.Tracabilite.Exemple.Domaine;

public enum EtatTache
{
    Ouverte,
    Terminee,
}

public sealed record Tache(Guid Id, string Titre, EtatTache Etat)
{
    public static Tache Nouvelle(string titre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titre);
        return new Tache(Guid.NewGuid(), titre.Trim(), EtatTache.Ouverte);
    }
}
