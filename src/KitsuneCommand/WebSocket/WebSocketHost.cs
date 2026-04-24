using Autofac;
using KitsuneCommand.Configuration;
using WebSocketSharp.Server;

namespace KitsuneCommand.WebSocket
{
    /// <summary>
    /// Manages the WebSocketSharp server lifecycle.
    /// </summary>
    public class WebSocketHost
    {
        private readonly AppSettings _settings;
        private readonly IContainer _container;
        private WebSocketServer _server;

        public WebSocketHost(AppSettings settings, IContainer container)
        {
            _settings = settings;
            _container = container;
        }

        public void Start()
        {
            try
            {
                _server = new WebSocketServer(_settings.WebSocketPort);
                _server.KeepClean = false; // Don't auto-close idle connections
                _server.WaitTime = TimeSpan.FromSeconds(30);
                // Endpoint path is "/kcevents" because Cloudflare's managed WAF blocks
                // BOTH "/ws*" (on WebSocket Upgrade) AND "/socket*" (on any request,
                // even plain GET) with HTTP 400 at the edge — presumably rules for
                // common WebSocket-exploit scanners. Tested empirically: /kcevents,
                // /kctunnel, /events, /pipe etc. all pass through CF cleanly. Keep
                // this name KC-specific so it doesn't collide with anything generic
                // a future WAF rule might target.
                _server.AddWebSocketService<TelnetBehavior>("/kcevents");
                _server.Start();

                // Initialize the broadcaster
                var eventBus = _container.Resolve<Core.ModEventBus>();
                EventBroadcaster.Initialize(_server, eventBus);

                Log.Out($"[KitsuneCommand] WebSocket server listening on port {_settings.WebSocketPort}");
            }
            catch (Exception ex)
            {
                Log.Error($"[KitsuneCommand] Failed to start WebSocket server: {ex.Message}");
                Log.Exception(ex);
            }
        }

        public void Stop()
        {
            try
            {
                _server?.Stop();
                _server = null;
                Log.Out("[KitsuneCommand] WebSocket server stopped.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Error stopping WebSocket server: {ex.Message}");
            }
        }
    }
}
