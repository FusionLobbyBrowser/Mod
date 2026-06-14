using System;
using System.Text;
using System.Text.Json;

using MelonLoader;

namespace FLB.Managers
{
    public static class ArgumentsManager
    {
        public static void Setup()
        {
            Core.Logger.Msg("[===== ARGUMENTS =====]");
            if (MelonLaunchOptions.ExternalArguments.TryGetValue("flb-code", out string code)
                && MelonLaunchOptions.ExternalArguments.TryGetValue("flb-layer", out string layer))
            {
                Join(code, layer);
            }
            else if (MelonLaunchOptions.ExternalArguments.TryGetValue("flb-encoded", out string encoded))
            {
                byte[] data = Convert.FromBase64String(encoded.FixBase64());
                Payload payload = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(data));
                Join(payload.Code, payload.Layer);
            }
            else
            {
                Core.Logger.Msg("No launch arguments were provided");
            }
        }

        // Layer in base64
        private static void Join(string code, string layer)
        {
            byte[] data = Convert.FromBase64String(layer);
            string decodedLayer = Encoding.UTF8.GetString(data);
            Core.Logger.Msg("Game was launched with arguments, queueing join");
            FusionManager.Join(code, decodedLayer);
        }

        private static string FixBase64(this string base64)
        {
            if (base64.Length % 4 != 0)
                base64 += ("===")[..(4 - (base64.Length % 4))];
            return base64.Replace("-", "+").Replace("_", "/");
        }
    }
}