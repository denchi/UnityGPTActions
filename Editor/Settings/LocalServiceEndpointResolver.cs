using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace GPTUnity.Settings
{
    internal static class LocalServiceEndpointResolver
    {
        private const string LoopbackHost = "127.0.0.1";

        private static string _searchConfiguredUrl = string.Empty;
        private static bool _searchAutoEnabled;
        private static string _searchResolvedUrl = "http://" + LoopbackHost + ":8000";

        private static string _bridgeConfiguredUrl = string.Empty;
        private static bool _bridgeAutoEnabled;
        private static string _bridgeResolvedUrl = "http://" + LoopbackHost + ":7071";

        public static string ResolveSearchApiHost(ChatSettings settings)
        {
            return ResolveCached(
                settings?.SearchApiHost,
                settings?.SearchApiAutoHost ?? false,
                8000,
                ref _searchConfiguredUrl,
                ref _searchAutoEnabled,
                ref _searchResolvedUrl);
        }

        public static string ResolveMcpBridgeUrl(ChatSettings settings)
        {
            return ResolveCached(
                settings?.McpBridgeUrl,
                settings?.McpBridgeAutoUrl ?? false,
                7071,
                ref _bridgeConfiguredUrl,
                ref _bridgeAutoEnabled,
                ref _bridgeResolvedUrl);
        }

        private static string ResolveCached(
            string configuredUrl,
            bool autoEnabled,
            int defaultPort,
            ref string cachedConfiguredUrl,
            ref bool cachedAutoEnabled,
            ref string cachedResolvedUrl)
        {
            var normalizedConfigured = NormalizeConfiguredUrl(configuredUrl, defaultPort);

            if (!autoEnabled)
                return normalizedConfigured;

            if (cachedAutoEnabled == autoEnabled &&
                string.Equals(cachedConfiguredUrl, normalizedConfigured, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(cachedResolvedUrl))
            {
                return cachedResolvedUrl;
            }

            cachedConfiguredUrl = normalizedConfigured;
            cachedAutoEnabled = autoEnabled;
            cachedResolvedUrl = ResolveAutoLocalUrl(normalizedConfigured, defaultPort);
            return cachedResolvedUrl;
        }

        private static string NormalizeConfiguredUrl(string configuredUrl, int defaultPort)
        {
            if (!TryParseHttpUri(configuredUrl, defaultPort, out var uri))
            {
                return BuildHttpUrl(LoopbackHost, defaultPort);
            }

            var host = NormalizeHost(uri.Host);
            return BuildHttpUrl(host, uri.Port);
        }

        private static string ResolveAutoLocalUrl(string configuredUrl, int defaultPort)
        {
            if (!TryParseHttpUri(configuredUrl, defaultPort, out var uri))
            {
                return BuildHttpUrl(LoopbackHost, defaultPort);
            }

            var host = NormalizeHost(uri.Host);
            var preferredPort = uri.Port > 0 ? uri.Port : defaultPort;

            if (!IsLocalHost(host))
            {
                host = LoopbackHost;
            }

            if (IsPortAvailable(host, preferredPort))
            {
                return BuildHttpUrl(host, preferredPort);
            }

            var freePort = FindFreePort(host);
            return BuildHttpUrl(host, freePort);
        }

        private static bool TryParseHttpUri(string input, int defaultPort, out Uri uri)
        {
            uri = null;
            var raw = string.IsNullOrWhiteSpace(input) ? BuildHttpUrl(LoopbackHost, defaultPort) : input.Trim();
            if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                raw = "http://" + raw;
            }

            if (!Uri.TryCreate(raw, UriKind.Absolute, out var parsed))
                return false;

            var port = parsed.IsDefaultPort ? defaultPort : parsed.Port;
            var builder = new UriBuilder("http", parsed.Host, port);
            uri = builder.Uri;
            return true;
        }

        private static string NormalizeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return LoopbackHost;

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return LoopbackHost;

            return host;
        }

        private static bool IsLocalHost(string host)
        {
            if (string.Equals(host, LoopbackHost, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!IPAddress.TryParse(host, out var ip))
                return false;

            return IPAddress.IsLoopback(ip);
        }

        private static bool IsPortAvailable(string host, int port)
        {
            try
            {
                var address = ParseHostAddress(host);
                var listener = new TcpListener(address, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int FindFreePort(string host)
        {
            var address = ParseHostAddress(host);
            var listener = new TcpListener(address, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static IPAddress ParseHostAddress(string host)
        {
            if (IPAddress.TryParse(host, out var parsed))
                return parsed;

            return IPAddress.Loopback;
        }

        private static string BuildHttpUrl(string host, int port)
        {
            return "http://" + host + ":" + port.ToString(CultureInfo.InvariantCulture);
        }
    }
}
