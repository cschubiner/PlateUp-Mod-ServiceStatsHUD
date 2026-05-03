using System.Collections.Generic;

namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsActionHookRules
    {
        private static readonly HashSet<string> TrackedAssemblies = new HashSet<string>
        {
            "Kitchen.Common",
            "KitchenMode"
        };

        private static readonly HashSet<string> ExcludedInteractionTypeNames = new HashSet<string>
        {
            "GroupPromptForOrder",
            "UseOrderMachine",
            "TransferInteractionProposalSystem",
            "HighlightInteraction",
            "PseudoInteractProcess",
            "ShowPingedApplianceInfo",
            "ShowPingedBlueprintDeskInfo",
            "ShowPingedCabinetInfo",
            "PseudoRenameNameplate",
            "HandleDumbWaiters"
        };

        private static readonly HashSet<string> ExcludedTransferTypeNames = new HashSet<string>
        {
            "AcceptIntoOrderSatisfaction",
            "AcceptIntoPiecemealSatisfaction",
            "AcceptIntoExtraSatisfaction"
        };

        public static bool ShouldScanAssembly(string assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName) && TrackedAssemblies.Contains(assemblyName);
        }

        public static bool ShouldTrackGenericInteractionType(string typeName)
        {
            return !string.IsNullOrWhiteSpace(typeName) && !ExcludedInteractionTypeNames.Contains(typeName);
        }

        public static bool ShouldTrackGenericTransferType(string typeName)
        {
            return !string.IsNullOrWhiteSpace(typeName) && !ExcludedTransferTypeNames.Contains(typeName);
        }
    }
}
