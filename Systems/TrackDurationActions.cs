using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateInGroup(typeof(HighPriorityInteractionGroup))]
    [UpdateAfter(typeof(UpdateTakesDuration))]
    public class TrackDurationActions : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<CTakesDuration>());
            using (NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < entities.Length; index++)
                {
                    Entity entity = entities[index];
                    if (!EntityManager.Exists(entity))
                    {
                        continue;
                    }

                    CTakesDuration duration = EntityManager.GetComponentData<CTakesDuration>(entity);
                    Entity actor;
                    if (!TryResolveDurationActor(entity, out actor))
                    {
                        ServiceStatsRuntime.RemoveInactiveDurationAction(entity);
                        continue;
                    }

                    ServiceStatsRuntime.RecordDurationActionSample(EntityManager, entity, duration, actor);
                }
            }
        }

        private bool TryResolveDurationActor(Entity entity, out Entity actor)
        {
            if (TryResolveActorOnEntity(entity, out actor))
            {
                return true;
            }

            if (EntityManager.HasComponent<CDurationInteractionProxy>(entity))
            {
                Entity proxy = EntityManager.GetComponentData<CDurationInteractionProxy>(entity).Proxy;
                if (TryResolveActorOnEntity(proxy, out actor))
                {
                    return true;
                }
            }

            actor = Entity.Null;
            return false;
        }

        private bool TryResolveActorOnEntity(Entity entity, out Entity actor)
        {
            actor = Entity.Null;
            if (!EntityManager.Exists(entity))
            {
                return false;
            }

            if (EntityManager.HasComponent<CBeingActedOnBy>(entity))
            {
                DynamicBuffer<CBeingActedOnBy> actors = EntityManager.GetBuffer<CBeingActedOnBy>(entity);
                if (actors.Length > 0)
                {
                    actor = actors[0].Interactor;
                    return actor != Entity.Null;
                }
            }

            if (EntityManager.HasComponent<CToolInUse>(entity))
            {
                actor = EntityManager.GetComponentData<CToolInUse>(entity).User;
                return actor != Entity.Null;
            }

            return false;
        }
    }
}
