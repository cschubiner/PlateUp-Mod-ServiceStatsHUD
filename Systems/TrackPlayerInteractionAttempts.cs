using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateInGroup(typeof(InteractionGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public class TrackPlayerInteractionAttempts : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<CPlayer>(),
                ComponentType.ReadOnly<CAttemptingInteraction>());

            using (NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < players.Length; index++)
                {
                    Entity player = players[index];
                    if (!EntityManager.Exists(player))
                    {
                        continue;
                    }

                    CAttemptingInteraction interaction = EntityManager.GetComponentData<CAttemptingInteraction>(player);
                    ServiceStatsRuntime.RecordInteractionAttemptSample(EntityManager, player, interaction);
                }
            }
        }
    }
}
