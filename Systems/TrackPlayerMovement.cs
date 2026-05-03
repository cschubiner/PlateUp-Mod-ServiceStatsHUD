using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenServiceStatsHUD.Systems
{
    public class TrackPlayerMovement : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<CPlayer>(),
                ComponentType.ReadOnly<CPosition>());

            using (NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < players.Length; index++)
                {
                    Entity player = players[index];
                    if (!ServiceStatsEntityHelpers.IsValidPlayer(EntityManager, player))
                    {
                        continue;
                    }

                    CPosition position = EntityManager.GetComponentData<CPosition>(player);
                    ServiceStatsRuntime.RecordMovementSample(EntityManager, player, position.Position);
                    ServiceStatsRuntime.RecordIdleSample(EntityManager, player, UnityEngine.Time.deltaTime);
                }
            }
        }
    }
}
