using System;
using System.Text;

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
                byte[] data = Convert.FromBase64String(layer);
                string decodedLayer = Encoding.UTF8.GetString(data);
                Core.Logger.Msg("Game was launched with arguments, queueing join");
                FusionManager.Join(code, decodedLayer);
            }
            else
            {
                Core.Logger.Msg("No launch arguments were provided");
            }
        }
    }
}