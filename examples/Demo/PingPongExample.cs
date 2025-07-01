using RestSharp;
using NBomber.CSharp;
using NBomber.RestSharp;

new PingPongExample().Run();

public class PingPongExample
{
    public void Run()
    {
        // For this example, you'll need to start the HttpApiSimulator, which is located in the examples/simulators solution folder.
        // Make sure it’s running before executing the client tests to ensure proper communication.

        var options = new RestClientOptions("http://localhost:5099");
        var client = new RestClient(options);

        var scenario = Scenario.Create("restsharp_scenario", async ctx =>
        {
            var request = new RestRequest("/api/pingpong/");
            return await client.Send(request);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();        
    }
}
