using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateBefore(typeof(GroupReceiveDrink))]
    public class TrackPendingDrinkServes : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<CWantsDrink>(),
                ComponentType.ReadOnly<CAssignedTable>());

            using (NativeArray<Entity> groups = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < groups.Length; index++)
                {
                    Entity group = groups[index];
                    if (!EntityManager.Exists(group))
                    {
                        continue;
                    }

                    CWantsDrink wantsDrink = EntityManager.GetComponentData<CWantsDrink>(group);
                    if (wantsDrink.TimeToNextDrink > 0f)
                    {
                        continue;
                    }

                    Entity tableSet = EntityManager.GetComponentData<CAssignedTable>(group);
                    RememberDrinksOnGrabPoints(group, tableSet, wantsDrink.TimeToNextDrink);
                }
            }
        }

        private void RememberDrinksOnGrabPoints(Entity group, Entity tableSet, float timeToNextDrink)
        {
            if (!EntityManager.Exists(tableSet) || !EntityManager.HasComponent<CTableSetGrabPoints>(tableSet))
            {
                return;
            }

            DynamicBuffer<CTableSetGrabPoints> grabPoints = EntityManager.GetBuffer<CTableSetGrabPoints>(tableSet);
            for (int index = 0; index < grabPoints.Length; index++)
            {
                Entity grabPoint = grabPoints[index];
                if (!EntityManager.Exists(grabPoint) || !EntityManager.HasComponent<CItemHolder>(grabPoint))
                {
                    continue;
                }

                Entity heldItem = EntityManager.GetComponentData<CItemHolder>(grabPoint).HeldItem;
                if (heldItem == Entity.Null ||
                    !EntityManager.Exists(heldItem) ||
                    !EntityManager.HasComponent<CDrink>(heldItem))
                {
                    continue;
                }

                Entity player;
                if (!ServiceStatsEntityHelpers.TryResolvePlayerFromSource(EntityManager, heldItem, out player) &&
                    !ServiceStatsRuntime.TryResolveRememberedItemOwner(EntityManager, heldItem, out player))
                {
                    continue;
                }

                ServiceStatsRuntime.RememberPotentialDrinkServe(EntityManager, group, heldItem, player, timeToNextDrink);
            }
        }
    }

    [UpdateAfter(typeof(GroupReceiveDrink))]
    public class TrackCompletedDrinkServes : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            ServiceStatsRuntime.CompletePotentialDrinkServes(EntityManager);
        }
    }
}
