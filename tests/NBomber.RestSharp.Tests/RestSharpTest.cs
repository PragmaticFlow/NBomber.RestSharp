using RestSharp;
using NBomber.CSharp;
using NBomber.RestSharp;

namespace Tests.RestSharp;

public class RestSharpTest
{
    [Fact]
    public void EndToEnd()
    {
        var scenario = Scenario.Create("restsharp_scenario", async ctx =>
        {
            var options = new RestClientOptions("https://reqres.in");
            var client = new RestClient(options);
            var request = new RestRequest("/api/users/2");

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
