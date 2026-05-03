using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateInGroup(typeof(PostResolveSatisfactionsGroup))]
    [UpdateAfter(typeof(GrantMoneyForSatisfactions))]
    [UpdateBefore(typeof(CleanAcceptances))]
    public class TrackServedDishesFromAcceptances : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            TrackFullOrderAcceptances();
            TrackPartialOrderAcceptances();
        }

        private void TrackFullOrderAcceptances()
        {
            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<CItemTransferAccept>(),
                ComponentType.ReadOnly<COrderAcceptance>());

            using (NativeArray<Entity> acceptances = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < acceptances.Length; index++)
                {
                    Entity acceptanceEntity = acceptances[index];
                    if (!EntityManager.Exists(acceptanceEntity))
                    {
                        continue;
                    }

                    CItemTransferAccept acceptance = EntityManager.GetComponentData<CItemTransferAccept>(acceptanceEntity);
                    COrderAcceptance details = EntityManager.GetComponentData<COrderAcceptance>(acceptanceEntity);
                    CItemTransferProposal proposal;
                    if (!TryGetProposal(acceptance, acceptanceEntity, out proposal))
                    {
                        continue;
                    }

                    ServiceStatsRuntime.RecordResolvedServe(EntityManager, acceptance, proposal, details.Source, acceptanceEntity);
                }
            }
        }

        private void TrackPartialOrderAcceptances()
        {
            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<CItemTransferAccept>(),
                ComponentType.ReadOnly<CPartialOrderAcceptance>());

            using (NativeArray<Entity> acceptances = query.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < acceptances.Length; index++)
                {
                    Entity acceptanceEntity = acceptances[index];
                    if (!EntityManager.Exists(acceptanceEntity))
                    {
                        continue;
                    }

                    CItemTransferAccept acceptance = EntityManager.GetComponentData<CItemTransferAccept>(acceptanceEntity);
                    CPartialOrderAcceptance details = EntityManager.GetComponentData<CPartialOrderAcceptance>(acceptanceEntity);
                    CItemTransferProposal proposal;
                    if (!TryGetProposal(acceptance, acceptanceEntity, out proposal))
                    {
                        continue;
                    }

                    ServiceStatsRuntime.RecordResolvedServe(EntityManager, acceptance, proposal, details.Source, acceptanceEntity);
                }
            }
        }

        private bool TryGetProposal(CItemTransferAccept acceptance, Entity acceptanceEntity, out CItemTransferProposal proposal)
        {
            proposal = default(CItemTransferProposal);
            Entity proposalEntity = acceptance.Proposal;
            if (EntityManager.Exists(proposalEntity) && EntityManager.HasComponent<CItemTransferProposal>(proposalEntity))
            {
                proposal = EntityManager.GetComponentData<CItemTransferProposal>(proposalEntity);
                return true;
            }

            if (EntityManager.Exists(acceptanceEntity) && EntityManager.HasComponent<CItemTransferProposal>(acceptanceEntity))
            {
                proposal = EntityManager.GetComponentData<CItemTransferProposal>(acceptanceEntity);
                return true;
            }

            return false;
        }
    }
}
