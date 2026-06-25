using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using MelonLoader.Utils;

namespace FLB.Managers
{
    public static class BridgeManager
    {
        public const string FILE_NAME = "Bridge";

        private const string DIRECTORY = "Dependencies.";

        public static string USERDATA => Path.Combine(MelonEnvironment.UserDataDirectory, "FLB");

        public static bool WaitForExit { get; set; } = false;

        public static Stream GetFile(this Assembly assembly, string name, out string fileName)
        {
            var _name = assembly.GetName().Name;
            string begin = $"{_name}.{DIRECTORY}";
            if (name.StartsWith(begin))
                name = name.Replace(begin, string.Empty);

            var path = $"{_name}.{DIRECTORY}{name}";
            fileName = name;

            return assembly.GetManifestResourceStream(path);
        }

        public static void CreateFile(this Assembly assembly, string name)
        {
            if (!Directory.Exists(USERDATA))
            {
                Core.Logger.Msg("Missing directory in UserData!");
                Directory.CreateDirectory(USERDATA);
            }

            try
            {
                using var embed = GetFile(assembly, name, out string fileName);
                Core.Logger.Msg($"Creating {fileName}");
                using var stream = File.Create(Path.Combine(USERDATA, fileName));
                stream.Position = 0;
                embed.Position = 0;
                embed.CopyTo(stream);
                stream.Flush();

                Core.Logger.Msg($"Created {fileName}");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Failed to create {name}", ex);
            }
        }

        public static void Setup()
        {
            Core.Logger.Msg("[======= BRIDGE =======]");

            var assembly = Assembly.GetExecutingAssembly();

            if (IsRunning(new(Path.Combine(USERDATA, "Bridge.exe"))))
            {
                Core.Logger.Msg("Bridge is currently being used, waiting until replacing files");
                WaitForExit = true;
            }
            else
            {
                FileCreate(assembly);
            }

            var path = Path.Combine(USERDATA, "launch.json");
            if (File.Exists(path))
            {
                Core.Logger.Msg("Found file with payload!");
                var payload = JsonSerializer.Deserialize<FilePayload>(File.ReadAllText(path));
                if (payload.Time != -1
                    && (DateTimeOffset.Now.ToUnixTimeSeconds() - payload.Time) < (60 * 15))
                {
                    Core.Logger.Msg("Joining with info provided in the file");
                    FusionManager.Join(payload.Code, payload.Layer);
                    File.Delete(path);
                }
            }

            UriManager.RegisterURI("flb-bridge", Path.Combine(USERDATA, $"{FILE_NAME}.exe"), true);
        }

        public static void FileCreate(Assembly assembly)
        {
            foreach (var name in assembly.GetManifestResourceNames())
                assembly.CreateFile(Path.GetFileName(name));
        }

        private static bool IsRunning(string FullPath)
        {
            string FilePath = Path.GetDirectoryName(FullPath);
            string FileName = Path.GetFileNameWithoutExtension(FullPath).ToLower();

            Process[] pList = Process.GetProcessesByName(FileName);

            return pList.Any(x => x.MainModule.FileName.StartsWith(FilePath, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}