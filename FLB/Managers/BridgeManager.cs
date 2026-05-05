using System.IO;
using System.Reflection;

using MelonLoader.Utils;

namespace FLB.Managers
{
    public static class BridgeManager
    {
        public const string FILE_NAME = "Bridge.exe";

        private const string DIRECTORY = "Dependencies.";

        public static string USERDATA => Path.Combine(MelonEnvironment.UserDataDirectory, "FLB");

        public static string BRIDGEPATH => Path.Combine(USERDATA, FILE_NAME);

        public static Stream GetFile()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var name = assembly.GetName().Name;
            var path = $"{name}.{DIRECTORY}{FILE_NAME}";

            return assembly.GetManifestResourceStream(path);
        }

        public static void CreateFile()
        {
            if (!Directory.Exists(USERDATA))
            {
                Core.Logger.Msg("Missing directory in UserData!");
                Directory.CreateDirectory(USERDATA);
            }

            Core.Logger.Msg($"Creating {FILE_NAME}");

            using var embed = GetFile();
            using var stream = File.Create(BRIDGEPATH);
            stream.Position = 0;
            embed.Position = 0;
            embed.CopyTo(stream);
            stream.Flush();

            Core.Logger.Msg($"Created {FILE_NAME}");
        }

        public static void Setup()
        {
            Core.Logger.Msg("[======= BRIDGE =======]");
            CreateFile();
            UriManager.RegisterURI("flb-bridge", BRIDGEPATH, true);
        }
    }
}