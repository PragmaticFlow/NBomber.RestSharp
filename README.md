# NBomber.RestSharp

[![build](https://github.com/PragmaticFlow/NBomber.RestSharp/actions/workflows/build.yml/badge.svg)](https://github.com/PragmaticFlow/NBomber.RestSharp/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/nbomber.RestSharp.svg)](https://www.nuget.org/packages/nbomber.RestSharp/)

NBomber plugin for writing HTTP scenarios with RestSharp client.

#### Documentation is located [here](https://nbomber.com/docs/protocols/http)

```csharp
var options = new RestClientOptions("http://localhost:5099");
using var client = RestClientBuilder.CreateDefaultClient(options);

var scenario = Scenario.Create("restsharp_scenario", async ctx =>
{
    var request = new RestRequest("/api/pingpong/");
    return await client.Send(request);
});
```