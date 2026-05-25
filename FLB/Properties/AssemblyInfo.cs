using System.Reflection;
using System.Runtime.InteropServices;

using MelonLoader;

#region MelonLoader

[assembly: MelonInfo(typeof(FLB.Core), "FLB", FLB.Core.Version, "HAHOOS", "https://github.com/FusionLobbyBrowser/Mod")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]

#endregion MelonLoader

#region Assembly Info

[assembly: AssemblyTitle("FLB")]
[assembly: AssemblyDescription("FLB Mod allows for quick joining through the website!")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyProduct("FLB")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#endregion Assembly Info

#region COM

[assembly: ComVisible(false)]
[assembly: Guid("1f9b8074-02ea-4f6f-9d44-529225b7bef6")]

#endregion COM

#region Version

[assembly: AssemblyVersion(FLB.Core.Version)]
[assembly: AssemblyFileVersion(FLB.Core.Version)]
[assembly: AssemblyInformationalVersion(FLB.Core.Version)]

#endregion Version