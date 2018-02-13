using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace ReverseProxyMiddleware
{
    public class ProxyRule
    {
        public Func<Uri, bool> Matcher { get; set; } = uri => false;
        public Action<HttpRequestMessage, ClaimsPrincipal> Modifier { get; set; } = (msg, user) => { };
        public Func<HttpResponseMessage, IOwinContext, Task> ResponseModifier { get; set; } = null;
        public bool PreProcessResponse { get; set; } = true;
        public bool RequiresAuthentication { get; set; }
    }
}