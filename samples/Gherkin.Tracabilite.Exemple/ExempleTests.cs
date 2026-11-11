using Gherkin.Tracabilite.Exemple.Domaine;
using Gherkin.Tracabilite.Runtime;
using Xunit;

namespace Gherkin.Tracabilite.Exemple;

public partial class ExempleTests
{
    [TestScenario("""
        Fonctionnalité : Gestion des tâches
        Scénario : Créer une tâche
            En tant qu'utilisateur
            Soit une liste de tâches vide
            Quand l'utilisateur crée la tâche « Acheter du pain »
            Alors[TacheCreee] la tâche apparaît dans la liste
            Et[TacheOuverte] la tâche est ouverte
            Et[TitreNettoye] son titre est « Acheter du pain »
        """)]
    [Fact]
    public void CreerTache()
    {
        var gestion = new GestionTaches();

        var tache = gestion.Creer("  Acheter du pain  ");

        ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheCreee), () =>
            Assert.Contains(tache, gestion.Taches));
        ScenarioAssert.Verify(nameof(CreerTache_Cles.TacheOuverte), () =>
            Assert.Equal(EtatTache.Ouverte, tache.Etat));
        ScenarioAssert.Verify(nameof(CreerTache_Cles.TitreNettoye), () =>
            Assert.Equal("Acheter du pain", tache.Titre));
    }

    [TestScenario("""
        Fonctionnalité : Gestion des tâches
        Scénario : Terminer une tâche
            Soit une tâche ouverte « Relire le contrat »
            Quand l'utilisateur termine la tâche
            Alors[TacheTerminee] la tâche est marquée comme terminée
            Quand l'utilisateur termine une tâche inconnue
            Alors[TacheInconnueRefusee] l'opération est refusée et la liste ne change pas
        """)]
    [Fact]
    public void TerminerTache()
    {
        var gestion = new GestionTaches();
        var tache = gestion.Creer("Relire le contrat");

        gestion.Terminer(tache.Id);

        ScenarioAssert.Verify(nameof(TerminerTache_Cles.TacheTerminee), () =>
            Assert.Equal(EtatTache.Terminee, gestion.Taches.Single().Etat));

        var inconnue = Guid.NewGuid();

        ScenarioAssert.Verify(nameof(TerminerTache_Cles.TacheInconnueRefusee), () =>
        {
            Assert.Throws<KeyNotFoundException>(() => gestion.Terminer(inconnue));
            Assert.Equal(tache.Id, Assert.Single(gestion.Taches).Id);
        });
    }

    [TestScenario("""
        Feature: Task management
        Scenario: Complete an open task
            Given an open task "Review the contract"
            When the user marks the task as done
            Then[TaskMarkedDone] the task is in the done state
            And[ListUnchanged] the task list still contains only this task
        """)]
    [Fact]
    public void CompleteOpenTask()
    {
        var gestion = new GestionTaches();
        var tache = gestion.Creer("Review the contract");

        gestion.Terminer(tache.Id);

        ScenarioAssert.Verify(nameof(CompleteOpenTask_Cles.TaskMarkedDone), () =>
            Assert.Equal(EtatTache.Terminee, gestion.Taches.Single().Etat));
        ScenarioAssert.Verify(nameof(CompleteOpenTask_Cles.ListUnchanged), () =>
            Assert.Equal(tache.Id, Assert.Single(gestion.Taches).Id));
    }

    [TestScenario("""
        Scénario : Enregistrer une tâche dans le dépôt
            Soit un service de tâches avec un dépôt vide
            Quand l'utilisateur crée la tâche « Préparer la réunion »
            Alors[TachePersistee] la tâche est présente dans le dépôt
        """)]
    [Fact]
    public async Task EnregistrerTacheDansDepot()
    {
        var depot = new DepotTachesMemoire();
        var service = new ServiceTaches(depot);

        await service.CreerAsync("Préparer la réunion");

        await ScenarioAssert.VerifyAsync(nameof(EnregistrerTacheDansDepot_Cles.TachePersistee), async () =>
        {
            var taches = await depot.ObtenirToutesAsync();
            Assert.Equal("Préparer la réunion", Assert.Single(taches).Titre);
        });
    }
}
