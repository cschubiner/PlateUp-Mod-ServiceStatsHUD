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
            PlayerProfile profile;
            if (TryGetProfileForPlayerId(playerId, out profile) && !string.IsNullOrWhiteSpace(profile.Name))
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

            PlayerProfile profile;
            if (TryGetProfileForPlayerId(playerId, out profile))
            {
                return profile.Colour;
            }

            return GetFallbackColor(playerId);
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
