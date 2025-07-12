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

            var getStep = await Step.Run("get", ctx, async () =>
            {
                var response = await client.SendGet(request);

                if (response.Payload.Value.Content != "\"Get\"")
                    throw new ArgumentException();

                return response;
            });

            var postStep = await Step.Run("post", ctx, async () =>
            {
                var response = await client.SendPost(request);

                if (response.Payload.Value.Content != "\"Post\"")
                    throw new ArgumentException();

                return response;
            });

            var putStep = await Step.Run("put", ctx, async () =>
            {
                var response = await client.SendPut(request);

                if (response.Payload.Value.Content != "\"Put\"")
                    throw new ArgumentException();

                return response;
            });

            var patchStep = await Step.Run("patch", ctx, async () =>
            {
                var response = await client.SendPatch(request);

                if (response.Payload.Value.Content != "\"Patch\"")
                    throw new ArgumentException();

                return response;
            });

            var deleteStep = await Step.Run("delete", ctx, async () =>
            {
                var response = await client.SendDelete(request);

                if (response.Payload.Value.Content != "\"Delete\"")
                    throw new ArgumentException();

                return response;
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(2))
        .WithLoadSimulations(
            Simulation.KeepConstant(1, TimeSpan.FromSeconds(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        Assert.True(stats.AllOkCount > 0);
        Assert.True(stats.AllFailCount == 0);

        foreach (var scenarioStats in stats.ScenarioStats)
        {
            foreach (var stepStats in scenarioStats.StepStats)
            {
                Assert.True(stepStats.Ok.Latency.MinMs > 0);
                Assert.True(stepStats.Ok.StatusCodes.Length > 0);
            }
        }
    }
}
