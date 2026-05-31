using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bridge
{
    public static class Program
    {
        public const string VERSION = "1.0.0";

        private const string HOST = "http://localhost:25712/";

        private const int EXIT_DELAY = 5;

        private readonly static Logger Logger = new();

        public static async Task Main(string[] args)
        {
            Logger.Info($"FLB Bridge | v{VERSION}");
            string arg;
            if (args.Length > 0)
            {
                arg = args[0];
            }
            else
            {
                Logger.Error("No argument found! The executable must be launched with the following URL");
                Logger.Error(" -> flb-bridge://join/[base64]");
                await ExitApp();
                return;
            }
            if (arg.EndsWith('/'))
                arg = arg[..^1];

            var uri = new Uri(arg);
            if (uri.Host != "join")
            {
                Logger.Error("Invalid host, the URL must be the following");
                Logger.Error(" -> flb-bridge://join/[base64]");
                await ExitApp();
                return;
            }
            arg = uri.AbsolutePath[1..];
            if (arg.Length % 4 != 0)
                arg += ("===")[..(4 - (arg.Length % 4))];
            arg = arg.Replace("-", "+").Replace("_", "/");
            byte[] data;
            try
            {
                data = Convert.FromBase64String(arg);
            }
            catch (FormatException)
            {
                Logger.Error("Invalid argument, it must a be a modified base64 value!");
                await ExitApp();
                return;
            }

            string decoded = Encoding.UTF8.GetString(data);

            string[] split = decoded.Split(" || ");

            if (split.Length == 2)
            {
                Logger.Info("Received the following data!");
                Logger.Info($" -> Code: {split[1]}");
                Logger.Info($" -> Layer: {split[0]}");
            }
            else
            {
                Logger.Error("Invalid data format, must be the following! (arrow is NOT part of the format)");
                Logger.Error(" -> [layer name] || [lobby code]");
                await ExitApp();
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
                Logger.Error("Failed to find game folder!");
                await ExitApp();
                return;
            }
            var executable = root.GetFiles().FirstOrDefault(x => x.Name.StartsWith("BONELAB") && x.Name.EndsWith(".exe"));
            if (executable == null)
            {
                Logger.Error("Failed to find executable!");
                await ExitApp();
                return;
            }

            if (IsRunning(executable.FullName))
            {
                Logger.Info("Game is launched, sending a request to join..");
                await client.PostAsync($"{HOST}join", content);
                Logger.Info("Sent a request to the game, it should join the lobby in a second...");
                await ExitApp();
            }
            else
            {
                var process = new Process();
                process.StartInfo.FileName = executable.FullName;
                process.StartInfo.WorkingDirectory = root.FullName;
                process.StartInfo.Arguments = $"--flb-code={code} --flb-layer={Convert.ToBase64String(Encoding.UTF8.GetBytes(layer))}";
                process.Start();

                Logger.Info("Launched game!");
                await ExitApp();
            }
        }

        private static async Task ExitApp()
        {
            string last = null;
            string msg = null;
            int seconds = EXIT_DELAY;
            int top = -1;
            while (seconds > -1)
            {
                if (last == null && msg == null)
                {
                    last = $"{seconds}...".Pastel(Color.AliceBlue);
                    msg = Logger.Info($"The app will exit in {last}");
                }
                else
                {
                    if (top == -1)
                        top = Console.CursorTop - 1;
                    Console.SetCursorPosition(msg.RemoveANSI().Length - last.RemoveANSI().Length, top);
                    last = $"{seconds}...".Pastel(Color.AliceBlue);
                    Console.Write(last);
                }
                await Task.Delay(1000);
                seconds--;
            }
            Environment.Exit(0);
        }

        private static string RemoveANSI(this string s)
            => Regex.Replace(s, @"(\x1B|\e|\033)\[(.*?)m", "");

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