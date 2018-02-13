using System;

namespace ReverseProxyMiddleware
{
    public class ProxyResult
    {
        public ProxyStatus ProxyStatus { get; set; }
        public int HttpStatusCode { get; set; }
        public Uri OriginalUri { get; set; }
        public Uri ProxiedUri { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}