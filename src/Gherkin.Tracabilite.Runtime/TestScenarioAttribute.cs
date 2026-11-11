namespace Gherkin.Tracabilite.Runtime;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestScenarioAttribute(string scenario) : Attribute
{
    public string Scenario { get; } = scenario;
}
