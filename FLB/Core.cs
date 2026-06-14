using System.Text.Json.Serialization;

using FLB.Managers;

using MelonLoader;

namespace FLB
{
    public class Core : MelonMod
    {
        public const string Version = "1.1.0";

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static Core Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Logger = LoggerInstance;
            BridgeManager.Setup();
            ArgumentsManager.Setup();
            HttpManager.Setup();
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnDeinitializeMelon()
        {
            base.OnDeinitializeMelon();
            HttpManager.Stop();
        }

        public override void OnUpdate()
        {
            HttpManager.Update();
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