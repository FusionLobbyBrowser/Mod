using System.IO;
using System.Reflection;

using MelonLoader.Utils;

namespace FLB.Managers
{
    public static class BridgeManager
    {
        public const string FILE_NAME = "Bridge";

        private const string DIRECTORY = "Dependencies.";

        public static string USERDATA => Path.Combine(MelonEnvironment.UserDataDirectory, "FLB");

        public static Stream GetFile(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var _name = assembly.GetName().Name;
            var path = $"{_name}.{DIRECTORY}{name}";

            return assembly.GetManifestResourceStream(path);
        }

        public static void CreateFile(string name)
        {
            if (!Directory.Exists(USERDATA))
            {
                Core.Logger.Msg("Missing directory in UserData!");
                Directory.CreateDirectory(USERDATA);
            }

            Core.Logger.Msg($"Creating {name}");

            using var embed = GetFile(name);
            using var stream = File.Create(Path.Combine(USERDATA, name));
            stream.Position = 0;
            embed.Position = 0;
            embed.CopyTo(stream);
            stream.Flush();

            Core.Logger.Msg($"Created {name}");
        }

        public static void Setup()
        {
            Core.Logger.Msg("[======= BRIDGE =======]");
            CreateFile($"{FILE_NAME}.exe");
            CreateFile($"{FILE_NAME}.dll");
            CreateFile($"{FILE_NAME}.runtimeconfig.json");
            UriManager.RegisterURI("flb-bridge", Path.Combine(USERDATA, $"{FILE_NAME}.exe"), true);
        }
    }
}