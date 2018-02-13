using System;
using System.Collections.Generic;
using Owin;

namespace ReverseProxyMiddleware
{
    public static class ProxyExtension
    {
        /// <summary>
        /// Sends request to remote server as specified in options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="proxyOptions">Options and rules for proxy actions</param>
        /// <returns></returns>
        public static IAppBuilder UseProxy(this IAppBuilder app, ProxyOptions proxyOptions)
        {
            return app.Use<ProxyMiddleware>(proxyOptions);
        }

        public static IAppBuilder UseProxy(this IAppBuilder app, List<ProxyRule> rules,
            Action<ProxyResult> reporter = null)
        {
            return app.Use<ProxyMiddleware>(new ProxyOptions(rules, reporter));
        }
    }
}