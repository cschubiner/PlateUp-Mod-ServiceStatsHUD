using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Systems
{
    [UpdateInGroup(typeof(PostResolveSatisfactionsGroup))]
    [UpdateBefore(typeof(CleanAcceptances))]
    public class TrackDrinkServesFromAcceptedTransfers : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            if (!Has<SIsDayTime>())
            {
                return;
            }

            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<CItemTransferAccept>());
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
                    CItemTransferProposal proposal;
                    if (!TryGetProposal(acceptance, acceptanceEntity, out proposal))
                    {
                        continue;
                    }

                    ServiceStatsRuntime.RecordResolvedDrinkDeliveryServe(EntityManager, acceptance, proposal, acceptanceEntity);
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
