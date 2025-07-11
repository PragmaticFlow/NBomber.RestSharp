using System.Text;
using RestSharp;
using NBomber.Contracts;
using NBomber.CSharp;
using RestSharp.Serializers;

namespace NBomber.RestSharp;

public static class RestClientBuilder
{
    /// <summary>
    /// Creates and configures a default <see cref="RestClient"/> instance with a preconfigured <see cref="SocketsHttpHandler"/>.
    /// </summary>
    /// <param name="options">
    /// Optional <see cref="RestClientOptions"/> to configure base URL, timeout, headers, etc. If null, default options are used.
    /// </param>
    /// <param name="disposeHttpClient">
    /// Indicates whether the <see cref="RestClient"/> should dispose the internally created <see cref="HttpClient"/> when disposed. 
    /// Default is <c>false</c>.
    /// </param>
    /// <param name="configureSerialization">
    /// Optional delegate to configure custom serialization behavior for the <see cref="RestClient"/>.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RestClient"/> configured with connection pooling, timeout, and serialization options.
    /// </returns>
    /// <remarks>
    /// The internal <see cref="SocketsHttpHandler"/> is configured with:
    /// - <c>PooledConnectionLifetime</c>: 10 minutes
    /// - <c>PooledConnectionIdleTimeout</c>: 5 minutes
    /// - <c>MaxConnectionsPerServer</c>: int.MaxValue
    /// </remarks>
    public static RestClient CreateDefaultClient(
        RestClientOptions? options = null, 
        bool disposeHttpClient = false,
        ConfigureSerialization? configureSerialization = null)
    {
        var socketsHandler = new SocketsHttpHandler();
        socketsHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(10.0);
        socketsHandler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5.0);
        socketsHandler.MaxConnectionsPerServer = int.MaxValue;
        
        var http = new HttpClient(socketsHandler);

        return new RestClient(http, options, disposeHttpClient, configureSerialization);
    }
}

public static class RestClientExtensions
{
    const int HeaderSeparatorLength = 2; // symbol `: `
    const int QueryParamSeparatorLength = 1; // symbol `=`
    const int CrlfLength = 2; // \r\n
    const int SpaceLength = 1;
    const int HttpVersionHeaderLength = 8;
    const int HostHeaderLength = 4;
    const int StatusCodeLength = 3;
    
    // Accept: application/json, text/json, text/x-json, text/javascript, application/xml, text/xml
    const int DefaultAcceptHeaderLength = 92 + CrlfLength;
    // Accept-Encoding: gzip, deflate, br
    const int DefaultAcceptEncodingHeaderLength = 34 + CrlfLength;

    /// <summary>
    /// Sends an asynchronous HTTP request using the specified <see cref="RestRequest"/>,
    /// then evaluates the response and wraps it in a <c>Response&lt;RestResponse&gt;</c> object, including size and latency metrics.
    /// </summary>
    public static async Task<Response<RestResponse>> Send(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var reqSize = CalcRequestSize(client, request);
        var resSize = CalcResponseSize(response);
        var sizeBytes = reqSize + resSize;

        return response.IsSuccessStatusCode 
            ? Response.Ok(payload: response, sizeBytes: sizeBytes) 
            : Response.Fail(payload: response, sizeBytes: sizeBytes);
    }

