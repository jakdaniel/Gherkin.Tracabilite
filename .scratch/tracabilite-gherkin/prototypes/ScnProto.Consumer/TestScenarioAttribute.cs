namespace Demo;

[System.AttributeUsage(System.AttributeTargets.Method)]
public sealed class TestScenarioAttribute(string scenario) : System.Attribute
{
    public string Scenario { get; } = scenario;
}
