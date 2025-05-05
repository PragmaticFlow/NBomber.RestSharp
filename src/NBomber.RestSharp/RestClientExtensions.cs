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

    /// <summary>
    /// Sends an asynchronous HTTP request using the specified <see cref="RestRequest"/>,
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// </summary>
    public static async Task<Response<RestResponse>> Send(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request, client.Options.BaseUrl.Authority) + GetResponseSize(response);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: response, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: response, sizeBytes: sizeBytes);
    }

    /// <summary>
    /// Sends an asynchronous HTTP request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response and wraps it
    /// in a custom <see cref="Response{T}"/> object, including size and latency metrics.
    /// </summary>
    public static async Task<Response<TResponse>> Send<TResponse>(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var sizeBytes = GetRequestSize(request, client.Options.BaseUrl.Authority) + GetResponseSize(response);

        var payload = client.Serializers.DeserializeContent<TResponse>(response);

        if (response.IsSuccessStatusCode)
            return Response.Ok(payload: payload, sizeBytes: sizeBytes);
        else
            return Response.Fail(payload: payload, sizeBytes: sizeBytes);
    }

    /// <summary>
    /// Sends an asynchronous HTTP GET request using the specified <see cref="RestRequest"/>, 
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>GET</c>.
    /// </summary>
    public static Task<Response<RestResponse>> SendGet(this RestClient client, RestRequest request)
    {
        request.Method = Method.Get;
        return Send(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP GET request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response 
    /// and wraps it in a <see cref="Response{T}"/> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>GET</c>.
    /// </summary>
    public static Task<Response<TResponse>> SendGet<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Get;
        return Send<TResponse>(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP POST request using the specified <see cref="RestRequest"/>, 
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>POST</c>.
    /// </summary>
    public static Task<Response<RestResponse>> SendPost(this RestClient client, RestRequest request)
    {
        request.Method = Method.Post;
        return Send(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP POST request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response 
    /// and wraps it in a <see cref="Response{T}"/> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>POST</c>.
    /// </summary>
    public static Task<Response<TResponse>> SendPost<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Post;
        return Send<TResponse>(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP PUT request using the specified <see cref="RestRequest"/>, 
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>PUT</c>.
    /// </summary>
    public static Task<Response<RestResponse>> SendPut(this RestClient client, RestRequest request)
    {
        request.Method = Method.Put;
        return Send(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP PUT request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response 
    /// and wraps it in a <see cref="Response{T}"/> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>PUT</c>.
    /// </summary>
    public static Task<Response<TResponse>> SendPut<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Put;

        return Send<TResponse>(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP PATCH request using the specified <see cref="RestRequest"/>, 
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>PATCH</c>.
    /// </summary>
    public static Task<Response<RestResponse>> SendPatch(this RestClient client, RestRequest request)
    {
        request.Method = Method.Patch;
        return Send(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP PATCH request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response 
    /// and wraps it in a <see cref="Response{T}"/> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>PATCH</c>.
    /// </summary>
    public static Task<Response<TResponse>> SendPatch<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Patch;
        return Send<TResponse>(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP DELETE request using the specified <see cref="RestRequest"/>, 
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>DELETE</c>.
    /// </summary>
    public static Task<Response<RestResponse>> SendDelete(this RestClient client, RestRequest request)
    {
        request.Method = Method.Delete;
        return Send(client, request);
    }

    /// <summary>
    /// Sends an asynchronous HTTP DELETE request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response 
    /// and wraps it in a <see cref="Response{T}"/> object, including size and latency metrics.
    /// <para />
    /// The <see cref="RestRequest.Method"/> will be overwritten with <c>DELETE</c>.
    /// </summary>
    public static Task<Response<TResponse>> SendDelete<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Delete;
        return Send<TResponse>(client, request);
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
