using RestSharp;
using NBomber.CSharp;
using NBomber.RestSharp;

new PingPongExample().Run();

public class PingPongExample
{
    public void Run()
    {
        var scenario = Scenario.Create("restsharp_scenario", async ctx =>
        {
            var options = new RestClientOptions("https://reqres.in");
            var client = new RestClient(options);
            var request = new RestRequest("/api/users/2");

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
