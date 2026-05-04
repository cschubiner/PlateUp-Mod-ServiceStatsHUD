using Kitchen;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsDisplayResolver
    {
        private static readonly Color[] FallbackPalette = new[]
        {
            new Color(0.95f, 0.55f, 0.29f, 1f),
            new Color(0.29f, 0.72f, 0.95f, 1f),
            new Color(0.42f, 0.84f, 0.58f, 1f),
            new Color(0.95f, 0.80f, 0.30f, 1f),
            new Color(0.83f, 0.53f, 0.96f, 1f),
            new Color(0.96f, 0.43f, 0.58f, 1f),
            new Color(0.47f, 0.88f, 0.87f, 1f),
            new Color(0.97f, 0.64f, 0.46f, 1f)
        };

        public static string ResolveName(int playerId)
        {
            PlayerInfo playerInfo;
            if (TryGetPlayerInfoForPlayerId(playerId, out playerInfo) && !string.IsNullOrWhiteSpace(playerInfo.Name))
            {
                return playerInfo.Name;
            }

            PlayerProfile profile;
            if (TryGetProfileForPlayerId(playerId, out profile) && !string.IsNullOrWhiteSpace(profile.Name))
            {
                return profile.Name;
            }

            return null;
        }

        public static string ResolveName(EntityManager entityManager, Entity player, int playerId, int displayIndex)
        {
            PlayerInfo playerInfo;
            if (TryGetPlayerInfoForPlayerId(playerId, out playerInfo) && !string.IsNullOrWhiteSpace(playerInfo.Name))
            {
                return playerInfo.Name;
            }

            if (TryGetPlayerInfoForDisplayIndex(displayIndex, out playerInfo) && !string.IsNullOrWhiteSpace(playerInfo.Name))
            {
                return playerInfo.Name;
            }

            PlayerProfile profile;
            if (entityManager.Exists(player) &&
                entityManager.HasComponent<CSetPlayerProfile>(player) &&
                TryGetProfileForPlayerId(entityManager.GetComponentData<CSetPlayerProfile>(player).PlayerID, out profile) &&
                !string.IsNullOrWhiteSpace(profile.Name))
            {
                return profile.Name;
            }

            if (TryGetProfileForPlayerId(playerId, out profile) && !string.IsNullOrWhiteSpace(profile.Name))
            {
                return profile.Name;
            }

            if (TryGetProfileForDisplayIndex(displayIndex, out profile) && !string.IsNullOrWhiteSpace(profile.Name))
            {
                return profile.Name;
            }

            return null;
        }

        public static Color ResolveColor(EntityManager entityManager, Entity player, int playerId)
        {
            if (entityManager.Exists(player))
            {
                if (entityManager.HasComponent<CPlayerColour>(player))
                {
                    return entityManager.GetComponentData<CPlayerColour>(player).Color;
                }

                if (entityManager.HasComponent<CSetPlayerProfile>(player))
                {
                    return entityManager.GetComponentData<CSetPlayerProfile>(player).Colour;
                }
            }

            PlayerInfo playerInfo;
            if (TryGetPlayerInfoForPlayerId(playerId, out playerInfo) && playerInfo.HasProfile)
            {
                return playerInfo.Profile.Colour;
            }

            PlayerProfile profile;
            if (TryGetProfileForPlayerId(playerId, out profile))
            {
                return profile.Colour;
            }

            return GetFallbackColor(playerId);
        }

        private static bool TryGetPlayerInfoForPlayerId(int playerId, out PlayerInfo playerInfo)
        {
            playerInfo = default(PlayerInfo);
            try
            {
                if (Players.Main == null || !Players.Main.Has(playerId))
                {
                    return false;
                }

                playerInfo = Players.Main.Get(playerId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetPlayerInfoForDisplayIndex(int displayIndex, out PlayerInfo playerInfo)
        {
            playerInfo = default(PlayerInfo);
            try
            {
                if (displayIndex < 0 || Players.Main == null)
                {
                    return false;
                }

                List<PlayerInfo> allPlayers = Players.Main.All();
                if (allPlayers == null)
                {
                    return false;
                }

                for (int index = 0; index < allPlayers.Count; index++)
                {
                    PlayerInfo candidate = allPlayers[index];
                    if (candidate.Index != displayIndex)
                    {
                        continue;
                    }

                    playerInfo = candidate;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGetProfileForPlayerId(int playerId, out PlayerProfile profile)
        {
            profile = default(PlayerProfile);
            List<RetainedPlayer> retainedPlayers = Session.RetainedPlayers;
            if (retainedPlayers == null || ProfileStore.Main == null)
            {
                return false;
            }

            for (int index = 0; index < retainedPlayers.Count; index++)
            {
                RetainedPlayer retained = retainedPlayers[index];
                if (retained.InputPlayerID != playerId)
                {
                    continue;
                }

                return ProfileStore.Main.TryGetProfile(retained.PlayerProfile, out profile);
            }

            return false;
        }

        private static bool TryGetProfileForDisplayIndex(int displayIndex, out PlayerProfile profile)
        {
            profile = default(PlayerProfile);
            List<RetainedPlayer> retainedPlayers = Session.RetainedPlayers;
            if (displayIndex < 0 ||
                retainedPlayers == null ||
                displayIndex >= retainedPlayers.Count ||
                ProfileStore.Main == null)
            {
                return false;
            }

            return ProfileStore.Main.TryGetProfile(retainedPlayers[displayIndex].PlayerProfile, out profile);
        }

        private static Color GetFallbackColor(int playerId)
        {
            int paletteIndex = playerId % FallbackPalette.Length;
            if (paletteIndex < 0)
            {
                paletteIndex += FallbackPalette.Length;
            }

            return FallbackPalette[paletteIndex];
        }
    }
}
