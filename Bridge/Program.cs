using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bridge
{
    public static class Program
    {
        private const string HOST = "http://localhost:25712/";

        private const int EXIT_DELAY = 3000;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("FLB Bridge was launched, checking for data...");
            string arg;
            if (args.Length > 0)
            {
                arg = args[0];
            }
            else
            {
                Console.WriteLine("No argument found!");
                await Task.Delay(EXIT_DELAY);
                return;
            }
            var uri = new Uri(arg);
            if (uri.Host != "join")
            {
                Console.WriteLine("Invalid host, the URL must be the following");
                Console.WriteLine(" -> flb-bridge://join/[base64]");
                await Task.Delay(EXIT_DELAY);
                return;
            }
            arg = uri.AbsolutePath[1..];
            if (arg.Length % 4 != 0)
                arg += ("===")[..(4 - (arg.Length % 4))];
            arg = arg.Replace("-", "+").Replace("_", "/");
            byte[] data = Convert.FromBase64String(arg);
            string decoded = Encoding.UTF8.GetString(data);

            string[] split = decoded.Split(" || ");

            Console.WriteLine(split.Length);

            if (split.Length == 2)
            {
                Console.WriteLine("Received the following data!");
                Console.WriteLine($" -> Code: {split[1]}");
                Console.WriteLine($" -> Layer: {split[0]}");
            }
            else
            {
                Console.WriteLine("Invalid data format, must be the following! (arrow is NOT part of the format)");
                Console.WriteLine(" -> [layer name] || [lobby code]");
                await Task.Delay(EXIT_DELAY);
                return;
            }

            string layer = split[0];
            string code = split[1];

            var client = new HttpClient
            {
                Timeout = new TimeSpan(0, 0, 2)
            };
            var content = new StringContent(
                JsonSerializer.Serialize(new Payload(code, layer)),
                Encoding.UTF8,
                "application/json");

            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var root = current?.Parent?.Parent;
            if (root == null)
            {
                Console.WriteLine("Failed to find game folder!");
                await Task.Delay(EXIT_DELAY);
                return;
            }
            var executable = root.GetFiles().FirstOrDefault(x => x.Name.StartsWith("BONELAB") && x.Name.EndsWith(".exe"));
            if (executable == null)
            {
                Console.WriteLine("Failed to find executable!");
                await Task.Delay(EXIT_DELAY);
                return;
            }

            if (IsRunning(executable.FullName))
            {
                Console.WriteLine("Game is launched, sending a request to join..");
                await client.PostAsync($"{HOST}join", content);
                Console.WriteLine("Sent a request to the game, it should join the lobby in a second");
                await Task.Delay(EXIT_DELAY);
            }
            else
            {
                var process = new Process();
                process.StartInfo.FileName = executable.FullName;
                process.StartInfo.WorkingDirectory = root.FullName;
                process.StartInfo.Arguments = $"--flb-code={code} --flb-layer={Convert.ToBase64String(Encoding.UTF8.GetBytes(layer))}";
                process.Start();

                Console.WriteLine("Launched game!");
                await Task.Delay(EXIT_DELAY);
            }
        }

        private static bool IsRunning(string FullPath)
        {
            string FilePath = Path.GetDirectoryName(FullPath);
            string FileName = Path.GetFileNameWithoutExtension(FullPath).ToLower();

            Process[] pList = Process.GetProcessesByName(FileName);

            return pList.Any(x => x.MainModule.FileName.StartsWith(FilePath, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    [method: JsonConstructor]
    public struct Payload(string code, string layer)
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = code;

        [JsonPropertyName("layer")]
        public string Layer { get; set; } = layer;
    }
}