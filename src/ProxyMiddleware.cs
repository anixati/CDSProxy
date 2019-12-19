using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CrmWebApiProxy
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _nextHandler;
        private readonly ILogger _logger;
        private static readonly HttpClient _client = new HttpClient();
        private readonly ProxyConfig _settings;
        private readonly ICRMAuthenticator _authenticator;

        public ProxyMiddleware(RequestDelegate nextHandler, IOptions<ProxyConfig> options,
            ILoggerFactory loggerFactory, ICRMAuthenticator authenticator)
        {
            _nextHandler = nextHandler;
            _settings = options.Value;
            _logger = loggerFactory
                      .CreateLogger<ProxyMiddleware>();
            _authenticator = authenticator;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.Contains("api/data"))
            {
                var requetUri = GetRequestUri(context.Request);
                if (requetUri != null)
                {
                    var requestMsg = BuildRequestMessage(context, requetUri);

                    using (var response = await _client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                    {
                        context.Response.StatusCode = (int)response.StatusCode;
                        SetupHeaders(context, response);
                        await response.Content.CopyToAsync(context.Response.Body);
                    }
                    return;
                }
            }
            await _nextHandler(context);
        }

        private void SetupHeaders(HttpContext context, HttpResponseMessage response)
        {
            foreach (var header in response.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in response.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding");
        }

        private HttpRequestMessage BuildRequestMessage(HttpContext context, Uri requestUri)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = requestUri,
                Method = GetMethod(context.Request.Method)
            };
            var method = context.Request.Method;
            if (!HttpMethods.IsGet(method) &&
              !HttpMethods.IsHead(method) &&
              !HttpMethods.IsDelete(method) &&
              !HttpMethods.IsTrace(method))
            {
                var streamContent = new StreamContent(context.Request.Body);
                request.Content = streamContent;
            }
            foreach (var header in context.Request.Headers)
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticator.GetAccessToken());
            //request.Content?.Headers.TryAddWithoutValidation("OData-MaxVersion", "4.0");
            //request.Content?.Headers.TryAddWithoutValidation("OData-Version", "4.0");
            // request.Content?.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.Host = requestUri.Host;

            return request;
        }

        private Uri GetRequestUri(HttpRequest request)
        {
            var uri = new Uri($"{_settings.CrmHostUri}{request.Path}");
            UriBuilder uriBuilder = new UriBuilder(uri);

            if (request.QueryString.HasValue)
            {
                uriBuilder.Query = request.QueryString.Value;
            }

            return uriBuilder.Uri;
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }
    }
}