    /// <summary>
    /// Sends an asynchronous HTTP request using the specified <see cref="RestRequest"/>, 
    /// deserializes the JSON response content into a specified type, then evaluates the response and wraps it
    /// in a custom <see cref="Response{T}"/> object, including size and latency metrics.
    /// </summary>
    public static async Task<Response<TResponse?>> Send<TResponse>(this RestClient client, RestRequest request)
    {
        var response = await client.ExecuteAsync(request);
        var reqSize = CalcRequestSize(client, request);
        var resSize = CalcResponseSize(response);
        var sizeBytes = reqSize + resSize;

        var payload = client.Serializers.DeserializeContent<TResponse>(response);

        return response.IsSuccessStatusCode 
            ? Response.Ok(payload: payload, sizeBytes: sizeBytes) 
            : Response.Fail(payload: payload, sizeBytes: sizeBytes);
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
    public static Task<Response<TResponse?>> SendGet<TResponse>(this RestClient client, RestRequest request)
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
    public static Task<Response<TResponse?>> SendPost<TResponse>(this RestClient client, RestRequest request)
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
    public static Task<Response<TResponse?>> SendPut<TResponse>(this RestClient client, RestRequest request)
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
    public static Task<Response<TResponse?>> SendPatch<TResponse>(this RestClient client, RestRequest request)
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
    public static Task<Response<TResponse?>> SendDelete<TResponse>(this RestClient client, RestRequest request)
    {
        request.Method = Method.Delete;
        return Send<TResponse>(client, request);
    }

    private static int CalcParamsSize(Parameter param, RestSerializers serializers)
    {    
        if (param.Type == ParameterType.HttpHeader)
            return Encoding.UTF8.GetByteCount(param.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(param.Value.ToString()) + CrlfLength;
            
        if (param.Type == ParameterType.QueryString)
            return Encoding.UTF8.GetByteCount(param.Name) + QueryParamSeparatorLength + Encoding.UTF8.GetByteCount(param.Value.ToString());

        if (param.Type == ParameterType.RequestBody)
        {
            var body = param as BodyParameter;
                
            if (body!.DataFormat == DataFormat.Binary)
                return (body.Value as byte[])?.Length ?? 0;

            if (body.DataFormat == DataFormat.None)
                return body.Value?.ToString()?.Length ?? 0;

            if (body.DataFormat == DataFormat.Json)
            {
                var json = serializers.GetSerializer(DataFormat.Json).Serialize(body);
                return json?.Length ?? 0;
            }

            if (body.DataFormat == DataFormat.Xml)
            {
                var json = serializers.GetSerializer(DataFormat.Xml).Serialize(body);
                return json?.Length ?? 0;
            }
        }

        return Encoding.UTF8.GetByteCount(param.Value.ToString());
    }
    
    private static int CalcUrlSize(ReadOnlyRestClientOptions options, RestRequest request)
    {
        // url size is calculated without query params.
        // query param will be calculated via request.Parameters
        
        var urlSize = request.Resource.Contains("http")
            ? request.Resource.Length 
            : (options.BaseUrl?.OriginalString.Length + request.Resource.Length + SpaceLength) ?? 0;

        return urlSize + SpaceLength;
    }
    
    private static int CalcHostHeaderSize(ReadOnlyRestClientOptions options)
    {
        var hostName = options.BaseUrl?.Authority ?? "";
        return HostHeaderLength + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(hostName) + CrlfLength;
    }
    
    private static long CalcRequestSize(RestClient client, RestRequest request)
    {
        var fileSize = request.Files.Sum(x => x.GetFile().Length);
        var reqParamsSize = request.Parameters.Sum(param => CalcParamsSize(param, client.Serializers));
        var defaultParamSize = client.DefaultParameters.Sum(param => CalcParamsSize(param, client.Serializers));
        
        var methodSize = Encoding.UTF8.GetByteCount(request.Method.ToString()) + SpaceLength;
        var urlSize = CalcUrlSize(client.Options, request);
        var httpVersionSize = HttpVersionHeaderLength;
        
        var hostHeaderSize = CalcHostHeaderSize(client.Options);

        var sizeBytes = 
            methodSize + urlSize + httpVersionSize + CrlfLength 
            + hostHeaderSize
            + defaultParamSize + reqParamsSize + fileSize
            + DefaultAcceptHeaderLength + DefaultAcceptEncodingHeaderLength;

        return sizeBytes;
    }

    private static long CalcResponseSize(RestResponse response)
    {
        var httpVersionSize = HttpVersionHeaderLength + SpaceLength;
        var statusCodeSize = StatusCodeLength + SpaceLength + Encoding.UTF8.GetByteCount(response.StatusCode.ToString()) + CrlfLength;
        
        var bodySize = response.ContentLength.HasValue ? response.ContentLength.Value + CrlfLength : 0;
        
        var contentHeadersSize = response.ContentHeaders?.Sum(p => 
            Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value) + CrlfLength
        ) ?? 0;
        
        var headersSize = response.Headers?.Sum(p => 
            Encoding.UTF8.GetByteCount(p.Name) + HeaderSeparatorLength + Encoding.UTF8.GetByteCount(p.Value) + CrlfLength
        ) ?? 0;
        
        var sizeBytes =
            httpVersionSize + statusCodeSize + contentHeadersSize + headersSize + bodySize;
        
        return sizeBytes;
    }
}
