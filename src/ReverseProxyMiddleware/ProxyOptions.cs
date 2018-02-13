using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace ReverseProxyMiddleware
{
    public class ProxyOptions
    {
        public List<ProxyRule> ProxyRules { get; set; } = new List<ProxyRule>();
        public HttpMessageHandler BackChannelMessageHandler { get; set; }
        public Action<ProxyResult> Reporter { get; set; } = result => { };

        public bool FollowRedirects { get; set; } = true;

        public bool UseExternalProxy { get; set; } = false;
        public WebProxy ExternalProxy { get; set; }
        public bool ExternalProxyPreAuthenticate { get; set; } = false;

        public ProxyOptions()
        {
        }

        public ProxyOptions(List<ProxyRule> rules, Action<ProxyResult> reporter = null)
        {
            ProxyRules = rules;
            if (reporter != null)
            {
                Reporter = reporter;
            }
        }

        public void AddProxyRule(ProxyRule rule)
        {
            ProxyRules.Add(rule);
        }
    }
}