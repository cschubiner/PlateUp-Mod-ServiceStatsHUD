using HarmonyLib;
using Kitchen;
using KitchenServiceStatsHUD.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace KitchenServiceStatsHUD.Patches
{
    internal static class ServeTrackingHelpers
    {
        private static readonly FieldInfo GroupPromptForOrderGroupField = AccessTools.Field(typeof(GroupPromptForOrder), "Group");
        private static readonly FieldInfo UseOrderMachineOrderTargetsField = AccessTools.Field(typeof(UseOrderMachine), "OrderTargets");
        private static readonly FieldInfo UseOrderMachineOrderOffsetField = AccessTools.Field(typeof(UseOrderMachine), "OrderOffset");
        private static readonly MethodInfo UseOrderMachineGetNextReorderGroupMethod = AccessTools.Method(typeof(UseOrderMachine), "GetNextReorderGroup");

        public static void RecordConfirmedServe(EntityManager entityManager, ServiceStatsServeAcceptanceState state, bool accepted)
        {
            if (!accepted ||
                !state.ShouldRecord ||
                !ServiceStatsRuntime.TryRecordDishServedForTransferOnce(entityManager, state.Transfer, state.Acceptance, state.Player))
            {
                return;
            }

            ServiceStatsRuntime.TryRecordActionForTransferOnce(entityManager, state.Transfer, state.Player);
        }

        public static void RecordActionFromTransfer(EntityManager entityManager, Entity transferEntity, Entity acceptanceEntity)
        {
            Entity player;
            if (!ServiceStatsEntityHelpers.TryResolvePlayerFromTransfer(entityManager, transferEntity, acceptanceEntity, out player))
            {
                return;
            }

            if (entityManager.Exists(transferEntity) && entityManager.HasComponent<CItemTransferProposal>(transferEntity))
            {
                CItemTransferProposal proposal = entityManager.GetComponentData<CItemTransferProposal>(transferEntity);
                ServiceStatsRuntime.RememberItemOwner(entityManager, proposal.Item, player);
            }

            ServiceStatsRuntime.TryRecordActionForTransferOnce(entityManager, transferEntity, player);
        }

        public static void RecordActionFromSuccessfulInteractionTransfer(EntityManager entityManager, Entity result, Entity transfer, Entity acceptance)
        {
            if (!IsAcceptedTransferResult(entityManager, result))
            {
                return;
            }

            RecordActionFromTransfer(entityManager, transfer, acceptance);
        }

        public static ServiceStatsTransferActionState PrepareActionFromSuccessfulInteractionTransfer(EntityManager entityManager, Entity result, Entity transfer, Entity acceptance)
        {
            ServiceStatsTransferActionState state = default(ServiceStatsTransferActionState);
            if (!IsAcceptedTransferResult(entityManager, result))
            {
                return state;
            }

            Entity player;
            if (!ServiceStatsEntityHelpers.TryResolvePlayerFromTransfer(entityManager, transfer, acceptance, out player))
            {
                return state;
            }

            if (entityManager.Exists(transfer) && entityManager.HasComponent<CItemTransferProposal>(transfer))
            {
                CItemTransferProposal proposal = entityManager.GetComponentData<CItemTransferProposal>(transfer);
                ServiceStatsRuntime.RememberItemOwner(entityManager, proposal.Item, player);
            }

            state.ShouldRecord = true;
            state.Transfer = transfer;
            state.Player = player;
            return state;
        }

        public static void RecordPreparedTransferAction(EntityManager entityManager, ServiceStatsTransferActionState state)
        {
            if (!state.ShouldRecord)
            {
                return;
            }

            ServiceStatsRuntime.TryRecordActionForTransferOnce(entityManager, state.Transfer, state.Player);
        }

        public static bool IsAcceptedTransferResult(EntityManager entityManager, Entity result)
        {
            return entityManager.Exists(result) &&
                   entityManager.HasComponent<CItemTransferAccept>(result) &&
                   entityManager.GetComponentData<CItemTransferAccept>(result).Status == ItemAcceptStatus.Accepted;
        }

        public static ServiceStatsOrderActionState PreparePromptForOrder(EntityManager entityManager, GroupPromptForOrder instance, Entity interactor)
        {
            ServiceStatsOrderActionState state = default(ServiceStatsOrderActionState);
            Entity player;
            if (!ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, interactor, out player))
            {
                return state;
            }

            object groupValue = GroupPromptForOrderGroupField?.GetValue(instance);
            if (!(groupValue is COccupiedByGroup occupiedGroup))
            {
                return state;
            }

            Entity group = occupiedGroup;
            if (!entityManager.Exists(group) || entityManager.HasComponent<CGroupPromptedForOrder>(group))
            {
                return state;
            }

            state.ShouldRecord = true;
            state.Player = player;
            return state;
        }

        public static ServiceStatsOrderActionState PrepareOrderMachineUse(EntityManager entityManager, UseOrderMachine instance, Entity interactor, Entity target)
        {
            ServiceStatsOrderActionState state = default(ServiceStatsOrderActionState);
            Entity player;
            if (!ServiceStatsEntityHelpers.TryResolvePlayerFromSource(entityManager, interactor, out player) ||
                !entityManager.Exists(target) ||
                !entityManager.HasComponent<CApplianceOrderMachine>(target))
            {
                return state;
            }

            CApplianceOrderMachine orderMachine = entityManager.GetComponentData<CApplianceOrderMachine>(target);
            bool shouldRecord = orderMachine.IsReorderMachine
                ? HasReorderTarget(instance)
                : HasOrderTarget(instance);
            if (!shouldRecord)
            {
                return state;
            }

            state.ShouldRecord = true;
            state.Player = player;
            return state;
        }

        public static void RecordConfirmedOrder(EntityManager entityManager, ServiceStatsOrderActionState state)
        {
            if (!state.ShouldRecord)
            {
                return;
            }

            ServiceStatsRuntime.RecordOrderTaken(entityManager, state.Player);
            ServiceStatsRuntime.RecordAction(entityManager, state.Player);
        }

        private static bool HasOrderTarget(UseOrderMachine instance)
        {
            if (UseOrderMachineOrderTargetsField == null || UseOrderMachineOrderOffsetField == null)
            {
                return false;
            }

            NativeArray<Entity> orderTargets = (NativeArray<Entity>) UseOrderMachineOrderTargetsField.GetValue(instance);
            int orderOffset = (int) UseOrderMachineOrderOffsetField.GetValue(instance);
            return orderTargets.IsCreated && orderOffset >= 0 && orderTargets.Length > orderOffset;
        }

        private static bool HasReorderTarget(UseOrderMachine instance)
        {
            if (UseOrderMachineGetNextReorderGroupMethod == null)
            {
                return false;
            }

            Entity group = (Entity) UseOrderMachineGetNextReorderGroupMethod.Invoke(instance, new object[0]);
            return group != Entity.Null;
        }
    }

    [HarmonyPatch(typeof(GroupPromptForOrder), "Perform")]
    internal static class GroupPromptForOrderPerformPatch
    {
        private static void Prefix(GroupPromptForOrder __instance, ref InteractionData __0, ref ServiceStatsOrderActionState __state)
        {
            __state = ServeTrackingHelpers.PreparePromptForOrder(__instance.EntityManager, __instance, __0.Interactor);
        }

        private static void Postfix(GroupPromptForOrder __instance, ref ServiceStatsOrderActionState __state)
        {
            ServeTrackingHelpers.RecordConfirmedOrder(__instance.EntityManager, __state);
        }
    }

    [HarmonyPatch(typeof(UseOrderMachine), "Perform")]
    internal static class UseOrderMachinePerformPatch
    {
        private static void Prefix(UseOrderMachine __instance, ref InteractionData __0, ref ServiceStatsOrderActionState __state)
        {
            __state = ServeTrackingHelpers.PrepareOrderMachineUse(__instance.EntityManager, __instance, __0.Interactor, __0.Target);
        }

        private static void Postfix(UseOrderMachine __instance, ref ServiceStatsOrderActionState __state)
        {
            ServeTrackingHelpers.RecordConfirmedOrder(__instance.EntityManager, __state);
        }
    }

    [HarmonyPatch(typeof(AcceptIntoOrderSatisfaction), "AcceptTransfer")]
    internal static class AcceptIntoOrderSatisfactionPatch
    {
        private static void Prefix(AcceptIntoOrderSatisfaction __instance, Entity __0, Entity __1, ref ServiceStatsServeAcceptanceState __state)
        {
            ServiceStatsEntityHelpers.TryPrepareOrderServeAcceptance(__instance.EntityManager, __0, __1, false, out __state);
        }

        private static void Postfix(AcceptIntoOrderSatisfaction __instance, ref ServiceStatsServeAcceptanceState __state)
        {
            ServeTrackingHelpers.RecordConfirmedServe(
                __instance.EntityManager,
                __state,
                ServiceStatsEntityHelpers.DidOrderServeAcceptanceComplete(__instance.EntityManager, __state));
        }
    }

    [HarmonyPatch(typeof(AcceptIntoPiecemealSatisfaction), "AcceptTransfer")]
    internal static class AcceptIntoPiecemealSatisfactionPatch
    {
        private static void Prefix(AcceptIntoPiecemealSatisfaction __instance, Entity __0, Entity __1, ref ServiceStatsServeAcceptanceState __state)
        {
            ServiceStatsEntityHelpers.TryPreparePartialServeAcceptance(__instance.EntityManager, __0, __1, out __state);
        }

        private static void Postfix(AcceptIntoPiecemealSatisfaction __instance, ref ServiceStatsServeAcceptanceState __state)
        {
            // Piecemeal transfers can be probed before the resolved acceptance is finalized.
            // The ECS acceptance tracker records confirmed partial serves without proximity false positives.
            ServeTrackingHelpers.RecordConfirmedServe(__instance.EntityManager, __state, false);
        }
    }

    [HarmonyPatch(typeof(AcceptIntoExtraSatisfaction), "AcceptTransfer")]
    internal static class AcceptIntoExtraSatisfactionPatch
    {
        private static void Prefix(AcceptIntoExtraSatisfaction __instance, Entity __0, Entity __1, ref ServiceStatsServeAcceptanceState __state)
        {
            ServiceStatsEntityHelpers.TryPrepareOrderServeAcceptance(__instance.EntityManager, __0, __1, true, out __state);
        }

        private static void Postfix(AcceptIntoExtraSatisfaction __instance, ref ServiceStatsServeAcceptanceState __state)
        {
            ServeTrackingHelpers.RecordConfirmedServe(
                __instance.EntityManager,
                __state,
                ServiceStatsEntityHelpers.DidOrderServeAcceptanceComplete(__instance.EntityManager, __state));
        }
    }

    [HarmonyPatch(typeof(TakeForCustomerOrders), "ReceiveResult")]
    internal static class TakeForCustomerOrdersReceiveResultPatch
    {
        private static void Prefix(TakeForCustomerOrders __instance, Entity __0, Entity __1, Entity __2, ref ServiceStatsServeAcceptanceState __state)
        {
            if (!ServeTrackingHelpers.IsAcceptedTransferResult(__instance.EntityManager, __0))
            {
                return;
            }

            if (ServiceStatsEntityHelpers.TryPrepareOrderServeAcceptance(__instance.EntityManager, __1, __2, false, out __state))
            {
                return;
            }

            ServiceStatsEntityHelpers.TryPreparePartialServeAcceptance(__instance.EntityManager, __1, __2, out __state);
        }

        private static void Postfix(TakeForCustomerOrders __instance, ref ServiceStatsServeAcceptanceState __state)
        {
            ServeTrackingHelpers.RecordConfirmedServe(
                __instance.EntityManager,
                __state,
                __state.ShouldRecord &&
                ServiceStatsEntityHelpers.DidOrderServeAcceptanceComplete(__instance.EntityManager, __state));
        }
    }

    [HarmonyPatch]
    internal static class TransferInteractionReceiveResultActionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type transferInteractionType = typeof(TransferInteractionProposalSystem);
            Type[] signature =
            {
                typeof(Entity),
                typeof(Entity),
                typeof(Entity),
                typeof(EntityContext)
            };

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => ServiceStatsActionHookRules.ShouldScanAssembly(assembly.GetName().Name))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => transferInteractionType.IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               ServiceStatsActionHookRules.ShouldTrackTransferInteractionResultType(type.Name))
                .Select(type => type.GetMethod(
                    "ReceiveResult",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null,
                    signature,
                    null))
                .Where(method => method != null)
                .Cast<MethodBase>();
        }

        private static void Prefix(object __instance, Entity __0, Entity __1, Entity __2, ref ServiceStatsTransferActionState __state)
        {
            if (!(__instance is GameSystemBase systemBase))
            {
                return;
            }

            __state = ServeTrackingHelpers.PrepareActionFromSuccessfulInteractionTransfer(systemBase.EntityManager, __0, __1, __2);
        }

        private static void Postfix(object __instance, ref ServiceStatsTransferActionState __state)
        {
            if (!(__instance is GameSystemBase systemBase))
            {
                return;
            }

            ServeTrackingHelpers.RecordPreparedTransferAction(systemBase.EntityManager, __state);
        }
    }

    [HarmonyPatch]
    internal static class GenericTransferActionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type transferInterface = typeof(IAcceptTransfers);
            Type[] signature = { typeof(Entity), typeof(Entity), typeof(EntityContext), typeof(Entity).MakeByRefType() };

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => ServiceStatsActionHookRules.ShouldScanAssembly(assembly.GetName().Name))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => transferInterface.IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               ServiceStatsActionHookRules.ShouldTrackGenericTransferType(type.Name))
                .Select(type => type.GetMethod(
                    "AcceptTransfer",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    signature,
                    null))
                .Where(method => method != null)
                .Cast<MethodBase>();
        }

        private static void Postfix(object __instance, Entity __0, Entity __1)
        {
            if (!(__instance is GameSystemBase systemBase))
            {
                return;
            }

            ServeTrackingHelpers.RecordActionFromTransfer(systemBase.EntityManager, __0, __1);
        }
    }

    [HarmonyPatch]
    internal static class GenericPlayerActionInteractionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type interactionSystemType = typeof(InteractionSystem);
            Type interactionDataRefType = typeof(InteractionData).MakeByRefType();
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => ServiceStatsActionHookRules.ShouldScanAssembly(assembly.GetName().Name))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => interactionSystemType.IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               ServiceStatsActionHookRules.ShouldTrackGenericInteractionType(type.Name))
                .Select(type => type.GetMethod(
                    "Perform",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { interactionDataRefType },
                    null))
                .Where(method => method != null)
                .Cast<MethodBase>();
        }

        private static void Postfix(object __instance, ref InteractionData __0)
        {
            if (__0.Interactor == Entity.Null || !(__instance is InteractionSystem interactionSystem))
            {
                return;
            }

            ServiceStatsRuntime.RecordAction(interactionSystem.EntityManager, __0.Interactor);
        }
    }
}
