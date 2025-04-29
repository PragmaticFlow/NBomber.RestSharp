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
    static readonly int HttpVersionHeaderLength = 8;
    static readonly int HostHeaderLength = 4;
    static readonly int StatusCodeLength = 3;    

    public static async Task<Response<RestResponse>> Send(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request, client.Options.BaseUrl.Authority) + GetResponseSize(response);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: response, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: response, sizeBytes: sizeBytes);
    }

    public static async Task<Response<TResponse>> Send<TResponse>(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request, client.Options.BaseUrl.Authority) + GetResponseSize(response);

        JsonSerializerOptions options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        TResponse payload = JsonSerializer.Deserialize<TResponse>(response.Content, options);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: payload, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: payload, sizeBytes: sizeBytes);
    }

    public static async Task<Response<RestResponse>> SendGet(this RestClient client, RestRequest request)
    {
        request.Method = Method.Get;

        return await Send(client, request);
    }

    public static async Task<Response<TResponse>> SendGet<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Get;

        return await Send<TResponse>(client, request);
    }

    public static async Task<Response<RestResponse>> SendPost(this RestClient client, RestRequest request)
    {
        request.Method = Method.Post;

        return await Send(client, request);
    }

    public static async Task<Response<TResponse>> SendPost<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Post;

        return await Send<TResponse>(client, request);
    }

    public static async Task<Response<RestResponse>> SendPut(this RestClient client, RestRequest request)
    {
        request.Method = Method.Put;

        return await Send(client, request);
    }

    public static async Task<Response<TResponse>> SendPut<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Put;

        return await Send<TResponse>(client, request);
    }

    public static async Task<Response<RestResponse>> SendPatch(this RestClient client, RestRequest request)
    {
        request.Method = Method.Patch;

        return await Send(client, request);
    }

    public static async Task<Response<TResponse>> SendPatch<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Patch;

        return await Send<TResponse>(client, request);
    }

    public static async Task<Response<RestResponse>> SendDelete(this RestClient client, RestRequest request)
    {
        request.Method = Method.Delete;

        return await Send(client, request);
    }

    public static async Task<Response<TResponse>> SendDelete<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Delete;

        return await Send<TResponse>(client, request);
    }

    private static long GetRequestSize(RestRequest request, string host)
    {
        var sizeBytes = 0;

        sizeBytes += request.Parameters
            .Sum(p => Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value.ToString()) + CrlfLength);

        sizeBytes += Encoding.UTF8.GetByteCount(request.Method.ToString()) + SpaceLength + Encoding.UTF8.GetByteCount(request.Resource)
            + SpaceLength + HttpVersionHeaderLength + CrlfLength;

        sizeBytes += HostHeaderLength + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(host) + CrlfLength;

        sizeBytes += CrlfLength;

        return sizeBytes;
    }

    private static long GetResponseSize(RestResponse response)
    {
        var sizeBytes = 0;

        sizeBytes += response.Headers
            .Sum(p => Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value) + CrlfLength);

        sizeBytes += HttpVersionHeaderLength + SpaceLength + StatusCodeLength + SpaceLength + Encoding.UTF8.GetByteCount(response.StatusCode.ToString())
            + CrlfLength;

        sizeBytes += CrlfLength;

        sizeBytes += response.RawBytes?.Length ?? 0;

        return sizeBytes;
    }
}
