using System;
using System.Collections;
using System.Linq;

using Il2CppSLZ.Marrow.SceneStreaming;

using LabFusion.Data;
using LabFusion.Network;
using LabFusion.UI.Popups;

using MelonLoader;

namespace FLB.Managers
{
    internal static class FusionManager
    {
        public static bool HasFusion => Core.FindMelon("LabFusion", "Lakatrazz") != null;

        public static bool IsConnected => NetworkInfo.HasServer;

        public static void Join(string code, string layer)
        {
            void join()
            {
                Core.Logger.Msg($"Attempting to join with the following code: {code}");
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
                    Core.Logger.Error("Player is already in the lobby");
                    ErrorNotif("Could not join, because you are already in the lobby!");
                }
            }

            MelonCoroutines.Start(AfterLevelLoaded(join));
        }

        internal static void ArgJoin(string arg)
        {
            if (!HasFusion)
                return;

            try
            {
                Core.Logger.Msg("Received Join Request");
                string[] split = arg.Split("-");

                if (split.Length <= 1)
                    throw new ArgumentException("Secret provided to join the lobby did not include all of the necessary info");

                if (split.Length > 2)
                    throw new ArgumentException("Secret provided to join the lobby was invalid, the name of the network layer or code to the server may have contained the '-' character used to separate network layer & code, causing unexpected results");

                string layer = split[0];
                string code = split[1];

                void join()
                {
                    Core.Logger.Msg($"Attempting to join with the following code: {code}");
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
                        Core.Logger.Error("Player is already in the lobby");
                        ErrorNotif("Could not join, because you are already in the lobby!");
                    }
                }

                MelonCoroutines.Start(AfterLevelLoaded(join));
            }
            catch (Exception ex)
            {
                ErrorNotif("An unexpected error has occurred while trying to join the lobby, check the console or logs for more details", 5f);
                Core.Logger.Error("An unexpected error has occurred while trying to join the lobby", ex);
            }
        }

        private static IEnumerator AfterLevelLoaded(Action callback)
        {
            while (SceneStreamer.Session?.Status != StreamStatus.DONE)
                yield return null;

            callback?.Invoke();
        }

        public static bool EnsureNetworkLayer(string title)
        {
            if (!HasFusion)
                return false;
            else
                return Internal_EnsureNetworkLayer(title);
        }

        private static bool Internal_EnsureNetworkLayer(string title)
        {
            if (!NetworkLayer.LayerLookup.TryGetValue(title, out var layer))
            {
                Core.Logger.Error($"Could find network layer '{title}'");
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
                Core.Logger.Error("An unexpected error has occurred while ensuring fusion is on the right network layer, exception", ex);
                return false;
            }

            return true;
        }

        public static void JoinByCode(string code)
        {
            if (HasFusion && !string.IsNullOrWhiteSpace(code))
                Internal_JoinByCode(code);
        }

        private static void Internal_JoinByCode(string code)
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

        private static void AttemptJoin(IMatchmaker.MatchmakerCallbackInfo x, string code)
        {
            LobbyInfo targetLobby = x.Lobbies.FirstOrDefault().Metadata.LobbyInfo;

            if (targetLobby == null || targetLobby.LobbyCode == null)
            {
                Core.Logger.Error("The lobby was not found");
                ErrorNotif("The lobby you wanted to join was not found!");
                return;
            }

            if (targetLobby.Privacy == ServerPrivacy.FRIENDS_ONLY)
            {
                var host = targetLobby.PlayerList?.Players?.FirstOrDefault(x => x.Username == targetLobby.LobbyHostName);
                if (host == null)
                {
                    Core.Logger.Warning("Could not find host, unable to verify if you can join the lobby (Privacy: Friends Only)");
                }
                else if (!NetworkLayerManager.Layer.IsFriend(host.PlatformID))
                {
                    Core.Logger.Error("The lobby is friends only and you are not friends with the host, cannot join");
                    ErrorNotif("Cannot join the lobby, because it is friends only and you are not friends with the host!");
                    return;
                }
            }

            if (targetLobby.Privacy == ServerPrivacy.LOCKED)
            {
                Core.Logger.Error("The lobby is locked, cannot join");
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