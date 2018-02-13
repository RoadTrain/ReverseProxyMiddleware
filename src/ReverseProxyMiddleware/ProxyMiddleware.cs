using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace ReverseProxyMiddleware
{
    public class ProxyMiddleware
    {
        private readonly AppFunc _next;
        private readonly HttpClient _httpClient;
        private readonly ProxyOptions _options;

        public ProxyMiddleware(AppFunc next, ProxyOptions options)
        {
            _next = next;
            _options = options;
            _httpClient = new HttpClient(_options.BackChannelMessageHandler ?? new HttpClientHandler
            {
                AllowAutoRedirect = _options.FollowRedirects,
                UseProxy = options.UseExternalProxy,
                Proxy = options.ExternalProxy,
                PreAuthenticate = options.ExternalProxyPreAuthenticate,
            });
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var context = new OwinContext(environment);
            var uri = GeRequestUri(context);
            var resultBuilder = new ProxyResultBuilder(uri);

            var matchedRule = _options.ProxyRules.FirstOrDefault(r => r.Matcher.Invoke(uri));
            if (matchedRule == null)
            {
                await _next(environment);
                _options.Reporter.Invoke(resultBuilder.NotProxied(context.Response.StatusCode));
                return;
            }

            if (matchedRule.RequiresAuthentication && !UserIsAuthenticated(context))
            {
                context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                _options.Reporter.Invoke(resultBuilder.NotAuthenticated());
                return;
            }

            var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
            SetProxyRequestBody(proxyRequest, context);
            SetProxyRequestHeaders(proxyRequest, context);

            matchedRule.Modifier.Invoke(proxyRequest, context.Authentication.User);
            proxyRequest.Headers.Host = proxyRequest.RequestUri.Host;

            try
            {
                await ProxyTheRequest(context, proxyRequest, matchedRule);
            }
            catch (HttpRequestException)
            {
                context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
            }

            _options.Reporter.Invoke(resultBuilder.Proxied(proxyRequest.RequestUri, context.Response.StatusCode));
        }

        private async Task ProxyTheRequest(IOwinContext context, HttpRequestMessage proxyRequest, ProxyRule proxyRule)
        {
            using (var responseMessage = await _httpClient.SendAsync(proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.Request.CallCancelled
                ))
            {
                if (proxyRule.PreProcessResponse || proxyRule.ResponseModifier == null)
                {
                    context.Response.StatusCode = (int) responseMessage.StatusCode;
                    context.Response.ContentType = responseMessage.Content?.Headers.ContentType?.MediaType;

                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers.SetValues(header.Key, header.Value.ToArray());
                    }

                    // SendAsync removes chunking from the response. 
                    // This removes the header so it doesn't expect a chunked response.
                    context.Response.Headers.Remove("transfer-encoding");

                    if (responseMessage.Content != null)
                    {
                        foreach (var contentHeader in responseMessage.Content.Headers)
                        {
                            context.Response.Headers.SetValues(contentHeader.Key, contentHeader.Value.ToArray());
                        }

                        await responseMessage.Content.CopyToAsync(context.Response.Body);
                    }
                }

                if (proxyRule.ResponseModifier != null)
                    await proxyRule.ResponseModifier.Invoke(responseMessage, context);
            }
        }

        private static Uri GeRequestUri(IOwinContext context)
        {
            var request = context.Request;
            var uriString = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            return new Uri(uriString);
        }

        private static void SetProxyRequestBody(HttpRequestMessage requestMessage, IOwinContext context)
        {
            var requestMethod = context.Request.Method;
            if (StringComparer.OrdinalIgnoreCase.Equals("GET", requestMethod) ||
                StringComparer.OrdinalIgnoreCase.Equals("HEAD", requestMethod) ||
                StringComparer.OrdinalIgnoreCase.Equals("DELETE", requestMethod) ||
                StringComparer.OrdinalIgnoreCase.Equals("TRACE", requestMethod))
                return;
            requestMessage.Content = new StreamContent(context.Request.Body);
        }

        private void SetProxyRequestHeaders(HttpRequestMessage requestMessage, IOwinContext context)
        {
            foreach (var header in context.Request.Headers)
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        private bool UserIsAuthenticated(IOwinContext context)
        {
            return context.Authentication.User.Identities.FirstOrDefault()?.IsAuthenticated ?? false;
        }
    }
}