using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace Bridge
{
    public static class Program
    {
        public const string VERSION = "1.0.0";

        public const string APP_ID = "1592190";

        private const string HOST = "http://localhost:25712/";

        public static string SteamPath { get; private set; }

        public static Config Config { get; private set; }

        public readonly static Logger Logger = new();

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        private const int STD_OUTPUT_HANDLE = -11;

        public static async Task Main(string[] args)
        {
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            await LoadConfig(current);

            if (!Config.HideConsole)
                SetupConsole();

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
            arg = FixBase64(uri.AbsolutePath[1..]);
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

            Payload payload = JsonSerializer.Deserialize<Payload>(decoded);

            if (!string.IsNullOrWhiteSpace(payload.Code) && !string.IsNullOrWhiteSpace(payload.Layer))
            {
                Logger.Info("Received the following data!");
                Logger.Info($" -> Code: {payload.Code}");
                Logger.Info($" -> Layer: {payload.Layer}");
            }
            else
            {
                Logger.Error("Missing information regarding Code and/or Layer!");
                await ExitApp();
                return;
            }

            var client = new HttpClient();
            var content = new StringContent(
                decoded,
                Encoding.UTF8,
                "application/json");

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
                try
                {
                    await client.PostAsync($"{HOST}join", content);
                    Logger.Info("Sent a request to the game, it should join the lobby in a second...");
                }
                catch (Exception ex)
                {
                    Logger.Error("The HTTP request failed to send, exception:");
                    Logger.Error(ex.ToString());
                    await ExitApp(15);
                }
                await ExitApp();
            }
            else
            {
                await LaunchGame(executable, root, payload);

                Logger.Info("Launched game!");
                await ExitApp();
            }
        }

        private static async Task LoadConfig(DirectoryInfo current)
        {
            var config = Path.Combine(current.FullName, "config.json");
            if (File.Exists(config))
            {
                Config = JsonSerializer.Deserialize<Config>(await File.ReadAllTextAsync(config));
            }
            else
            {
                Config = new();
                await using var stream = File.Create(config);
                await JsonSerializer.SerializeAsync(stream, Config, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                await stream.FlushAsync();
            }
        }

        private static async Task LaunchGame(FileInfo executable, DirectoryInfo root, Payload payload)
        {
            if (OperatingSystem.IsWindows())
            {
                SteamPath = GetWindowsPath();
                await SteamLaunch(executable, root, payload);
            }
            else if (OperatingSystem.IsLinux())
            {
                SteamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
                await SteamLaunch(executable, root, payload);
            }
            else
            {
                Logger.Error("Unrecognized OS, cannot launch with steam. Launching directly...");
                DirectLaunch(executable, root, payload);
            }
        }

        private static string FixBase64(this string base64)
        {
            if (base64.Length % 4 != 0)
                base64 += ("===")[..(4 - (base64.Length % 4))];
            return base64.Replace("-", "+").Replace("_", "/");
        }

        private static string ToBrokenBase64(this string text)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).RemoveAll(@"\+").RemoveAll(@"\/").RemoveAll(@"\=+$");

        private static string RemoveAll(this string text, string pattern)
            => Regex.Replace(text, pattern, string.Empty);

        private static void SetupConsole()
        {
            _ = AllocConsole();
            IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new(stdHandle, true);
            FileStream fileStream = new(safeFileHandle, FileAccess.Write);
            StreamWriter standardOutput = new(fileStream, Console.OutputEncoding)
            {
                AutoFlush = true
            };
            Console.SetOut(standardOutput);
            ConsoleColors.Initialize();
        }

        private static string Arguments(this Payload payload, bool uri = false)
        {
            if (!uri)
                return $"--flb-code={payload.Code} --flb-layer={Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.Layer))}";
            else
                return $"--flb-encoded={JsonSerializer.Serialize(payload).ToBrokenBase64()}";
        }

        private static void DirectLaunch(FileInfo executable, DirectoryInfo root, Payload payload)
        {
            var process = new Process();
            process.StartInfo.FileName = executable.FullName;
            process.StartInfo.WorkingDirectory = root.FullName;
            process.StartInfo.Arguments = payload.Arguments();
            process.Start();
        }

        private static async Task SteamLaunch(FileInfo executable, DirectoryInfo root, Payload payload)
        {
            if (!HasSteamGame() || !Config.LaunchWithSteam)
            {
                if (Config.NonSteamAppID == "-1" || !Config.LaunchWithSteam)
                {
                    Logger.Warning("User does not own the game, direct launch imminent");
                    if (Config.LaunchWithSteam)
                        Logger.Warning("If you'd like to launch the game (Meta Oculus Link version) with Steam, follow the instructions on the github repository!");
                    DirectLaunch(executable, root, payload);
                }
                else
                {
                    Logger.Info($"Launching non-steam game through Steam... (App ID: {Config.NonSteamAppID})");
                    await LaunchNonSteamGame(payload, Config.NonSteamAppID);
                }
            }
            else
            {
                Logger.Info("Launching bought game through Steam...");
                LaunchSteamGame(payload);
            }
        }

        private static void LaunchSteamGame(Payload payload, string appId = APP_ID)
        {
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(SteamPath, OperatingSystem.IsWindows() ? "steam.exe" : "steam");
            process.StartInfo.WorkingDirectory = SteamPath;
            process.StartInfo.Arguments = $"-applaunch ${appId} --flb-code={payload.Code} --flb-layer={Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.Layer))}";
            process.Start();
        }

        private static async Task LaunchNonSteamGame(Payload payload, string appId)
        {
            var process = new Process();
            process.StartInfo.FileName = $"steam://launch/{appId}//{Arguments(payload, true)}";
            process.StartInfo.UseShellExecute = true;
            process.Start();
            HttpManager.Payload = payload;
            _ = HttpManager.Start();
            await Task.Delay(60 * 1000);
            Logger.Info("Timeout, closing...");
            HttpManager.Stop();
            Environment.Exit(0);
        }

        private static bool HasSteamGame()
        {
            var libPath = Path.Combine(SteamPath, "config", "libraryfolders.vdf");
            if (!File.Exists(libPath))
                return false;

            var libraryFolders = VdfConvert.Deserialize(File.ReadAllText(libPath));

            foreach (var library
                in libraryFolders.Value
                .Select(x => ((VProperty)((VProperty)x).Value.First(y => ((VProperty)y).Key == "path")).Value.ToString()))
            {
                var steamapps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(steamapps))
                    continue;

                foreach (var acfPath in Directory.EnumerateFiles(steamapps, "*.acf"))
                {
                    VToken acf;
                    try
                    {
                        acf = VdfConvert.Deserialize(File.ReadAllText(acfPath)).Value;
                    }
                    catch
                    {
                        continue;
                    }

                    var id = ((VProperty)acf.FirstOrDefault(x => ((VProperty)x).Key == "appid"))?.Value?.ToString();

                    if (id == null)
                        continue;

                    if (id == APP_ID)
                        return true;
                }
            }

            return false;
        }

        private static string GetWindowsPath()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            key ??= Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            return (string)key?.GetValue("InstallPath");
#pragma warning restore CA1416 // Validate platform compatibility
        }

        private static async Task ExitApp(int seconds = -1)
        {
            if (Config.HideConsole)
                Environment.Exit(0);

            if (seconds == -1)
                seconds = Config.ExitTime;
            string last = null;
            string msg = null;
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

        public static string RemoveANSI(this string s)
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

    public static class ConsoleColors
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void Initialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                EnableANSI();
        }

        private static void EnableANSI()
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == IntPtr.Zero)
                throw new Win32Exception("Cannot get standard output handle");

            if (!GetConsoleMode(handle, out uint mode))
                throw new Win32Exception("Cannot get console mode");

            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            if (!SetConsoleMode(handle, mode))
                throw new Win32Exception("Cannot set console mode");
        }
    }
}