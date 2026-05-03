using Kitchen;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsRuntime
    {
        private const float MaxMovementSampleDistance = 3f;

        private static readonly Dictionary<int, ServiceStatsPlayerState> PlayerStats = new Dictionary<int, ServiceStatsPlayerState>();
        private static readonly Dictionary<int, ServiceStatsProcessSnapshot> CleaningProcesses = new Dictionary<int, ServiceStatsProcessSnapshot>();
        private static readonly Dictionary<int, ServiceStatsMovementSnapshot> MovementSnapshots = new Dictionary<int, ServiceStatsMovementSnapshot>();
        private static readonly Dictionary<int, int> ActiveInteractionAttempts = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> LastActionFrameByPlayer = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> LastWashFrameByPlayer = new Dictionary<int, int>();
        private static readonly Dictionary<int, Entity> ItemOwners = new Dictionary<int, Entity>();
        private static readonly HashSet<Entity> ProcessedServeAcceptances = new HashSet<Entity>();
        private static readonly HashSet<Entity> ProcessedActionTransfers = new HashSet<Entity>();
        private static readonly HashSet<Entity> ActiveDurationActions = new HashSet<Entity>();
        private static int _resolutionFailureLogs;
        private static bool _hasObservedDayPhase;
        private static bool _lastObservedDayPhase;

        public static void ResetAll()
        {
            PlayerStats.Clear();
            CleaningProcesses.Clear();
            MovementSnapshots.Clear();
            ActiveInteractionAttempts.Clear();
            LastActionFrameByPlayer.Clear();
            LastWashFrameByPlayer.Clear();
            ItemOwners.Clear();
            ProcessedServeAcceptances.Clear();
            ProcessedActionTransfers.Clear();
            ActiveDurationActions.Clear();
            _resolutionFailureLogs = 0;
        }

        public static void RefreshDayPhase(bool isDayTime)
        {
            if (!ServiceStatsLifecycleLogic.ShouldResetForDayPhase(isDayTime, ref _hasObservedDayPhase, ref _lastObservedDayPhase))
            {
                return;
            }

            ResetAll();
        }

        public static bool TryBeginServeAcceptance(Entity acceptanceEntity)
        {
            if (acceptanceEntity == Entity.Null || ProcessedServeAcceptances.Contains(acceptanceEntity))
            {
                return false;
            }

            ProcessedServeAcceptances.Add(acceptanceEntity);
            return true;
        }

        public static void NotePotentialPlayer(EntityManager entityManager, Entity player)
        {
            ServiceStatsPlayerState state;
            TryGetOrCreateState(entityManager, player, out state);
        }

        public static void RecordOrderTaken(EntityManager entityManager, Entity player)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            state.OrdersTaken++;
        }

        public static void RecordDishServed(EntityManager entityManager, Entity player)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            state.Served++;
        }

        public static bool TryRecordDishServedOnce(EntityManager entityManager, Entity acceptanceEntity, Entity player)
        {
            if (!TryBeginServeAcceptance(acceptanceEntity))
            {
                return false;
            }

            RecordDishServed(entityManager, player);
            return true;
        }

        public static bool TryRecordDishServedForTransferOnce(EntityManager entityManager, Entity transferEntity, Entity fallbackEntity, Entity player)
        {
            Entity serveKey = transferEntity == Entity.Null ? fallbackEntity : transferEntity;
            return TryRecordDishServedOnce(entityManager, serveKey, player);
        }

        public static void RecordResolvedServe(EntityManager entityManager, CItemTransferAccept acceptance, CItemTransferProposal proposal, Entity source, Entity fallbackEntity)
        {
            Entity player;
            bool hasPlayerActor = ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, source, out player) ||
                                  ServiceStatsEntityHelpers.TryResolveServingPlayer(entityManager, proposal, out player) ||
                                  TryResolveRememberedItemOwner(entityManager, proposal.Item, out player);
            bool itemStillHeldByPlayer = hasPlayerActor && ServiceStatsEntityHelpers.IsItemStillHeldByPlayer(entityManager, proposal.Item, player);

            if (!ServiceStatsHudLogic.ShouldRecordResolvedServe(acceptance.Status == ItemAcceptStatus.Accepted, hasPlayerActor, itemStillHeldByPlayer))
            {
                return;
            }

            RememberItemOwner(entityManager, proposal.Item, player);
            if (TryRecordDishServedForTransferOnce(entityManager, acceptance.Proposal, fallbackEntity, player))
            {
                TryRecordActionForTransferOnce(entityManager, acceptance.Proposal, player);
            }
        }

        public static void RecordResolvedDrinkDeliveryServe(EntityManager entityManager, CItemTransferAccept acceptance, CItemTransferProposal proposal, Entity fallbackEntity)
        {
            Entity player;
            bool hasPlayerActor = TryResolveDrinkDeliveryPlayer(entityManager, proposal, out player);
            bool isDrinkItem = entityManager.Exists(proposal.Item) && entityManager.HasComponent<CDrink>(proposal.Item);
            bool isCustomerDrinkDestination = IsCustomerDrinkDestination(entityManager, proposal.Destination);
            bool itemStillHeldByPlayer = hasPlayerActor && ServiceStatsEntityHelpers.IsItemStillHeldByPlayer(entityManager, proposal.Item, player);

            if (!ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(
                acceptance.Status == ItemAcceptStatus.Accepted,
                isDrinkItem,
                isCustomerDrinkDestination,
                hasPlayerActor,
                itemStillHeldByPlayer))
            {
                return;
            }

            RememberItemOwner(entityManager, proposal.Item, player);
            if (TryRecordDishServedForTransferOnce(entityManager, acceptance.Proposal, fallbackEntity, player))
            {
                TryRecordActionForTransferOnce(entityManager, acceptance.Proposal, player);
            }
        }

        public static void RecordDishWashed(EntityManager entityManager, Entity player)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            int currentFrame = Time.frameCount;
            int lastFrame;
            if (currentFrame > 0 &&
                LastWashFrameByPlayer.TryGetValue(state.PlayerId, out lastFrame) &&
                lastFrame == currentFrame)
            {
                return;
            }

            if (currentFrame > 0)
            {
                LastWashFrameByPlayer[state.PlayerId] = currentFrame;
            }

            state.DishesWashed++;
        }

        public static void RecordCompletedCleaningProcess(EntityManager entityManager, Entity player, bool isWashCleaningProcess)
        {
            RecordCompletedCleaningProcess(entityManager, player, isWashCleaningProcess, false);
        }

        public static void RecordCompletedCleaningProcess(EntityManager entityManager, Entity player, bool isWashCleaningProcess, bool actionAlreadyRecorded)
        {
            ServiceStatsCleaningCredit credit = ServiceStatsHudLogic.GetCompletedCleaningCredit(isWashCleaningProcess, actionAlreadyRecorded);
            if (credit.RecordAction)
            {
                RecordAction(entityManager, player);
            }

            if (credit.RecordWashed)
            {
                RecordDishWashed(entityManager, player);
            }
        }

        public static void RecordAction(EntityManager entityManager, Entity player)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            int currentFrame = Time.frameCount;
            int lastFrame;
            if (currentFrame > 0 &&
                LastActionFrameByPlayer.TryGetValue(state.PlayerId, out lastFrame) &&
                lastFrame == currentFrame)
            {
                return;
            }

            if (currentFrame > 0)
            {
                LastActionFrameByPlayer[state.PlayerId] = currentFrame;
            }

            state.ActionsPerformed++;
            state.SecondsSinceLastAction = 0f;
        }

        public static bool TryRecordActionForTransferOnce(EntityManager entityManager, Entity transferEntity, Entity player)
        {
            if (transferEntity == Entity.Null)
            {
                RecordAction(entityManager, player);
                return true;
            }

            if (ProcessedActionTransfers.Contains(transferEntity))
            {
                return false;
            }

            ProcessedActionTransfers.Add(transferEntity);
            RecordAction(entityManager, player);
            return true;
        }

        public static void RememberItemOwner(EntityManager entityManager, Entity item, Entity player)
        {
            if (item == Entity.Null ||
                !entityManager.Exists(item) ||
                !entityManager.HasComponent<CItem>(item) ||
                !ServiceStatsEntityHelpers.IsValidPlayer(entityManager, player))
            {
                return;
            }

            ItemOwners[item.Index] = player;
        }

        public static bool TryResolveRememberedItemOwner(EntityManager entityManager, Entity item, out Entity player)
        {
            player = Entity.Null;
            Entity rememberedPlayer;
            if (item == Entity.Null ||
                !ItemOwners.TryGetValue(item.Index, out rememberedPlayer) ||
                !ServiceStatsEntityHelpers.IsValidPlayer(entityManager, rememberedPlayer))
            {
                return false;
            }

            player = rememberedPlayer;
            return true;
        }

        public static void RecordDurationActionSample(EntityManager entityManager, Entity durationEntity, CTakesDuration duration, Entity player)
        {
            bool hasPlayerActor = ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, player, out player);
            bool alreadyActive = ActiveDurationActions.Contains(durationEntity);
            if (!ServiceStatsHudLogic.ShouldRecordDurationAction(duration.Active, duration.Manual, hasPlayerActor, alreadyActive))
            {
                if (!duration.Active)
                {
                    ActiveDurationActions.Remove(durationEntity);
                }
                return;
            }

            ActiveDurationActions.Add(durationEntity);
            RecordAction(entityManager, player);
        }

        public static void RemoveInactiveDurationAction(Entity durationEntity)
        {
            ActiveDurationActions.Remove(durationEntity);
        }

        public static void RecordInteractionAttemptSample(EntityManager entityManager, Entity player, CAttemptingInteraction interaction)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            if (!ServiceStatsHudLogic.ShouldRecordInteractionAttempt((int) interaction.Type, (int) interaction.Result))
            {
                ActiveInteractionAttempts.Remove(state.PlayerId);
                return;
            }

            RememberInteractionTargetOwner(entityManager, interaction.Target, player);

            int attemptKey = CalculateInteractionAttemptKey(interaction);
            int activeAttemptKey;
            if (ActiveInteractionAttempts.TryGetValue(state.PlayerId, out activeAttemptKey) &&
                activeAttemptKey == attemptKey)
            {
                return;
            }

            ActiveInteractionAttempts[state.PlayerId] = attemptKey;
            RecordAction(entityManager, player);

            if (ServiceStatsHudLogic.ShouldCreditWashFromInteractionAttempt(
                (int) interaction.Type,
                (int) interaction.Result,
                IsFloorMessCleaningInteractionTarget(entityManager, interaction.Target)))
            {
                RecordDishWashed(entityManager, player);
            }
        }

        private static int CalculateInteractionAttemptKey(CAttemptingInteraction interaction)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + interaction.Target.Index;
                hash = (hash * 31) + interaction.Target.Version;
                hash = (hash * 31) + (int) interaction.Type;
                hash = (hash * 31) + (int) interaction.Mode;
                hash = (hash * 31) + interaction.Process;
                hash = (hash * 31) + (interaction.IsHeld ? 1 : 0);
                hash = (hash * 31) + (interaction.TransferOnly ? 1 : 0);
                return hash;
            }
        }

        private static void RememberInteractionTargetOwner(EntityManager entityManager, Entity target, Entity player)
        {
            RememberItemOwner(entityManager, target, player);

            if (target == Entity.Null || !entityManager.Exists(target) || !entityManager.HasComponent<CItemHolder>(target))
            {
                return;
            }

            Entity heldItem = entityManager.GetComponentData<CItemHolder>(target).HeldItem;
            RememberItemOwner(entityManager, heldItem, player);
        }

        private static bool IsFloorMessCleaningInteractionTarget(EntityManager entityManager, Entity target)
        {
            if (IsFloorMessCleaningTarget(entityManager, target))
            {
                return true;
            }

            if (!entityManager.Exists(target))
            {
                return false;
            }

            if (entityManager.HasComponent<CItemHolder>(target) &&
                IsFloorMessCleaningTarget(entityManager, entityManager.GetComponentData<CItemHolder>(target).HeldItem))
            {
                return true;
            }

            if (entityManager.HasComponent<CDurationInteractionProxy>(target) &&
                IsFloorMessCleaningTarget(entityManager, entityManager.GetComponentData<CDurationInteractionProxy>(target).Proxy))
            {
                return true;
            }

            return false;
        }

        private static bool TryResolveDrinkDeliveryPlayer(EntityManager entityManager, CItemTransferProposal proposal, out Entity player)
        {
            if (ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, proposal.Item, out player))
            {
                return true;
            }

            if (ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, proposal.Source, out player))
            {
                return true;
            }

            return TryResolveRememberedItemOwner(entityManager, proposal.Item, out player);
        }

        private static bool IsCustomerDrinkDestination(EntityManager entityManager, Entity destination)
        {
            Entity tableSet;
            if (!ServiceStatsEntityHelpers.TryResolveTableSet(entityManager, destination, out tableSet))
            {
                return false;
            }

            if (!entityManager.Exists(tableSet) || !entityManager.HasComponent<COccupiedByGroup>(tableSet))
            {
                return false;
            }

            Entity group = entityManager.GetComponentData<COccupiedByGroup>(tableSet);
            if (!entityManager.Exists(group) || !entityManager.HasComponent<CWantsDrink>(group))
            {
                return false;
            }

            CWantsDrink wantsDrink = entityManager.GetComponentData<CWantsDrink>(group);
            return wantsDrink.TimeToNextDrink <= 0f;
        }

        public static void RecordMovementSample(EntityManager entityManager, Entity player, Vector3 position)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            ServiceStatsMovementSnapshot snapshot;
            if (MovementSnapshots.TryGetValue(state.PlayerId, out snapshot) && snapshot.HasPosition)
            {
                state.DistanceTravelled += ServiceStatsHudLogic.CalculateMovementDelta(
                    snapshot.LastPosition.x,
                    snapshot.LastPosition.z,
                    position.x,
                    position.z,
                    MaxMovementSampleDistance);
            }

            MovementSnapshots[state.PlayerId] = new ServiceStatsMovementSnapshot
            {
                HasPosition = true,
                LastPosition = position
            };
        }

        public static void RecordIdleSample(EntityManager entityManager, Entity player, float deltaSeconds)
        {
            ServiceStatsPlayerState state;
            if (!TryGetOrCreateState(entityManager, player, out state))
            {
                return;
            }

            state.IdleTime += ServiceStatsHudLogic.CalculateIdleDelta(state.SecondsSinceLastAction, deltaSeconds, ServiceStatsSettings.IdleThresholdSeconds);
            state.AsleepTime += ServiceStatsHudLogic.CalculateIdleDelta(state.SecondsSinceLastAction, deltaSeconds, ServiceStatsSettings.AsleepThresholdSeconds);
            if (deltaSeconds > 0f)
            {
                state.SecondsSinceLastAction += deltaSeconds;
            }
        }

        public static void RememberCleaningProcess(EntityManager entityManager, Entity itemEntity, CItemUndergoingProcess process)
        {
            int itemKey = GetProcessSnapshotKey(itemEntity);
            Entity resolvedPlayer;
            if (!entityManager.Exists(itemEntity) ||
                process.Actor == Entity.Null ||
                process.IsAutomatic ||
                !ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, process.Actor, out resolvedPlayer))
            {
                CleaningProcesses.Remove(itemKey);
                return;
            }

            NotePotentialPlayer(entityManager, resolvedPlayer);
            RememberItemOwner(entityManager, itemEntity, resolvedPlayer);
            bool isWashCleaningProcess = IsWashCleaningProcess(entityManager, itemEntity, process.Appliance);
            ServiceStatsProcessSnapshot existingSnapshot;
            bool actionRecorded = CleaningProcesses.TryGetValue(itemKey, out existingSnapshot) &&
                                  existingSnapshot.Actor == resolvedPlayer &&
                                  existingSnapshot.Appliance == process.Appliance &&
                                  existingSnapshot.Process == process.Process &&
                                  existingSnapshot.ActionRecorded;
            if (!actionRecorded)
            {
                RecordAction(entityManager, resolvedPlayer);
                actionRecorded = true;
            }

            CleaningProcesses[itemKey] = new ServiceStatsProcessSnapshot
            {
                Item = itemEntity,
                RawActor = process.Actor,
                Actor = resolvedPlayer,
                Appliance = process.Appliance,
                Process = process.Process,
                IsWashCleaningProcess = isWashCleaningProcess,
                ActionRecorded = actionRecorded
            };
        }

        public static int GetProcessSnapshotKey(Entity processEntity)
        {
            return ServiceStatsHudLogic.GetProcessSnapshotKey(processEntity.Index);
        }

        public static bool TryConsumeCompletedCleaningProcess(EntityManager entityManager, int itemKey, CCompletedProcess completion, out Entity actor, out bool isWashCleaningProcess)
        {
            bool actionAlreadyRecorded;
            return TryConsumeCompletedCleaningProcess(entityManager, itemKey, completion, out actor, out isWashCleaningProcess, out actionAlreadyRecorded);
        }

        public static bool TryConsumeCompletedCleaningProcess(EntityManager entityManager, int itemKey, CCompletedProcess completion, out Entity actor, out bool isWashCleaningProcess, out bool actionAlreadyRecorded)
        {
            actor = Entity.Null;
            isWashCleaningProcess = false;
            actionAlreadyRecorded = false;

            ServiceStatsProcessSnapshot snapshot;
            if (!CleaningProcesses.TryGetValue(itemKey, out snapshot))
            {
                return false;
            }

            CleaningProcesses.Remove(itemKey);

            if (completion.IsBad ||
                snapshot.Process != completion.Process ||
                !entityManager.Exists(snapshot.Item) ||
                !ServiceStatsEntityHelpers.IsValidPlayer(entityManager, snapshot.Actor))
            {
                return false;
            }

            actor = snapshot.Actor;
            isWashCleaningProcess = snapshot.IsWashCleaningProcess && IsWashCleaningProcess(entityManager, snapshot.Item, snapshot.Appliance);
            actionAlreadyRecorded = snapshot.ActionRecorded;
            return true;
        }

        public static bool IsWashCleaningProcess(EntityManager entityManager, Entity itemEntity, Entity appliance)
        {
            return ServiceStatsHudLogic.IsCompletedCleaningProcessWashEligible(
                IsDishCleaningAppliance(entityManager, appliance),
                IsFloorMessCleaningTarget(entityManager, itemEntity) || IsFloorMessCleaningTarget(entityManager, appliance));
        }

        public static bool IsDishCleaningAppliance(EntityManager entityManager, Entity appliance)
        {
            return entityManager.Exists(appliance) &&
                   (entityManager.HasComponent<CCleanAppliance>(appliance) ||
                    entityManager.HasComponent<CToolClean>(appliance));
        }

        public static bool IsFloorMessCleaningTarget(EntityManager entityManager, Entity target)
        {
            return entityManager.Exists(target) &&
                   (entityManager.HasComponent<CMess>(target) ||
                    entityManager.HasComponent<CStackableMess>(target));
        }

        public static void RemoveStaleCleaningProcesses(EntityManager entityManager)
        {
            if (CleaningProcesses.Count == 0)
            {
                return;
            }

            List<Entity> stale = new List<Entity>();
            List<int> staleKeys = new List<int>();
            foreach (KeyValuePair<int, ServiceStatsProcessSnapshot> pair in CleaningProcesses)
            {
                Entity item = pair.Value.Item;
                if (!entityManager.Exists(item) || !entityManager.HasComponent<CItemUndergoingProcess>(item))
                {
                    staleKeys.Add(pair.Key);
                    continue;
                }

                CItemUndergoingProcess process = entityManager.GetComponentData<CItemUndergoingProcess>(item);
                Entity resolvedActor;
                if (process.IsAutomatic ||
                    process.Process != pair.Value.Process ||
                    process.Actor == Entity.Null ||
                    !ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, process.Actor, out resolvedActor) ||
                    resolvedActor != pair.Value.Actor ||
                    process.Appliance != pair.Value.Appliance)
                {
                    staleKeys.Add(pair.Key);
                }
            }

            for (int index = 0; index < staleKeys.Count; index++)
            {
                CleaningProcesses.Remove(staleKeys[index]);
            }
        }

        public static ServiceStatsHudState BuildHudState()
        {
            ServiceStatsSettings.SyncFromPreferences();

            if (!ServiceStatsSettings.Enabled)
            {
                return ServiceStatsHudState.Empty;
            }

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(PlayerStats.Values, ServiceStatsSettings.HideZeroServePlayers);
            if (cards.Count == 0)
            {
                return ServiceStatsHudState.Empty;
            }

            return new ServiceStatsHudState(
                cards,
                ServiceStatsHudLogic.CalculateColumnCount(cards.Count, (int) ServiceStatsSettings.SplitThreshold),
                ServiceStatsSettings.ShowServed,
                ServiceStatsSettings.ShowOrders,
                ServiceStatsSettings.ShowWashed,
                ServiceStatsSettings.ShowActions,
                ServiceStatsSettings.ShowDistance,
                ServiceStatsSettings.ShowIdle,
                ServiceStatsSettings.ScaleMultiplier,
                ServiceStatsSettings.Font,
                ServiceStatsSettings.YOffsetScreenPercent);
        }

        private static bool TryGetOrCreateState(EntityManager entityManager, Entity player, out ServiceStatsPlayerState state)
        {
            state = null;

            Entity resolvedPlayer;
            if (!TryResolvePlayerEntity(entityManager, player, out resolvedPlayer))
            {
                return false;
            }

            int playerId;
            if (!ServiceStatsEntityHelpers.TryGetPlayerId(entityManager, resolvedPlayer, out playerId))
            {
                return false;
            }

            if (!PlayerStats.TryGetValue(playerId, out state))
            {
                state = new ServiceStatsPlayerState
                {
                    PlayerId = playerId,
                    BadgeColor = ServiceStatsDisplayResolver.ResolveColor(entityManager, resolvedPlayer, playerId)
                };

                PlayerStats.Add(playerId, state);
            }

            RefreshIdentity(entityManager, resolvedPlayer, state);
            return true;
        }

        private static bool TryResolvePlayerEntity(EntityManager entityManager, Entity entity, out Entity resolvedPlayer)
        {
            resolvedPlayer = Entity.Null;

            if (ServiceStatsEntityHelpers.IsValidPlayer(entityManager, entity))
            {
                resolvedPlayer = entity;
                return true;
            }

            if (ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, entity, out resolvedPlayer))
            {
                return true;
            }

            if (_resolutionFailureLogs < 8)
            {
                _resolutionFailureLogs++;
                Mod.LogWarning("Service stats could not resolve player for entity " +
                    ServiceStatsEntityHelpers.DescribeEntity(entity) +
                    " (has CPlayer=" + HasComponent<CPlayer>(entityManager, entity) +
                    ", has CPosition=" + HasComponent<CPosition>(entityManager, entity) +
                    ", has COwnedByPlayer=" + HasComponent<COwnedByPlayer>(entityManager, entity) +
                    ", has CHeldBy=" + HasComponent<CHeldBy>(entityManager, entity) +
                    ", has CItemHolder=" + HasComponent<CItemHolder>(entityManager, entity) + ")");
            }

            return false;
        }

        private static bool HasComponent<T>(EntityManager entityManager, Entity entity)
            where T : struct, IComponentData
        {
            return entity != Entity.Null && entityManager.Exists(entity) && entityManager.HasComponent<T>(entity);
        }

        private static void RefreshIdentity(EntityManager entityManager, Entity player, ServiceStatsPlayerState state)
        {
            state.BadgeColor = ServiceStatsDisplayResolver.ResolveColor(entityManager, player, state.PlayerId);

            string resolvedName = ServiceStatsDisplayResolver.ResolveName(state.PlayerId);
            if (!string.IsNullOrWhiteSpace(resolvedName))
            {
                state.ResolvedName = resolvedName;
            }
        }
    }
}
