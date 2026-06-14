using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Threading;

namespace Bridge
{
    internal static class HttpManager
    {
        public static int Port { get; set; } = 39373;

        public static Payload? Payload { get; set; }

        private static HttpListener Listener { get; set; }

        public static bool ShutdownRequested { get; set; } = false;

        private static CancellationTokenSource Token { get; set; }

        public static async Task HandleRequests()
        {
            ShutdownRequested = false;
            while (!ShutdownRequested)
            {
                var ctx = await Task.Run(async () => await Listener.GetContextAsync(), Token.Token);

                if (ShutdownRequested)
                    return;

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                resp.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                resp.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                resp.AddHeader("Access-Control-Max-Age", "1728000");
                resp.AppendHeader("Access-Control-Allow-Origin", "*");

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/")
                {
                    if (Payload != null)
                    {
                        await resp.Respond(JsonSerializer.Serialize(Payload), 200, type: "application/json");
                        Environment.Exit(0);
                    }
                    else
                    {
                        await resp.Respond("No payload found", 500);
                    }
                }
                else
                {
                    await resp.Respond("Not Found", 404);
                }
            }
        }

        public static async Task Respond(this HttpListenerResponse resp, string message, int statusCode = 200, string type = "text/plain")
        {
            resp.StatusCode = statusCode;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            resp.ContentType = type;
            resp.ContentLength64 = buffer.Length;
            await resp.OutputStream.WriteAsync(buffer);
            resp.Close();
        }

        public static async Task Start()
        {
            ShutdownRequested = true;
            Token = new();
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://localhost:{Port}/");
            Listener.Start();
            Program.Logger.Info($"Awaiting payload read on port {Port}");

            await HandleRequests();
        }

        public static void Stop()
        {
            ShutdownRequested = true;
            Token?.Cancel();
            Listener.Stop();
            Program.Logger.Info("HTTP Server stopped.");
        }
    }
}