using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;

namespace FLB.Managers
{
    internal static class HttpManager
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
                resp.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                resp.AddHeader("Access-Control-Max-Age", "1728000");
                resp.AppendHeader("Access-Control-Allow-Origin", "*");

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/join")
                    await Join(req, resp);
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/")
                    await resp.Respond("OK", 200);
                else
                    await resp.Respond("Not Found", 404);
            }
        }

        private static async Task Join(HttpListenerRequest req, HttpListenerResponse resp)
        {
            Console.WriteLine("Got join request! Reading...");
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            Core.Logger.Msg("Received: " + body);
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            string code, layer;
            if (!json.TryGetProperty("code", out var codeElem) || !json.TryGetProperty("layer", out var layerElem))
            {
                await resp.Respond("Missing code or layer parameter.", 400);
                return;
            }

            code = codeElem.GetString();
            layer = layerElem.GetString();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(layer))
                await resp.Respond("Missing code or layer parameter.", 400);

            Core.Logger.Msg("Received join request");
            Core.Logger.Msg($"[+] Layer: {layer}");
            Core.Logger.Msg($"[+] Code: {code}");
            Requests.Add($"{layer}-{code}");
            await resp.Respond("Join request received.", 200);
        }

        public static async Task Respond(this HttpListenerResponse resp, string message, int statusCode = 200)
        {
            resp.StatusCode = statusCode;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            resp.ContentType = "text/plain";
            resp.ContentLength64 = buffer.Length;
            await resp.OutputStream.WriteAsync(buffer);
            resp.Close();
        }

        public static void Setup()
        {
            Core.Logger.Msg("[======== HTTP =======]");
            Core.Logger.Msg("Starting HTTP Server...");

            try
            {
                Start();
                Core.Logger.Msg("Started HTTP Server!");
            }
            catch (Exception ex)
            {
                Core.Logger.Error("Failed to start HTTP Server :(", ex);
            }
            Core.Logger.Msg("[===================]");
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
                Requests.ForEach(FusionManager.ArgJoin);
                Requests.Clear();
            }
        }
    }
}