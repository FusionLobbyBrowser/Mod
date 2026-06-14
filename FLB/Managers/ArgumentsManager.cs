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
                Join(code, layer, true);
            }
            else if (MelonLaunchOptions.ExternalArguments.TryGetValue("flb-encoded", out string encoded))
            {
                var @fixed = encoded.FixBase64();
                byte[] data = Convert.FromBase64String(@fixed);
                Payload payload = JsonSerializer.Deserialize<Payload>(Encoding.UTF8.GetString(data));
                Join(payload.Code, payload.Layer);
            }
            else
            {
                Core.Logger.Msg("No launch arguments were provided");
            }
        }

        // Layer in base64
        private static void Join(string code, string layer, bool decodeLayer = false)
        {
            if (decodeLayer)
            {
                byte[] data = Convert.FromBase64String(layer);
                layer = Encoding.UTF8.GetString(data);
            }
            Core.Logger.Msg("Game was launched with arguments, queueing join");
            FusionManager.Join(code, layer);
        }

        private static string FixBase64(this string base64)
        {
            if (base64.Length % 4 != 0)
                base64 += ("===")[..(4 - (base64.Length % 4))];
            return base64.Replace("-", "+").Replace("_", "/");
        }
    }
}