using Gherkin.Tracabilite.Runtime;
using Xunit;

namespace Gherkin.Tracabilite.Tests;

public class ScenarioAssertTests
{
    [Fact]
    public void Verify_execute_l_assertion_une_seule_fois()
    {
        var appels = 0;

        ScenarioAssert.Verify("TacheEnregistree", () => appels++);

        Assert.Equal(1, appels);
    }

    [Fact]
    public void Verify_laisse_remonter_l_exception_telle_quelle()
    {
        var attendue = new InvalidOperationException("échec métier");

        var recue = Assert.Throws<InvalidOperationException>(
            () => ScenarioAssert.Verify("TacheEnregistree", () => throw attendue));

        Assert.Same(attendue, recue);
    }

    [Fact]
    public async Task VerifyAsync_attend_l_assertion_avant_de_rendre_la_main()
    {
        var terminee = false;

        await ScenarioAssert.VerifyAsync("TacheEnregistree", async () =>
        {
            await Task.Yield();
            terminee = true;
        });

        Assert.True(terminee);
    }

    [Fact]
    public async Task VerifyAsync_laisse_remonter_l_exception_telle_quelle()
    {
        var attendue = new InvalidOperationException("échec asynchrone");

        var recue = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ScenarioAssert.VerifyAsync("TacheEnregistree", async () =>
            {
                await Task.Yield();
                throw attendue;
            }));

        Assert.Same(attendue, recue);
    }
}
