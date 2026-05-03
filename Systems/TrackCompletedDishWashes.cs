using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateInGroup(typeof(HighPriorityInteractionGroup))]
    [UpdateAfter(typeof(UpdateTakesDuration))]
    public class TrackActiveCleaningProcesses : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            CaptureActiveCleaningProcesses();
            ServiceStatsRuntime.RemoveStaleCleaningProcesses(EntityManager);
        }

        private void CaptureActiveCleaningProcesses()
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<CItemUndergoingProcess>());
            using (NativeArray<Entity> items = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < items.Length; index++)
                {
                    Entity item = items[index];
                    if (!EntityManager.Exists(item))
                    {
                        continue;
                    }

                    CItemUndergoingProcess process = EntityManager.GetComponentData<CItemUndergoingProcess>(item);
                    ServiceStatsRuntime.RememberCleaningProcess(EntityManager, item, process);
                }
            }
        }
    }

    [UpdateInGroup(typeof(ApplianceProcessReactionGroup))]
    [UpdateBefore(typeof(ClearProcessComplete))]
    public class TrackCompletedDishWashes : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            ConsumeCompletedCleaningProcesses();
            ServiceStatsRuntime.RemoveStaleCleaningProcesses(EntityManager);
        }

        private void ConsumeCompletedCleaningProcesses()
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<CCompletedProcess>());
            using (NativeArray<Entity> items = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < items.Length; index++)
                {
                    Entity item = items[index];
                    if (!EntityManager.Exists(item))
                    {
                        continue;
                    }

                    CCompletedProcess completion = EntityManager.GetComponentData<CCompletedProcess>(item);
                    Entity actor;
                    bool isCleaningAppliance;
                    bool actionAlreadyRecorded;
                    if (!ServiceStatsRuntime.TryConsumeCompletedCleaningProcess(EntityManager, ServiceStatsRuntime.GetProcessSnapshotKey(item), completion, out actor, out isCleaningAppliance, out actionAlreadyRecorded))
                    {
                        if (!EntityManager.HasComponent<CItemUndergoingProcess>(item))
                        {
                            continue;
                        }

                        ServiceStatsRuntime.RememberCleaningProcess(EntityManager, item, EntityManager.GetComponentData<CItemUndergoingProcess>(item));
                        if (!ServiceStatsRuntime.TryConsumeCompletedCleaningProcess(EntityManager, ServiceStatsRuntime.GetProcessSnapshotKey(item), completion, out actor, out isCleaningAppliance, out actionAlreadyRecorded))
                        {
                            continue;
                        }
                    }

                    ServiceStatsRuntime.RecordCompletedCleaningProcess(EntityManager, actor, isCleaningAppliance, actionAlreadyRecorded);
                }
            }
        }
    }
}
