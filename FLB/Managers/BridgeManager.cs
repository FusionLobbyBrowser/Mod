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

            using var embed = GetFile(assembly, name, out string fileName);
            Core.Logger.Msg($"Creating {fileName}");
            using var stream = File.Create(Path.Combine(USERDATA, fileName));
            stream.Position = 0;
            embed.Position = 0;
            embed.CopyTo(stream);
            stream.Flush();

            Core.Logger.Msg($"Created {fileName}");
        }

        public static void Setup()
        {
            Core.Logger.Msg("[======= BRIDGE =======]");

            var assembly = Assembly.GetExecutingAssembly();

            foreach (var name in assembly.GetManifestResourceNames())
                assembly.CreateFile(Path.GetFileName(name));

            UriManager.RegisterURI("flb-bridge", Path.Combine(USERDATA, $"{FILE_NAME}.exe"), true);
        }
    }
}