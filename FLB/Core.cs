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
using MelonLoader.Utils;

using Microsoft.Win32;

[assembly: MelonInfo(typeof(Core), "FLB", "1.0.1", "HAHOOS", null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]

namespace FLB
{
    public class Core : MelonMod
    {
        public static bool HasFusion => FindMelon("LabFusion", "Lakatrazz") != null;

        public static bool IsConnected => NetworkInfo.HasServer;

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static Core Instance { get; private set; }

        private const string URI_NAME = "bonelab-flb";

        public override void OnInitializeMelon()
        {
            Instance = this;
            Logger = LoggerInstance;
            LoggerInstance.Msg("[====== URI =======]");
            LoggerInstance.Msg("Registering URI Scheme...");
            try
            {
                RegisterURI(URI_NAME);
                LoggerInstance.Msg("Registered URI Scheme!");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to register URI scheme :(", ex);
            }
            var arg = MelonLaunchOptions.CommandLineArgs.FirstOrDefault(x => x.StartsWith($"{URI_NAME}://"));
            if (!string.IsNullOrWhiteSpace(arg))
            {
                LoggerInstance.Msg("Game launched from website, preparing to join lobby...");
                var info = Regex.Match(arg, URI_NAME + ":\\/\\/(.*?)\\/")?.Groups?[1].Value;
                ArgJoin(info);
            }

            LoggerInstance.Msg("[====== HTTP =======]");
            LoggerInstance.Msg("Starting HTTP Server...");

            try
            {
                HttpServer.Start();
                LoggerInstance.Msg("Started HTTP Server!");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Failed to start HTTP Server :(", ex);
            }
            LoggerInstance.Msg("[===================]");
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnDeinitializeMelon()
        {
            base.OnDeinitializeMelon();
            HttpServer.Stop();
        }

        public override void OnUpdate()
        {
            HttpServer.Update();
        }

        private static void RegisterURI(string name)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var classes = Registry.CurrentUser.OpenSubKey("Software", true)?.OpenSubKey("Classes", true);
            RegistryKey key = classes?.OpenSubKey(name);
            if (key == null)
            {
                key = classes.CreateSubKey(name);
                key.SetValue(string.Empty, "URL: " + name);
                key.SetValue("URL Protocol", string.Empty);

                key = key.CreateSubKey(@"shell\open\command");
                key.SetValue(string.Empty, MelonEnvironment.GameExecutablePath + " %1");
                key.Close();
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }


        public void Join(string code, string layer)
        {
            void join()
            {
                LoggerInstance.Msg($"Attempting to join with the following code: {code}");
                if (code != LobbyInfoManager.LobbyInfo?.LobbyCode)
                {
                    Notifier.Send(new Notification()
                    {
                        Title = "FLB",
                        Message = "Attempting to join the target lobby, this might take a few seconds...",
                        PopupLength = 4f,
                        ShowPopup = true,
                        SaveToMenu = false,
                        Type = NotificationType.INFORMATION
                    });

                    if (EnsureNetworkLayer(layer))
                        JoinByCode(code);
                    else
                        ErrorNotif("Failed to ensure network layer, check the console/logs for errors. If none are present, it's likely the server is on a network layer that you do not have.", 5f);

                }
                else
                {
                    LoggerInstance.Error("Player is already in the lobby");
                    ErrorNotif("Could not join, because you are already in the lobby!");
                }
            }

            MelonCoroutines.Start(AfterLevelLoaded(join));
        }
        internal void ArgJoin(string arg)
        {
            if (!HasFusion)
                return;

            try
            {
                LoggerInstance.Msg("Received Join Request");
                string[] split = arg.Split("-");

                if (split.Length <= 1)
                    throw new ArgumentException("Secret provided to join the lobby did not include all of the necessary info");

                if (split.Length > 2)
                    throw new ArgumentException("Secret provided to join the lobby was invalid, the name of the network layer or code to the server may have contained the '-' character used to separate network layer & code, causing unexpected results");

                string layer = split[0];
                string code = split[1];

                void join()
                {
                    LoggerInstance.Msg($"Attempting to join with the following code: {code}");
                    if (code != LobbyInfoManager.LobbyInfo?.LobbyCode)
                    {
                        Notifier.Send(new Notification()
                        {
                            Title = "FLB",
                            Message = "Attempting to join the target lobby, this might take a few seconds...",
                            PopupLength = 4f,
                            ShowPopup = true,
                            SaveToMenu = false,
                            Type = NotificationType.INFORMATION
                        });

                        if (EnsureNetworkLayer(layer))
                            JoinByCode(code);
                        else
                            ErrorNotif("Failed to ensure network layer, check the console/logs for errors. If none are present, it's likely the server is on a network layer that you do not have.", 5f);

                    }
                    else
                    {
                        LoggerInstance.Error("Player is already in the lobby");
                        ErrorNotif("Could not join, because you are already in the lobby!");
                    }
                }

                MelonCoroutines.Start(AfterLevelLoaded(join));
            }
            catch (Exception ex)
            {
                ErrorNotif("An unexpected error has occurred while trying to join the lobby, check the console or logs for more details", 5f);
                LoggerInstance.Error("An unexpected error has occurred while trying to join the lobby", ex);
            }
        }

        private static IEnumerator AfterLevelLoaded(Action callback)
        {
            while (SceneStreamer.Session?.Status != StreamStatus.DONE)
                yield return null;

            callback?.Invoke();
        }

        public bool EnsureNetworkLayer(string title)
        {
            if (!HasFusion)
                return false;
            else
                return Internal_EnsureNetworkLayer(title);
        }

        private bool Internal_EnsureNetworkLayer(string title)
        {
            if (!NetworkLayer.LayerLookup.TryGetValue(title, out var layer))
            {
                LoggerInstance.Error($"Could find network layer '{title}'");
                return false;
            }

            try
            {
                if (NetworkLayerManager.LoggedIn && NetworkLayerManager.Layer == layer)
                    return true;

                if (NetworkLayerManager.LoggedIn)
                    NetworkLayerManager.LogOut();

                NetworkLayerManager.LogIn(layer);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("An unexpected error has occurred while ensuring fusion is on the right network layer, exception", ex);
                return false;
            }

            return true;
        }

        public void JoinByCode(string code)
        {
            if (HasFusion && !string.IsNullOrWhiteSpace(code))
                Internal_JoinByCode(code);
        }

        private void Internal_JoinByCode(string code)
        {
            if (string.Equals(NetworkHelper.GetServerCode(), code, StringComparison.OrdinalIgnoreCase))
            {
                ErrorNotif("You are already in the lobby!");
                return;
            }

            if (NetworkLayerManager.Layer.Matchmaker != null)
            {
                NetworkLayerManager.Layer.Matchmaker.RequestLobbiesByCode(code, x => AttemptJoin(x, code));
            }
            else
            {
                if (IsConnected)
                    NetworkHelper.Disconnect("Joining another lobby");

                NetworkHelper.JoinServerByCode(code);
            }
        }

        private void AttemptJoin(IMatchmaker.MatchmakerCallbackInfo x, string code)
        {
            LobbyInfo targetLobby = x.Lobbies.FirstOrDefault().Metadata.LobbyInfo;


            if (targetLobby == null || targetLobby.LobbyCode == null)
            {
                LoggerInstance.Error("The lobby was not found");
                ErrorNotif("The lobby you wanted to join was not found!");
                return;
            }

            if (targetLobby.Privacy == ServerPrivacy.FRIENDS_ONLY)
            {
                var host = targetLobby.PlayerList?.Players?.FirstOrDefault(x => x.Username == targetLobby.LobbyHostName);
                if (host == null)
                {
                    LoggerInstance.Warning("Could not find host, unable to verify if you can join the lobby (Privacy: Friends Only)");
                }
                else if (!NetworkLayerManager.Layer.IsFriend(host.PlatformID))
                {
                    LoggerInstance.Error("The lobby is friends only and you are not friends with the host, cannot join");
                    ErrorNotif("Cannot join the lobby, because it is friends only and you are not friends with the host!");
                    return;

                }
            }

            if (targetLobby.Privacy == ServerPrivacy.LOCKED)
            {
                LoggerInstance.Error("The lobby is locked, cannot join");
                ErrorNotif("Cannot join the lobby, because it is locked");
                return;
            }

            if (IsConnected)
                NetworkHelper.Disconnect("Joining another lobby");

            NetworkHelper.JoinServerByCode(code);
        }

        private static void ErrorNotif(string msg, float length = 3.5f)
        {
            Notifier.Send(new Notification()
            {
                Title = "Error | FLB",
                Message = msg,
                PopupLength = length,
                ShowPopup = true,
                SaveToMenu = false,
                Type = NotificationType.ERROR
            });
        }
    }
}