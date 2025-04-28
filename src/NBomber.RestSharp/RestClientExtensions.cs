using System.Text.Json;
using System.Text;
using RestSharp;
using NBomber.Contracts;
using NBomber.CSharp;

namespace NBomber.RestSharp;

public static class RestClientExtensions
{
    static readonly int HeaderSeparatorLength = 2;
    static readonly int CrlfLength = 2;
    static readonly int SpaceLength = 1;
    static readonly int HttpPartLength = 8;
    static readonly int StatusCodeLength = 3;

    public static async Task<Response<RestResponse>> Send(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request) + GetResponseSize(response);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: response, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: response, sizeBytes: sizeBytes);
    }

    public static async Task<Response<TResponse>> Send<TResponse>(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request) + GetResponseSize(response);

        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        TResponse payload = JsonSerializer.Deserialize<TResponse>(response.Content, options);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: payload, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: payload, sizeBytes: sizeBytes);
    }

    private static long GetRequestSize(RestRequest request)
    {
        var sizeBytes = 0;

        sizeBytes += request.Parameters
            .Sum(p => Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value.ToString()) + CrlfLength);

        sizeBytes += Encoding.UTF8.GetByteCount(request.Method.ToString()) + SpaceLength + Encoding.UTF8.GetByteCount(request.Resource)
            + SpaceLength + HttpPartLength + CrlfLength;

        sizeBytes += CrlfLength;

        return sizeBytes;
    }

    private static long GetResponseSize(RestResponse response)
    {
        var sizeBytes = 0;

        sizeBytes += response.Headers
            .Sum(p => Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value) + CrlfLength);

        sizeBytes += HttpPartLength + SpaceLength + StatusCodeLength + SpaceLength + Encoding.UTF8.GetByteCount(response.StatusCode.ToString())
            + CrlfLength;

        sizeBytes += CrlfLength;

        sizeBytes += response.RawBytes?.Length ?? 0;

        return sizeBytes;
    }
}
