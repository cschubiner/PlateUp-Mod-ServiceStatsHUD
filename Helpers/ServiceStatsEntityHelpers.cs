using Kitchen;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsEntityHelpers
    {
        public static bool IsValidPlayer(EntityManager entityManager, Entity entity)
        {
            return entityManager.Exists(entity) &&
                   entityManager.HasComponent<CPlayer>(entity) &&
                   entityManager.HasComponent<CPosition>(entity) &&
                   !entityManager.HasComponent<CUnplacedPlayer>(entity);
        }

        public static bool TryGetPlayerId(EntityManager entityManager, Entity player, out int playerId)
        {
            playerId = 0;
            if (!IsValidPlayer(entityManager, player))
            {
                return false;
            }

            playerId = entityManager.GetComponentData<CPlayer>(player).ID;
            return true;
        }

        public static bool TryResolveServingPlayerFromTransfer(EntityManager entityManager, Entity transfer, out Entity player)
        {
            player = Entity.Null;
            if (!entityManager.Exists(transfer) || !entityManager.HasComponent<CItemTransferProposal>(transfer))
            {
                return false;
            }

            CItemTransferProposal proposal = entityManager.GetComponentData<CItemTransferProposal>(transfer);
            return TryResolveServingPlayer(entityManager, proposal, out player);
        }

        public static bool TryResolvePlayerFromTransfer(EntityManager entityManager, Entity transfer, Entity acceptance, out Entity player)
        {
            if (TryResolveServingPlayerFromTransfer(entityManager, transfer, out player))
            {
                return true;
            }

            if (TryResolvePlayerFromSource(entityManager, transfer, out player))
            {
                return true;
            }

            if (TryResolvePlayerFromSource(entityManager, acceptance, out player))
            {
                return true;
            }

            player = Entity.Null;
            return false;
        }

        internal static bool TryPrepareOrderServeAcceptance(EntityManager entityManager, Entity proposalEntity, Entity acceptance, bool isExtra, out ServiceStatsServeAcceptanceState state)
        {
            state = default(ServiceStatsServeAcceptanceState);

            Entity player;
            if (!TryResolveServingPlayerFromTransfer(entityManager, proposalEntity, out player) ||
                !entityManager.Exists(acceptance) ||
                !entityManager.HasComponent<COrderAcceptance>(acceptance) ||
                !entityManager.Exists(proposalEntity) ||
                !entityManager.HasComponent<CItemTransferProposal>(proposalEntity))
            {
                return false;
            }

            COrderAcceptance orderAcceptance = entityManager.GetComponentData<COrderAcceptance>(acceptance);
            CItemTransferProposal proposal = entityManager.GetComponentData<CItemTransferProposal>(proposalEntity);
            Entity tableSet;
            Entity group;
            if (orderAcceptance.OrderIndex < 0 ||
                !TryResolveOrderTarget(entityManager, orderAcceptance.TableSet, orderAcceptance.Group, proposal.Destination, out tableSet, out group))
            {
                return false;
            }

            if (!entityManager.Exists(group) ||
                !entityManager.HasComponent<CWaitingForItem>(group))
            {
                return false;
            }

            DynamicBuffer<CWaitingForItem> waitingItems = entityManager.GetBuffer<CWaitingForItem>(group);
            if (orderAcceptance.OrderIndex >= waitingItems.Length)
            {
                return false;
            }

            CWaitingForItem waitingItem = waitingItems[orderAcceptance.OrderIndex];
            state = new ServiceStatsServeAcceptanceState
            {
                ShouldRecord = true,
                Transfer = proposalEntity,
                Acceptance = acceptance,
                Player = player,
                Group = group,
                OrderIndex = orderAcceptance.OrderIndex,
                IsExtra = isExtra,
                WasSatisfied = isExtra ? waitingItem.ExtraSatisfied : waitingItem.Satisfied
            };
            return true;
        }

        internal static bool TryPreparePartialServeAcceptance(EntityManager entityManager, Entity proposalEntity, Entity acceptance, out ServiceStatsServeAcceptanceState state)
        {
            state = default(ServiceStatsServeAcceptanceState);

            Entity player;
            if (!TryResolveServingPlayerFromTransfer(entityManager, proposalEntity, out player) ||
                !entityManager.Exists(acceptance) ||
                !entityManager.HasComponent<CPartialOrderAcceptance>(acceptance) ||
                !entityManager.Exists(proposalEntity) ||
                !entityManager.HasComponent<CItemTransferProposal>(proposalEntity))
            {
                return false;
            }

            CPartialOrderAcceptance partialAcceptance = entityManager.GetComponentData<CPartialOrderAcceptance>(acceptance);
            CItemTransferProposal proposal = entityManager.GetComponentData<CItemTransferProposal>(proposalEntity);
            Entity tableSet;
            Entity group;
            if (partialAcceptance.OrderIndex < 0 ||
                !TryResolveOrderTarget(entityManager, Entity.Null, partialAcceptance.Group, proposal.Destination, out tableSet, out group))
            {
                return false;
            }

            if (!entityManager.Exists(group) ||
                !entityManager.HasComponent<CWaitingForItem>(group))
            {
                return false;
            }

            DynamicBuffer<CWaitingForItem> waitingItems = entityManager.GetBuffer<CWaitingForItem>(group);
            if (partialAcceptance.OrderIndex >= waitingItems.Length ||
                !entityManager.Exists(waitingItems[partialAcceptance.OrderIndex].Item) ||
                !entityManager.HasComponent<CItem>(waitingItems[partialAcceptance.OrderIndex].Item))
            {
                return false;
            }

            state = new ServiceStatsServeAcceptanceState
            {
                ShouldRecord = true,
                Transfer = proposalEntity,
                Acceptance = acceptance,
                Player = player,
                Group = group,
                OrderIndex = partialAcceptance.OrderIndex,
                IsExtra = false,
                WasSatisfied = false
            };
            return true;
        }

        internal static bool DidOrderServeAcceptanceComplete(EntityManager entityManager, ServiceStatsServeAcceptanceState state)
        {
            if (!state.ShouldRecord ||
                !entityManager.Exists(state.Group) ||
                !entityManager.HasComponent<CWaitingForItem>(state.Group))
            {
                return false;
            }

            DynamicBuffer<CWaitingForItem> waitingItems = entityManager.GetBuffer<CWaitingForItem>(state.Group);
            if (state.OrderIndex < 0 || state.OrderIndex >= waitingItems.Length)
            {
                return false;
            }

            CWaitingForItem waitingItem = waitingItems[state.OrderIndex];
            bool isSatisfied = state.IsExtra ? waitingItem.ExtraSatisfied : waitingItem.Satisfied;
            return !state.WasSatisfied && isSatisfied;
        }

        public static bool TryResolvePlayerFromSource(EntityManager entityManager, Entity source, out Entity player)
        {
            player = Entity.Null;

            if (IsValidPlayer(entityManager, source))
            {
                player = source;
                return true;
            }

            if (TryResolveOwnedPlayer(entityManager, source, out player))
            {
                return true;
            }

            if (TryResolveHeldPlayer(entityManager, source, out player))
            {
                return true;
            }

            if (entityManager.Exists(source) && entityManager.HasComponent<CInteractionTransferProposal>(source))
            {
                Entity interactor = entityManager.GetComponentData<CInteractionTransferProposal>(source).Interactor;
                if (interactor != source && TryResolvePlayerFromSource(entityManager, interactor, out player))
                {
                    return true;
                }
            }

            if (entityManager.Exists(source) && entityManager.HasComponent<CItemHolder>(source))
            {
                Entity heldItem = entityManager.GetComponentData<CItemHolder>(source).HeldItem;
                if (TryResolveOwnedPlayer(entityManager, heldItem, out player) ||
                    TryResolveHeldPlayer(entityManager, heldItem, out player) ||
                    IsValidPlayer(entityManager, heldItem))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryResolveOrderTarget(EntityManager entityManager, Entity acceptanceTableSet, Entity acceptanceGroup, Entity proposalDestination, out Entity tableSet, out Entity group)
        {
            tableSet = Entity.Null;
            group = Entity.Null;

            if (entityManager.Exists(acceptanceGroup) && entityManager.HasComponent<CWaitingForItem>(acceptanceGroup))
            {
                group = acceptanceGroup;
                if (entityManager.Exists(acceptanceTableSet))
                {
                    tableSet = acceptanceTableSet;
                }
                return true;
            }

            if (TryResolveTableSet(entityManager, acceptanceTableSet, out tableSet) ||
                TryResolveTableSet(entityManager, proposalDestination, out tableSet))
            {
                if (entityManager.Exists(tableSet) && entityManager.HasComponent<COccupiedByGroup>(tableSet))
                {
                    group = entityManager.GetComponentData<COccupiedByGroup>(tableSet);
                    return group != Entity.Null;
                }
            }

            return false;
        }

        public static bool TryResolveTableSet(EntityManager entityManager, Entity entity, out Entity tableSet)
        {
            tableSet = Entity.Null;
            if (!entityManager.Exists(entity))
            {
                return false;
            }

            if (entityManager.HasComponent<CPartOfTableSet>(entity))
            {
                tableSet = entityManager.GetComponentData<CPartOfTableSet>(entity).TableSet;
                return tableSet != Entity.Null;
            }

            if (entityManager.HasComponent<CTableSet>(entity))
            {
                tableSet = entity;
                return true;
            }

            return false;
        }

        public static bool TryResolveServingPlayer(EntityManager entityManager, CItemTransferProposal proposal, out Entity player)
        {
            player = Entity.Null;

            if (TryResolveOwnedPlayer(entityManager, proposal.Item, out player))
            {
                return true;
            }

            if (TryResolveHeldPlayer(entityManager, proposal.Item, out player))
            {
                return true;
            }

            if (IsValidPlayer(entityManager, proposal.Source))
            {
                player = proposal.Source;
                return true;
            }

            if (TryResolveOwnedPlayer(entityManager, proposal.Source, out player))
            {
                return true;
            }

            if (entityManager.Exists(proposal.Source) && entityManager.HasComponent<CItemHolder>(proposal.Source))
            {
                Entity heldItem = entityManager.GetComponentData<CItemHolder>(proposal.Source).HeldItem;
                if (TryResolveOwnedPlayer(entityManager, heldItem, out player) ||
                    TryResolveHeldPlayer(entityManager, heldItem, out player))
                {
                    return true;
                }
            }

            if (IsValidPlayer(entityManager, proposal.Destination))
            {
                player = proposal.Destination;
                return true;
            }

            if (TryResolveOwnedPlayer(entityManager, proposal.Destination, out player) ||
                TryResolveHeldPlayer(entityManager, proposal.Destination, out player))
            {
                return true;
            }

            if (entityManager.Exists(proposal.Destination) && entityManager.HasComponent<CItemHolder>(proposal.Destination))
            {
                Entity heldItem = entityManager.GetComponentData<CItemHolder>(proposal.Destination).HeldItem;
                if (TryResolveOwnedPlayer(entityManager, heldItem, out player) ||
                    TryResolveHeldPlayer(entityManager, heldItem, out player) ||
                    IsValidPlayer(entityManager, heldItem))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveOwnedPlayer(EntityManager entityManager, Entity entity, out Entity player)
        {
            player = Entity.Null;
            if (!entityManager.Exists(entity) || !entityManager.HasComponent<COwnedByPlayer>(entity))
            {
                return false;
            }

            Entity ownedPlayer = entityManager.GetComponentData<COwnedByPlayer>(entity).Player;
            if (!IsValidPlayer(entityManager, ownedPlayer))
            {
                return false;
            }

            player = ownedPlayer;
            return true;
        }

        private static bool TryResolveHeldPlayer(EntityManager entityManager, Entity entity, out Entity player)
        {
            player = Entity.Null;
            if (!entityManager.Exists(entity) || !entityManager.HasComponent<CHeldBy>(entity))
            {
                return false;
            }

            Entity holder = entityManager.GetComponentData<CHeldBy>(entity).Holder;
            if (!IsValidPlayer(entityManager, holder))
            {
                return false;
            }

            player = holder;
            return true;
        }

        public static string DescribeEntity(Entity entity)
        {
            if (entity == Entity.Null)
            {
                return "Null";
            }

            return entity.Index + ":" + entity.Version;
        }
    }
}
