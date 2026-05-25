using System;
using System.Linq;
using System.Text.RegularExpressions;

using MelonLoader;
using MelonLoader.Utils;

using Microsoft.Win32;

namespace FLB.Managers
{
    public static class UriManager
    {
        private const string ARGUMENT_STRING = " %1";

        public static void RegisterGame(string name, bool allowArguments = true)
        {
            var path = MelonEnvironment.GameExecutablePath;
            RegisterURI(name, path, allowArguments);
        }

        public static void RegisterURI(string name, string path, bool allowArguments = true)
        {
            path = (allowArguments && !path.EndsWith(ARGUMENT_STRING)) ? path + ARGUMENT_STRING : path;
#pragma warning disable CA1416 // Validate platform compatibility
            var classes = Registry.CurrentUser.OpenSubKey("Software", true)?.OpenSubKey("Classes", true);
            RegistryKey key = classes?.OpenSubKey(name);
            if (key == null)
            {
                key = classes.CreateSubKey(name);
                key.SetValue(string.Empty, "URL: " + name);
                key.SetValue("URL Protocol", string.Empty);

                key = key.CreateSubKey(@"shell\open\command");
                key.SetValue(string.Empty, path);
                key.Close();
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}