using System.Collections.Generic;
using System.Text;

namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsHudLogic
    {
        public static string BuildFallbackLabel(int playerId)
        {
            return "P" + (playerId + 1);
        }

        public static string BuildDisplayLabel(string resolvedName, int playerId)
        {
            if (!string.IsNullOrWhiteSpace(resolvedName))
            {
                return resolvedName.Trim();
            }

            return BuildFallbackLabel(playerId);
        }

        public static int CalculateColumnCount(int visiblePlayerCount, int splitAfterVisiblePlayers)
        {
            int threshold = splitAfterVisiblePlayers < 1 ? 1 : splitAfterVisiblePlayers;
            return visiblePlayerCount > threshold ? 2 : 1;
        }

        public static int CountVisibleStats(bool showOrders, bool showWashed, bool showActions, bool showDistance)
        {
            return CountVisibleStats(showOrders, showWashed, showActions, showDistance, false);
        }

        public static int CountVisibleStats(bool showOrders, bool showWashed, bool showActions, bool showDistance, bool showIdle)
        {
            int count = 1;
            if (showOrders)
            {
                count++;
            }

            if (showWashed)
            {
                count++;
            }

            if (showActions)
            {
                count++;
            }

            if (showDistance)
            {
                count++;
            }

            if (showIdle)
            {
                count++;
            }

            return count;
        }

        public static int CalculateRowsForColumns(int itemCount, int columnCount)
        {
            if (itemCount <= 0)
            {
                return 0;
            }

            int safeColumnCount = columnCount < 1 ? 1 : columnCount;
            return (itemCount + safeColumnCount - 1) / safeColumnCount;
        }

        public static ServiceStatsPanelFit CalculatePanelFit(
            int visiblePlayerCount,
            int requestedColumnCount,
            float requestedScale,
            float cardWidth,
            float cardHeight,
            float spacingX,
            float spacingY,
            float availableWidth,
            float availableHeight,
            int maxColumnCount)
        {
            if (visiblePlayerCount <= 0)
            {
                return new ServiceStatsPanelFit(1, ClampScale(requestedScale));
            }

            int minimumColumns = ClampInt(requestedColumnCount, 1, visiblePlayerCount);
            int maximumColumns = ClampInt(maxColumnCount, minimumColumns, visiblePlayerCount);
            float targetScale = ClampScale(requestedScale);

            int bestColumns = minimumColumns;
            float bestScale = 0f;

            for (int columns = minimumColumns; columns <= maximumColumns; columns++)
            {
                int rows = (visiblePlayerCount + columns - 1) / columns;
                float panelWidth = (cardWidth * columns) + (spacingX * (columns - 1));
                float panelHeight = (cardHeight * rows) + (spacingY * (rows - 1));
                float widthScale = panelWidth <= 0f ? targetScale : availableWidth / panelWidth;
                float heightScale = panelHeight <= 0f ? targetScale : availableHeight / panelHeight;
                float candidateScale = ClampScale(Min(targetScale, Min(widthScale, heightScale)));

                if (candidateScale > bestScale + 0.001f)
                {
                    bestColumns = columns;
                    bestScale = candidateScale;
                }
            }

            if (bestScale <= 0f)
            {
                bestScale = 0.30f;
            }

            return new ServiceStatsPanelFit(bestColumns, bestScale);
        }

        public static List<ServiceStatsCardViewModel> BuildVisibleCards(IEnumerable<ServiceStatsPlayerState> players, bool hideZeroServePlayers)
        {
            List<ServiceStatsPlayerState> orderedPlayers = new List<ServiceStatsPlayerState>();
            foreach (ServiceStatsPlayerState player in players)
            {
                if (player == null)
                {
                    continue;
                }

                if (hideZeroServePlayers && player.Served <= 0)
                {
                    continue;
                }

                orderedPlayers.Add(player);
            }

            orderedPlayers.Sort(ComparePlayers);

            List<ServiceStatsCardViewModel> cards = new List<ServiceStatsCardViewModel>(orderedPlayers.Count);
            for (int index = 0; index < orderedPlayers.Count; index++)
            {
                ServiceStatsPlayerState player = orderedPlayers[index];
                cards.Add(new ServiceStatsCardViewModel
                {
                    PlayerId = player.PlayerId,
                    DisplayName = BuildDisplayLabel(player.ResolvedName, player.PlayerId),
                    BadgeColor = player.BadgeColor,
                    Served = player.Served,
                    OrdersTaken = player.OrdersTaken,
                    DishesWashed = player.DishesWashed,
                    ActionsPerformed = player.ActionsPerformed,
                    DistanceTravelled = player.DistanceTravelled,
                    IdleTime = player.IdleTime
                });
            }

            return cards;
        }

        public static float CalculateMovementDelta(float previousX, float previousZ, float currentX, float currentZ, float maxSampleDistance)
        {
            float x = currentX - previousX;
            float z = currentZ - previousZ;
            float distance = (float) System.Math.Sqrt((x * x) + (z * z));
            if (distance < 0.001f || distance > maxSampleDistance)
            {
                return 0f;
            }

            return distance;
        }

        public static string FormatDistance(float distance)
        {
            if (distance < 0f)
            {
                distance = 0f;
            }

            if (distance < 100f)
            {
                return distance.ToString("0.0");
            }

            return distance.ToString("0");
        }

        public static string BuildDebugText(IList<ServiceStatsCardViewModel> cards, bool showOrders, bool showWashed, bool showActions, bool showDistance)
        {
            return BuildDebugText(cards, showOrders, showWashed, showActions, showDistance, false);
        }

        public static string BuildDebugText(IList<ServiceStatsCardViewModel> cards, bool showOrders, bool showWashed, bool showActions, bool showDistance, bool showIdle)
        {
            if (cards == null || cards.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SERVICE STATS");

            for (int index = 0; index < cards.Count; index++)
            {
                ServiceStatsCardViewModel card = cards[index];
                if (card == null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(card.DisplayName) ? BuildFallbackLabel(card.PlayerId) : card.DisplayName.Trim();
                builder.Append(name);
                builder.Append("  srv ").Append(card.Served);

                if (showOrders)
                {
                    builder.Append("  ord ").Append(card.OrdersTaken);
                }

                if (showWashed)
                {
                    builder.Append("  wash ").Append(card.DishesWashed);
                }

                if (showActions)
                {
                    builder.Append("  act ").Append(card.ActionsPerformed);
                }

                if (showDistance)
                {
                    builder.Append("  dist ").Append(FormatDistance(card.DistanceTravelled));
                }

                if (showIdle)
                {
                    builder.Append("  idle ").Append(FormatDuration(card.IdleTime));
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        public static string FormatDuration(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int roundedSeconds = (int) System.Math.Floor(seconds + 0.5f);
            int minutes = roundedSeconds / 60;
            int remainingSeconds = roundedSeconds % 60;
            if (minutes <= 0)
            {
                return remainingSeconds + "s";
            }

            return minutes + "m" + remainingSeconds.ToString("00") + "s";
        }

        public static float CalculateIdleDelta(float previousSecondsSinceAction, float deltaSeconds, float idleThresholdSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return 0f;
            }

            if (previousSecondsSinceAction < 0f)
            {
                previousSecondsSinceAction = 0f;
            }

            if (idleThresholdSeconds < 0f)
            {
                idleThresholdSeconds = 0f;
            }

            float nextSecondsSinceAction = previousSecondsSinceAction + deltaSeconds;
            float previousIdleSeconds = previousSecondsSinceAction > idleThresholdSeconds ? previousSecondsSinceAction - idleThresholdSeconds : 0f;
            float nextIdleSeconds = nextSecondsSinceAction > idleThresholdSeconds ? nextSecondsSinceAction - idleThresholdSeconds : 0f;
            return nextIdleSeconds - previousIdleSeconds;
        }

        public static float CalculateAnchoredYOffset(float screenPercent, float referenceHeight, float currentMargin)
        {
            if (screenPercent <= 0f || referenceHeight <= 0f)
            {
                return -currentMargin;
            }

            if (screenPercent > 0.90f)
            {
                screenPercent = 0.90f;
            }

            return -(referenceHeight * screenPercent);
        }

        public static ServiceStatsCleaningCredit GetCompletedCleaningCredit(bool isCleaningAppliance)
        {
            return GetCompletedCleaningCredit(isCleaningAppliance, false);
        }

        public static ServiceStatsCleaningCredit GetCompletedCleaningCredit(bool isCleaningAppliance, bool actionAlreadyRecorded)
        {
            return new ServiceStatsCleaningCredit(!actionAlreadyRecorded, isCleaningAppliance);
        }

        public static bool ShouldRecordResolvedServe(bool transferAccepted, bool hasPlayerActor)
        {
            return transferAccepted && hasPlayerActor;
        }

        public static ServiceStatsServeCredit GetServeCredit(bool served)
        {
            return new ServiceStatsServeCredit(served, served);
        }

        public static int GetProcessSnapshotKey(int processEntityIndex)
        {
            return processEntityIndex;
        }

        public static bool ShouldRecordDurationAction(bool isActive, bool isManual, bool hasPlayerActor, bool alreadyActive)
        {
            return isActive && isManual && hasPlayerActor && !alreadyActive;
        }

        public static bool ShouldRecordInteractionAttempt(int interactionType, int interactionResult)
        {
            const int grab = 1;
            const int act = 2;
            const int performed = 2;
            return interactionResult == performed && (interactionType == grab || interactionType == act);
        }

        public static bool DoesVerticalContentFit(float cardHeight, float paddingY, float headerHeight, float contentSpacing, float firstSectionHeight, float secondSectionHeight)
        {
            float availableHeight = cardHeight - (paddingY * 2f);
            float requiredHeight = headerHeight + contentSpacing + firstSectionHeight + contentSpacing + secondSectionHeight;
            return availableHeight + 0.001f >= requiredHeight;
        }

        private static int ComparePlayers(ServiceStatsPlayerState left, ServiceStatsPlayerState right)
        {
            int servedCompare = right.Served.CompareTo(left.Served);
            if (servedCompare != 0)
            {
                return servedCompare;
            }

            int orderCompare = right.OrdersTaken.CompareTo(left.OrdersTaken);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            int washedCompare = right.DishesWashed.CompareTo(left.DishesWashed);
            if (washedCompare != 0)
            {
                return washedCompare;
            }

            return left.PlayerId.CompareTo(right.PlayerId);
        }

        private static float ClampScale(float value)
        {
            if (value < 0.30f)
            {
                return 0.30f;
            }

            if (value > 1.30f)
            {
                return 1.30f;
            }

            return value;
        }

        private static int ClampInt(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private static float Min(float left, float right)
        {
            return left < right ? left : right;
        }
    }

    public struct ServiceStatsPanelFit
    {
        public ServiceStatsPanelFit(int columnCount, float scaleMultiplier)
        {
            ColumnCount = columnCount;
            ScaleMultiplier = scaleMultiplier;
        }

        public int ColumnCount;
        public float ScaleMultiplier;
    }

    public struct ServiceStatsCleaningCredit
    {
        public ServiceStatsCleaningCredit(bool recordAction, bool recordWashed)
        {
            RecordAction = recordAction;
            RecordWashed = recordWashed;
        }

        public bool RecordAction;
        public bool RecordWashed;
    }

    public struct ServiceStatsServeCredit
    {
        public ServiceStatsServeCredit(bool recordAction, bool recordServed)
        {
            RecordAction = recordAction;
            RecordServed = recordServed;
        }

        public bool RecordAction;
        public bool RecordServed;
    }
}
