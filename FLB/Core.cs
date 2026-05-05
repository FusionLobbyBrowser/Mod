using System;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

using FLB;

using Il2CppSLZ.Marrow.SceneStreaming;

using LabFusion.Data;
using LabFusion.Network;
using LabFusion.UI.Popups;

using MelonLoader;
using FLB.Managers;

[assembly: MelonInfo(typeof(Core), "FLB", "1.0.1", "HAHOOS", null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]

namespace FLB
{
    public class Core : MelonMod
    {
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