using RestSharp;
using NBomber.CSharp;
using NBomber.RestSharp;

namespace Tests.RestSharp;

public class RestSharpTest
{
    [Fact]
    public void EndToEnd()
    {
        // For this example, you'll need to start the HttpApiSimulator, which is located in the examples/simulators solution folder.
        // Make sure it’s running before executing the client tests to ensure proper communication.

        var scenario = Scenario.Create("restsharp_scenario", async ctx =>
        {
            var options = new RestClientOptions("http://localhost:5099");
            var client = new RestClient(options);
            var request = new RestRequest("/api/pingpong/");

            return await client.Send(request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.KeepConstant(1, TimeSpan.FromSeconds(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        Assert.True(stats.AllOkCount > 0);
        Assert.True(stats.AllFailCount == 0);

        foreach (var scenarioStats in stats.ScenarioStats)
        {
            foreach (var stepStats in scenarioStats.StepStats)
                Assert.True(stepStats.Ok.Latency.MinMs > 0);
        }
    }
}
