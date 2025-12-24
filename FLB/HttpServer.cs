using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FusionServerBrowser_Mod
{
    internal static class HttpServer
    {
        public static int Port { get; set; } = 25712;

        private static HttpListener Listener { get; set; }

        public static bool ShutdownRequested { get; set; } = false;

        private static readonly List<string> Requests = [];

        public static async Task HandleRequests()
        {
            ShutdownRequested = false;
            while (!ShutdownRequested)
            {
                var ctx = await Listener.GetContextAsync();

                if (ShutdownRequested)
                    return;

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                resp.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                resp.AddHeader("Access-Control-Allow-Methods", "GET");
                resp.AddHeader("Access-Control-Max-Age", "1728000");
                resp.AppendHeader("Access-Control-Allow-Origin", "*");

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/join")
                {
                    var code = req.QueryString["code"];
                    var layer = req.QueryString["layer"];
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(layer))
                    {
                        resp.StatusCode = 400;
                        byte[] errorBuffer = Encoding.UTF8.GetBytes("Missing code or layer parameter.");
                        resp.ContentType = "text/plain";
                        resp.ContentLength64 = errorBuffer.Length;
                        await resp.OutputStream.WriteAsync(errorBuffer);
                        resp.Close();
                        continue;
                    }
                    else
                    {
                        resp.StatusCode = 200;
                        byte[] responseBuffer = Encoding.UTF8.GetBytes("Join request received.");
                        resp.ContentType = "text/plain";
                        resp.ContentLength64 = responseBuffer.Length;
                        await resp.OutputStream.WriteAsync(responseBuffer);
                        resp.Close();
                        Core.Logger.Msg($"Received join request: code={code}, layer={layer}");
                        Requests.Add($"{layer}-{code}");
                        continue;
                    }
                }

                resp.StatusCode = 404;
                byte[] notFoundBuffer = Encoding.UTF8.GetBytes("Not Found");
                resp.ContentType = "text/plain";
                resp.ContentLength64 = notFoundBuffer.Length;
                await resp.OutputStream.WriteAsync(notFoundBuffer);
                resp.Close();
            }

        }

        public static void Start()
        {
            ShutdownRequested = true;
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://localhost:{Port}/");
            Listener.Start();
            Core.Logger.Msg($"HTTP Server started on port {Port}");

            _ = HandleRequests();
        }

        public static void Stop()
        {
            ShutdownRequested = true;
            Listener.Stop();
            Core.Logger.Msg("HTTP Server stopped.");
        }

        public static void Update()
        {
            if (Requests.Any())
            {
                Requests.ForEach(Core.Instance.ArgJoin);
                Requests.Clear();
            }

        }
    }
}
