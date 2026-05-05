using MelonLoader;

using FLB.Managers;

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
            UriManager.Setup();
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
}