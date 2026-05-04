using KitchenServiceStatsHUD.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KitchenServiceStatsHUD.Tests
{
    [TestClass]
    public class ServiceStatsHudLogicTests
    {
        [TestInitialize]
        public void ResetSettingsBeforeTest()
        {
            SetPrivateStaticField("_manager", null);
            SetPrivateStaticField("_preferenceReadWarningLogged", false);
            SetPrivateStaticProperty("Enabled", true);
            SetPrivateStaticProperty("ShowServed", true);
            SetPrivateStaticProperty("ShowOrders", true);
            SetPrivateStaticProperty("ShowWashed", false);
            SetPrivateStaticProperty("ShowActions", false);
            SetPrivateStaticProperty("ShowDistance", true);
            SetPrivateStaticProperty("ShowIdle", true);
            SetPrivateStaticProperty("IdleThresholdSeconds", 1);
            SetPrivateStaticProperty("AsleepThresholdSeconds", 5);
            SetPrivateStaticProperty("HideZeroServePlayers", true);
            SetPrivateStaticProperty("Scale", ServiceStatsScaleOption.Normal);
            SetPrivateStaticProperty("Font", ServiceStatsFontOption.Alternate1);
            SetPrivateStaticProperty("YOffset", ServiceStatsYOffsetOption.Current);
            SetPrivateStaticProperty("SplitThreshold", ServiceStatsSplitThresholdOption.Six);
        }

        [TestMethod]
        public void HiddenUntilFirstServe_ButPreservesTrackedTotals()
        {
            ServiceStatsPlayerState player = new ServiceStatsPlayerState
            {
                PlayerId = 2,
                ResolvedName = "Alex",
                BadgeColor = Color.cyan,
                Served = 0,
                OrdersTaken = 4,
                DishesWashed = 3,
                ActionsPerformed = 9,
                DistanceTravelled = 12.5f,
                IdleTime = 6.5f,
                AsleepTime = 2.5f
            };

            List<ServiceStatsCardViewModel> hiddenCards = ServiceStatsHudLogic.BuildVisibleCards(new[] { player }, true);
            Assert.AreEqual(0, hiddenCards.Count);

            player.Served = 1;

            List<ServiceStatsCardViewModel> visibleCards = ServiceStatsHudLogic.BuildVisibleCards(new[] { player }, true);
            Assert.AreEqual(1, visibleCards.Count);
            Assert.AreEqual(4, visibleCards[0].OrdersTaken);
            Assert.AreEqual(3, visibleCards[0].DishesWashed);
            Assert.AreEqual(9, visibleCards[0].ActionsPerformed);
            Assert.AreEqual(12.5f, visibleCards[0].DistanceTravelled, 0.001f);
            Assert.AreEqual(6.5f, visibleCards[0].IdleTime, 0.001f);
            Assert.AreEqual(2.5f, visibleCards[0].AsleepTime, 0.001f);
        }

        [TestMethod]
        public void ResetDailyTotals_ZeroesCountsWithoutDroppingIdentity()
        {
            ServiceStatsPlayerState player = new ServiceStatsPlayerState
            {
                PlayerId = 1,
                ResolvedName = "Morgan",
                BadgeColor = Color.magenta,
                Served = 5,
                OrdersTaken = 7,
                DishesWashed = 9,
                ActionsPerformed = 11,
                DistanceTravelled = 33.3f,
                IdleTime = 18f,
                AsleepTime = 14f,
                SecondsSinceLastAction = 23f
            };

            player.ResetDailyTotals();

            Assert.AreEqual(0, player.Served);
            Assert.AreEqual(0, player.OrdersTaken);
            Assert.AreEqual(0, player.DishesWashed);
            Assert.AreEqual(0, player.ActionsPerformed);
            Assert.AreEqual(0f, player.DistanceTravelled, 0.001f);
            Assert.AreEqual(0f, player.IdleTime, 0.001f);
            Assert.AreEqual(0f, player.AsleepTime, 0.001f);
            Assert.AreEqual(0f, player.SecondsSinceLastAction, 0.001f);
            Assert.AreEqual("Morgan", player.ResolvedName);
            Assert.AreEqual(Color.magenta, player.BadgeColor);
        }

        [TestMethod]
        public void BuildVisibleCards_SortsByDisplayNameFirst()
        {
            List<ServiceStatsPlayerState> players = new List<ServiceStatsPlayerState>
            {
                new ServiceStatsPlayerState { PlayerId = 3, ResolvedName = "Zoe", BadgeColor = Color.white, Served = 99, OrdersTaken = 4, DishesWashed = 1 },
                new ServiceStatsPlayerState { PlayerId = 1, ResolvedName = "alex", BadgeColor = Color.white, Served = 1, OrdersTaken = 0, DishesWashed = 0 },
                new ServiceStatsPlayerState { PlayerId = 2, ResolvedName = "Mina", BadgeColor = Color.white, Served = 2, OrdersTaken = 40, DishesWashed = 5 },
                new ServiceStatsPlayerState { PlayerId = 0, ResolvedName = "Janet", BadgeColor = Color.white, Served = 8, OrdersTaken = 4, DishesWashed = 5 }
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(players, true);

            Assert.AreEqual(4, cards.Count);
            Assert.AreEqual(1, cards[0].PlayerId);
            Assert.AreEqual(0, cards[1].PlayerId);
            Assert.AreEqual(2, cards[2].PlayerId);
            Assert.AreEqual(3, cards[3].PlayerId);
        }

        [TestMethod]
        public void BuildVisibleCards_TiesDisplayNameByDisplayIndexThenPlayerId()
        {
            List<ServiceStatsPlayerState> players = new List<ServiceStatsPlayerState>
            {
                new ServiceStatsPlayerState { PlayerId = 99, DisplayIndex = 2, ResolvedName = "", BadgeColor = Color.white, Served = 1 },
                new ServiceStatsPlayerState { PlayerId = -12839712, DisplayIndex = 1, ResolvedName = "", BadgeColor = Color.white, Served = 1 },
                new ServiceStatsPlayerState { PlayerId = 3, DisplayIndex = 1, ResolvedName = "", BadgeColor = Color.white, Served = 1 }
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(players, true);

            Assert.AreEqual(3, cards.Count);
            Assert.AreEqual(-12839712, cards[0].PlayerId);
            Assert.AreEqual(3, cards[1].PlayerId);
            Assert.AreEqual(99, cards[2].PlayerId);
        }

        [TestMethod]
        public void CalculateColumnCount_UsesSecondColumnOnlyAboveSixVisiblePlayers()
        {
            Assert.AreEqual(1, ServiceStatsHudLogic.CalculateColumnCount(0, 6));
            Assert.AreEqual(1, ServiceStatsHudLogic.CalculateColumnCount(6, 6));
            Assert.AreEqual(2, ServiceStatsHudLogic.CalculateColumnCount(7, 6));
            Assert.AreEqual(2, ServiceStatsHudLogic.CalculateColumnCount(12, 6));
        }

        [TestMethod]
        public void BuildVisibleCards_CanShowZeroServePlayersWhenSettingDisabled()
        {
            ServiceStatsPlayerState player = new ServiceStatsPlayerState
            {
                PlayerId = 4,
                ResolvedName = "Sam",
                BadgeColor = Color.green,
                Served = 0,
                OrdersTaken = 2,
                DishesWashed = 1,
                ActionsPerformed = 6
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(new[] { player }, false);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(4, cards[0].PlayerId);
            Assert.AreEqual(6, cards[0].ActionsPerformed);
        }

        [TestMethod]
        public void CalculateColumnCount_UsesConfiguredSplitThreshold()
        {
            Assert.AreEqual(1, ServiceStatsHudLogic.CalculateColumnCount(4, 4));
            Assert.AreEqual(2, ServiceStatsHudLogic.CalculateColumnCount(5, 4));
            Assert.AreEqual(1, ServiceStatsHudLogic.CalculateColumnCount(8, 8));
            Assert.AreEqual(2, ServiceStatsHudLogic.CalculateColumnCount(9, 8));
        }

        [TestMethod]
        public void CountVisibleStats_UsesEnabledStatsIncludingOptionalServed()
        {
            Assert.AreEqual(1, ServiceStatsHudLogic.CountVisibleStats(false, false, false, false));
            Assert.AreEqual(3, ServiceStatsHudLogic.CountVisibleStats(true, false, true, false));
            Assert.AreEqual(5, ServiceStatsHudLogic.CountVisibleStats(true, true, true, true));
            Assert.AreEqual(7, ServiceStatsHudLogic.CountVisibleStats(true, true, true, true, true));
            Assert.AreEqual(0, ServiceStatsHudLogic.CountVisibleStats(false, false, false, false, false, false));
            Assert.AreEqual(6, ServiceStatsHudLogic.CountVisibleStats(false, true, true, true, true, true));
        }

        [TestMethod]
        public void CalculateRowsForColumns_UsesSafeColumnCount()
        {
            Assert.AreEqual(0, ServiceStatsHudLogic.CalculateRowsForColumns(0, 2));
            Assert.AreEqual(5, ServiceStatsHudLogic.CalculateRowsForColumns(5, 0));
            Assert.AreEqual(3, ServiceStatsHudLogic.CalculateRowsForColumns(5, 2));
        }

        [TestMethod]
        public void CalculatePanelFit_KeepsRequestedColumnsWhenTheyAlreadyFit()
        {
            ServiceStatsPanelFit fit = ServiceStatsHudLogic.CalculatePanelFit(
                visiblePlayerCount: 2,
                requestedColumnCount: 1,
                requestedScale: 1f,
                cardWidth: 100f,
                cardHeight: 50f,
                spacingX: 10f,
                spacingY: 10f,
                availableWidth: 500f,
                availableHeight: 500f,
                maxColumnCount: 4);

            Assert.AreEqual(1, fit.ColumnCount);
            Assert.AreEqual(1f, fit.ScaleMultiplier, 0.001f);
        }

        [TestMethod]
        public void CalculatePanelFit_AddsColumnsToKeepCrowdedHudReadable()
        {
            ServiceStatsPanelFit fit = ServiceStatsHudLogic.CalculatePanelFit(
                visiblePlayerCount: 10,
                requestedColumnCount: 1,
                requestedScale: 1f,
                cardWidth: 100f,
                cardHeight: 100f,
                spacingX: 10f,
                spacingY: 10f,
                availableWidth: 450f,
                availableHeight: 350f,
                maxColumnCount: 4);

            Assert.AreEqual(4, fit.ColumnCount);
            Assert.AreEqual(1f, fit.ScaleMultiplier, 0.001f);
        }

        [TestMethod]
        public void CalculatePanelFit_NeverShrinksBelowThirtyPercent()
        {
            ServiceStatsPanelFit fit = ServiceStatsHudLogic.CalculatePanelFit(
                visiblePlayerCount: 18,
                requestedColumnCount: 1,
                requestedScale: 1f,
                cardWidth: 400f,
                cardHeight: 240f,
                spacingX: 20f,
                spacingY: 20f,
                availableWidth: 260f,
                availableHeight: 180f,
                maxColumnCount: 1);

            Assert.AreEqual(1, fit.ColumnCount);
            Assert.AreEqual(0.30f, fit.ScaleMultiplier, 0.001f);
        }

        [TestMethod]
        public void BuildDisplayLabel_FallsBackToPlayerNumberWhenNameMissing()
        {
            Assert.AreEqual("P3", ServiceStatsHudLogic.BuildDisplayLabel(null, 2));
            Assert.AreEqual("P5", ServiceStatsHudLogic.BuildDisplayLabel("   ", 4));
            Assert.AreEqual("Jordan", ServiceStatsHudLogic.BuildDisplayLabel("Jordan", 0));
        }

        [TestMethod]
        public void BuildDisplayLabel_NeverShowsRawInvalidPlayerIds()
        {
            Assert.AreEqual("P?", ServiceStatsHudLogic.BuildDisplayLabel(null, -12839712));
            Assert.AreEqual("P?", ServiceStatsHudLogic.BuildDisplayLabel("   ", 999999));
        }

        [TestMethod]
        public void BuildVisibleCards_UsesDisplayIndexForFallbackLabels()
        {
            ServiceStatsPlayerState player = new ServiceStatsPlayerState
            {
                PlayerId = -12839712,
                DisplayIndex = 1,
                BadgeColor = Color.white,
                Served = 1
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(new[] { player }, true);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(-12839712, cards[0].PlayerId);
            Assert.AreEqual(1, cards[0].DisplayIndex);
            Assert.AreEqual("P2", cards[0].DisplayName);
        }

        [TestMethod]
        public void CalculateMovementDelta_UsesPlanarDistanceAndIgnoresTeleports()
        {
            Assert.AreEqual(5f, ServiceStatsHudLogic.CalculateMovementDelta(0f, 0f, 3f, 4f, 6f), 0.001f);
            Assert.AreEqual(0f, ServiceStatsHudLogic.CalculateMovementDelta(0f, 0f, 3f, 4f, 4.9f), 0.001f);
            Assert.AreEqual(0f, ServiceStatsHudLogic.CalculateMovementDelta(1f, 1f, 1f, 1f, 6f), 0.001f);
        }

        [TestMethod]
        public void FormatDistance_UsesCompactDecimalForHud()
        {
            Assert.AreEqual("0.0", ServiceStatsHudLogic.FormatDistance(-2f));
            Assert.AreEqual("12.3", ServiceStatsHudLogic.FormatDistance(12.34f));
            Assert.AreEqual("124", ServiceStatsHudLogic.FormatDistance(123.9f));
        }

        [TestMethod]
        public void FormatDuration_UsesCompactSecondsAndMinutes()
        {
            Assert.AreEqual("0s", ServiceStatsHudLogic.FormatDuration(-1f));
            Assert.AreEqual("6s", ServiceStatsHudLogic.FormatDuration(5.6f));
            Assert.AreEqual("1m05s", ServiceStatsHudLogic.FormatDuration(65.2f));
        }

        [TestMethod]
        public void CalculateIdleDelta_CanUseIdleOrAsleepThresholds()
        {
            Assert.AreEqual(0f, ServiceStatsHudLogic.CalculateIdleDelta(0f, 0.9f, 1f), 0.001f);
            Assert.AreEqual(0.5f, ServiceStatsHudLogic.CalculateIdleDelta(0.5f, 1f, 1f), 0.001f);
            Assert.AreEqual(2f, ServiceStatsHudLogic.CalculateIdleDelta(3f, 2f, 1f), 0.001f);
            Assert.AreEqual(0f, ServiceStatsHudLogic.CalculateIdleDelta(0f, 4.9f, 5f), 0.001f);
            Assert.AreEqual(0.5f, ServiceStatsHudLogic.CalculateIdleDelta(4.5f, 1f, 5f), 0.001f);
            Assert.AreEqual(2f, ServiceStatsHudLogic.CalculateIdleDelta(8f, 2f, 5f), 0.001f);
        }

        [TestMethod]
        public void BuildDebugText_FormatsPlainUpperRightHudRows()
        {
            List<ServiceStatsCardViewModel> cards = new List<ServiceStatsCardViewModel>
            {
                new ServiceStatsCardViewModel
                {
                    PlayerId = 0,
                    DisplayName = "Clay",
                    Served = 2,
                    OrdersTaken = 3,
                    DishesWashed = 1,
                    ActionsPerformed = 14,
                    DistanceTravelled = 12.34f,
                    IdleTime = 6.2f,
                    AsleepTime = 2.2f
                },
                new ServiceStatsCardViewModel
                {
                    PlayerId = 2,
                    DisplayName = "",
                    Served = 1,
                    OrdersTaken = 0,
                    DishesWashed = 4,
                    ActionsPerformed = 7,
                    DistanceTravelled = 123.9f,
                    IdleTime = 65.2f,
                    AsleepTime = 61.2f
                }
            };

            string text = ServiceStatsHudLogic.BuildDebugText(cards, true, true, true, true, true);

            StringAssert.StartsWith(text, "SERVICE STATS");
            StringAssert.Contains(text, "Clay  srv 2  ord 3  wash 1  act 14  dist 12.3  idle 6s  asleep 2s");
            StringAssert.Contains(text, "P3  srv 1  ord 0  wash 4  act 7  dist 124  idle 1m05s  asleep 1m01s");
        }

        [TestMethod]
        public void BuildDebugText_RespectsHiddenOptionalStats()
        {
            List<ServiceStatsCardViewModel> cards = new List<ServiceStatsCardViewModel>
            {
                new ServiceStatsCardViewModel
                {
                    PlayerId = 1,
                    DisplayName = "Sam",
                    Served = 5,
                    OrdersTaken = 8,
                    DishesWashed = 9,
                    ActionsPerformed = 10,
                    DistanceTravelled = 11f
                }
            };

            string text = ServiceStatsHudLogic.BuildDebugText(cards, false, false, true, false);

            Assert.AreEqual("SERVICE STATS\r\nSam  srv 5  act 10", text);
        }

        [TestMethod]
        public void BuildDebugText_CanHideServedStat()
        {
            List<ServiceStatsCardViewModel> cards = new List<ServiceStatsCardViewModel>
            {
                new ServiceStatsCardViewModel
                {
                    PlayerId = 1,
                    DisplayName = "Sam",
                    Served = 5,
                    OrdersTaken = 8,
                    DishesWashed = 9,
                    ActionsPerformed = 10,
                    DistanceTravelled = 11f
                }
            };

            string text = ServiceStatsHudLogic.BuildDebugText(cards, false, true, false, true, false, false);

            Assert.AreEqual("SERVICE STATS\r\nSam  ord 8  act 10", text);
        }

        [TestMethod]
        public void CalculateAnchoredYOffset_KeepsCurrentMarginOrUsesScreenPercent()
        {
            Assert.AreEqual(-24f, ServiceStatsHudLogic.CalculateAnchoredYOffset(0f, 1080f, 24f), 0.001f);
            Assert.AreEqual(-540f, ServiceStatsHudLogic.CalculateAnchoredYOffset(0.50f, 1080f, 24f), 0.001f);
            Assert.AreEqual(-756f, ServiceStatsHudLogic.CalculateAnchoredYOffset(0.70f, 1080f, 24f), 0.001f);
            Assert.AreEqual(-972f, ServiceStatsHudLogic.CalculateAnchoredYOffset(1.00f, 1080f, 24f), 0.001f);
        }

        [TestMethod]
        public void GetCompletedCleaningCredit_AlwaysCountsCleaningAsAction()
        {
            ServiceStatsCleaningCredit washCredit = ServiceStatsHudLogic.GetCompletedCleaningCredit(true);
            Assert.IsTrue(washCredit.RecordAction);
            Assert.IsTrue(washCredit.RecordWashed);

            ServiceStatsCleaningCredit nonWashCleaningCredit = ServiceStatsHudLogic.GetCompletedCleaningCredit(false);
            Assert.IsTrue(nonWashCleaningCredit.RecordAction);
            Assert.IsFalse(nonWashCleaningCredit.RecordWashed);
        }

        [TestMethod]
        public void GetCompletedCleaningCredit_DoesNotDoubleCountProcessStartActions()
        {
            ServiceStatsCleaningCredit washCredit = ServiceStatsHudLogic.GetCompletedCleaningCredit(true, true);
            Assert.IsFalse(washCredit.RecordAction);
            Assert.IsTrue(washCredit.RecordWashed);

            ServiceStatsCleaningCredit genericProcessCredit = ServiceStatsHudLogic.GetCompletedCleaningCredit(false, true);
            Assert.IsFalse(genericProcessCredit.RecordAction);
            Assert.IsFalse(genericProcessCredit.RecordWashed);
        }

        [TestMethod]
        public void IsCompletedCleaningProcessWashEligible_CountsDishesAndFloorMessesOnly()
        {
            Assert.IsTrue(ServiceStatsHudLogic.IsCompletedCleaningProcessWashEligible(
                isDishCleaningAppliance: true,
                isFloorMessTarget: false));

            Assert.IsTrue(ServiceStatsHudLogic.IsCompletedCleaningProcessWashEligible(
                isDishCleaningAppliance: false,
                isFloorMessTarget: true));

            Assert.IsFalse(ServiceStatsHudLogic.IsCompletedCleaningProcessWashEligible(
                isDishCleaningAppliance: false,
                isFloorMessTarget: false));
        }

        [TestMethod]
        public void GetProcessSnapshotKey_UsesCompletedProcessEntityIndex()
        {
            Assert.AreEqual(42, ServiceStatsHudLogic.GetProcessSnapshotKey(42));
        }

        [TestMethod]
        public void ShouldRecordResolvedServe_RequiresAcceptedTransferPlayerAndReleasedItem()
        {
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordResolvedServe(true, true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordResolvedServe(false, true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordResolvedServe(true, false));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordResolvedServe(true, true, true));
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordResolvedServe(true, true, false));
        }

        [TestMethod]
        public void ShouldRecordDrinkDeliveryServe_RequiresAcceptedDrinkDeliveryWithReleasedItem()
        {
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(
                transferAccepted: true,
                isDrinkItem: true,
                isCustomerDrinkDestination: true,
                hasPlayerActor: true));

            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(false, true, true, true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(true, false, true, true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(true, true, false, true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(true, true, true, false));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(true, true, true, true, true));
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordDrinkDeliveryServe(true, true, true, true, false));
        }

        [TestMethod]
        public void GetServeCredit_CountsSuccessfulServeAsAction()
        {
            ServiceStatsServeCredit credit = ServiceStatsHudLogic.GetServeCredit(true);
            Assert.IsTrue(credit.RecordServed);
            Assert.IsTrue(credit.RecordAction);

            ServiceStatsServeCredit noCredit = ServiceStatsHudLogic.GetServeCredit(false);
            Assert.IsFalse(noCredit.RecordServed);
            Assert.IsFalse(noCredit.RecordAction);
        }

        [TestMethod]
        public void ShouldRecordDurationAction_OnlyRecordsManualPlayerStartEdge()
        {
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordDurationAction(
                isActive: true,
                isManual: true,
                hasPlayerActor: true,
                alreadyActive: false));

            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDurationAction(false, true, true, false));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDurationAction(true, false, true, false));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDurationAction(true, true, false, false));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordDurationAction(true, true, true, true));
        }

        [TestMethod]
        public void ShouldRecordInteractionAttempt_RecordsOnlyPerformedGrabAndAct()
        {
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 1, interactionResult: 2));
            Assert.IsTrue(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 2, interactionResult: 2));

            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 0, interactionResult: 2));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 3, interactionResult: 2));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 1, interactionResult: 1));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldRecordInteractionAttempt(interactionType: 2, interactionResult: 0));
        }

        [TestMethod]
        public void ShouldCreditWashFromInteractionAttempt_OnlyCreditsCompletedFloorCleaningActs()
        {
            Assert.IsTrue(ServiceStatsHudLogic.ShouldCreditWashFromInteractionAttempt(
                interactionType: 2,
                interactionResult: 2,
                isFloorMessTarget: true));

            Assert.IsFalse(ServiceStatsHudLogic.ShouldCreditWashFromInteractionAttempt(
                interactionType: 1,
                interactionResult: 2,
                isFloorMessTarget: true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldCreditWashFromInteractionAttempt(
                interactionType: 2,
                interactionResult: 1,
                isFloorMessTarget: true));
            Assert.IsFalse(ServiceStatsHudLogic.ShouldCreditWashFromInteractionAttempt(
                interactionType: 2,
                interactionResult: 2,
                isFloorMessTarget: false));
        }

        [TestMethod]
        public void DoesVerticalContentFit_CatchesContentClipping()
        {
            Assert.IsFalse(ServiceStatsHudLogic.DoesVerticalContentFit(
                cardHeight: 154f,
                paddingY: 14f,
                headerHeight: 28f,
                contentSpacing: 10f,
                firstSectionHeight: 44f,
                secondSectionHeight: 68f));

            Assert.IsTrue(ServiceStatsHudLogic.DoesVerticalContentFit(
                cardHeight: 196f,
                paddingY: 14f,
                headerHeight: 28f,
                contentSpacing: 10f,
                firstSectionHeight: 44f,
                secondSectionHeight: 68f));
        }

        [TestMethod]
        public void BuildVisibleCards_CopiesActionCountsForMultiplePlayers()
        {
            List<ServiceStatsPlayerState> players = new List<ServiceStatsPlayerState>
            {
                new ServiceStatsPlayerState
                {
                    PlayerId = 0,
                    ResolvedName = "Riley",
                    BadgeColor = Color.red,
                    Served = 2,
                    OrdersTaken = 1,
                    DishesWashed = 0,
                    ActionsPerformed = 14,
                    DistanceTravelled = 42.25f,
                    IdleTime = 3.5f,
                    AsleepTime = 0.5f
                },
                new ServiceStatsPlayerState
                {
                    PlayerId = 1,
                    ResolvedName = "Quinn",
                    BadgeColor = Color.blue,
                    Served = 1,
                    OrdersTaken = 3,
                    DishesWashed = 2,
                    ActionsPerformed = 11,
                    DistanceTravelled = 8.5f,
                    IdleTime = 9f,
                    AsleepTime = 5f
                }
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(players, true);

            Assert.AreEqual(2, cards.Count);
            Assert.AreEqual("Quinn", cards[0].DisplayName);
            Assert.AreEqual(11, cards[0].ActionsPerformed);
            Assert.AreEqual(8.5f, cards[0].DistanceTravelled, 0.001f);
            Assert.AreEqual(9f, cards[0].IdleTime, 0.001f);
            Assert.AreEqual(5f, cards[0].AsleepTime, 0.001f);
            Assert.AreEqual("Riley", cards[1].DisplayName);
            Assert.AreEqual(14, cards[1].ActionsPerformed);
            Assert.AreEqual(42.25f, cards[1].DistanceTravelled, 0.001f);
            Assert.AreEqual(3.5f, cards[1].IdleTime, 0.001f);
            Assert.AreEqual(0.5f, cards[1].AsleepTime, 0.001f);
        }

        [TestMethod]
        public void HudState_PreservesShowActionsAndScaleSettings()
        {
            List<ServiceStatsCardViewModel> cards = new List<ServiceStatsCardViewModel>
            {
                new ServiceStatsCardViewModel
                {
                    PlayerId = 2,
                    DisplayName = "Jamie",
                    BadgeColor = Color.yellow,
                    Served = 1,
                    OrdersTaken = 2,
                    DishesWashed = 3,
                    ActionsPerformed = 9,
                    DistanceTravelled = 10f
                }
            };

            ServiceStatsHudState state = new ServiceStatsHudState(cards, 2, true, false, false, true, 1.25f);

            Assert.AreEqual(1, state.Cards.Count);
            Assert.AreEqual(2, state.ColumnCount);
            Assert.IsTrue(state.ShowServed);
            Assert.IsTrue(state.ShowOrders);
            Assert.IsFalse(state.ShowWashed);
            Assert.IsFalse(state.ShowActions);
            Assert.IsTrue(state.ShowDistance);
            Assert.IsTrue(state.ShowIdle);
            Assert.AreEqual(1.25f, state.ScaleMultiplier);
            Assert.AreEqual(ServiceStatsFontOption.Default, state.Font);
            Assert.AreEqual(9, state.Cards[0].ActionsPerformed);
            Assert.AreEqual(10f, state.Cards[0].DistanceTravelled, 0.001f);
        }

        [TestMethod]
        public void HudState_PreservesSelectedFontOption()
        {
            ServiceStatsHudState state = new ServiceStatsHudState(
                new List<ServiceStatsCardViewModel>(),
                1,
                true,
                true,
                true,
                true,
                1f,
                ServiceStatsFontOption.Alternate2);

            Assert.AreEqual(ServiceStatsFontOption.Alternate2, state.Font);
        }

        [TestMethod]
        public void HudState_PreservesYOffsetPercent()
        {
            ServiceStatsHudState state = new ServiceStatsHudState(
                new List<ServiceStatsCardViewModel>(),
                1,
                true,
                true,
                true,
                true,
                true,
                1f,
                ServiceStatsFontOption.Default,
                0.60f);

            Assert.AreEqual(0.60f, state.YOffsetScreenPercent, 0.001f);
        }

        [TestMethod]
        public void ActionHookRules_TrackKitchenCommonAndKitchenModeAssemblies()
        {
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldScanAssembly("Kitchen.Common"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldScanAssembly("KitchenMode"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldScanAssembly("Kitchen.RestaurantMode"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldScanAssembly(""));
        }

        [TestMethod]
        public void ActionHookRules_KeepChopStyleInstantProcessesEnabled()
        {
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("InstantProcessToolCompleteProcess"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("InstantProcessToolCompleteDuration"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("TakeFromComponentSplit"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("TakeFromCopySplit"));
        }

        [TestMethod]
        public void ActionHookRules_StillExcludeNoisyOrSpecialCaseInteractionSystems()
        {
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("GroupPromptForOrder"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("UseOrderMachine"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("TransferInteractionProposalSystem"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("PseudoInteractProcess"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("ShowPingedApplianceInfo"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericInteractionType("HandleDumbWaiters"));
        }

        [TestMethod]
        public void ActionHookRules_ExcludeServeAcceptorsButKeepCombineTransfers()
        {
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericTransferType("AcceptIntoOrderSatisfaction"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericTransferType("AcceptIntoPiecemealSatisfaction"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackGenericTransferType("AcceptIntoExtraSatisfaction"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericTransferType("AcceptMergeIntoHolder"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackGenericTransferType("AcceptIntoHolder"));
        }

        [TestMethod]
        public void ActionHookRules_TrackProviderReceiveResultsForMachineAndDispenserActions()
        {
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackTransferInteractionResultType("TakeFromProvider"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackTransferInteractionResultType("TakeFromHolder"));
            Assert.IsTrue(ServiceStatsActionHookRules.ShouldTrackTransferInteractionResultType("TakeFromComponentSplit"));
            Assert.IsFalse(ServiceStatsActionHookRules.ShouldTrackTransferInteractionResultType(""));
        }

        [TestMethod]
        public void HudScaleOptions_IncludeLowScalePresetsDownToThirtyPercent()
        {
            Assert.AreEqual(0.30f, ResolveScale(ServiceStatsScaleOption.Thirty), 0.001f);
            Assert.AreEqual(0.45f, ResolveScale(ServiceStatsScaleOption.FortyFive), 0.001f);
            Assert.AreEqual(0.60f, ResolveScale(ServiceStatsScaleOption.Sixty), 0.001f);
            Assert.AreEqual(0.75f, ResolveScale(ServiceStatsScaleOption.SeventyFive), 0.001f);
            Assert.AreEqual(1.00f, ResolveScale(ServiceStatsScaleOption.Normal), 0.001f);
        }

        [TestMethod]
        public void SettingsScaleMenu_ExposesIndexedOptionsDownToThirtyPercent()
        {
            ServiceStatsScaleOption[] options = GetPrivateStaticField<ServiceStatsScaleOption[]>("ScaleOptions");
            string[] labels = GetPrivateStaticField<string[]>("ScaleLabels");

            CollectionAssert.AreEqual(
                new[]
                {
                    ServiceStatsScaleOption.Thirty,
                    ServiceStatsScaleOption.FortyFive,
                    ServiceStatsScaleOption.Sixty,
                    ServiceStatsScaleOption.SeventyFive,
                    ServiceStatsScaleOption.Small,
                    ServiceStatsScaleOption.Normal,
                    ServiceStatsScaleOption.Large,
                    ServiceStatsScaleOption.ExtraLarge
                },
                options);
            CollectionAssert.AreEqual(
                new[] { "30%", "45%", "60%", "75%", "85%", "100%", "115%", "130%" },
                labels);
            Assert.AreEqual(options.Length, labels.Length);
            Assert.AreEqual(8, options.Length);
        }

        [TestMethod]
        public void SettingsFontMenu_DefaultsToAltFontOneAndKeepsDefaultSelectable()
        {
            ServiceStatsFontOption[] options = GetPrivateStaticField<ServiceStatsFontOption[]>("FontOptions");
            string[] labels = GetPrivateStaticField<string[]>("FontLabels");

            CollectionAssert.AreEqual(
                new[]
                {
                    ServiceStatsFontOption.Default,
                    ServiceStatsFontOption.Alternate1,
                    ServiceStatsFontOption.Alternate2,
                    ServiceStatsFontOption.Alternate3,
                    ServiceStatsFontOption.Alternate4
                },
                options);
            CollectionAssert.AreEqual(
                new[] { "Default Font", "Alt Font 1", "Alt Font 2", "Alt Font 3", "Alt Font 4" },
                labels);
            Assert.AreEqual(ServiceStatsFontOption.Alternate1, ServiceStatsSettings.Font);
            Assert.AreEqual(options.Length, labels.Length);
        }

        [TestMethod]
        public void SettingsYOffsetMenu_DefaultsCurrentAndAddsTenThroughNinetyPercentSteps()
        {
            ServiceStatsYOffsetOption[] options = GetPrivateStaticField<ServiceStatsYOffsetOption[]>("YOffsetOptions");
            string[] labels = GetPrivateStaticField<string[]>("YOffsetLabels");

            CollectionAssert.AreEqual(
                new[]
                {
                    ServiceStatsYOffsetOption.Current,
                    ServiceStatsYOffsetOption.Ten,
                    ServiceStatsYOffsetOption.Twenty,
                    ServiceStatsYOffsetOption.Thirty,
                    ServiceStatsYOffsetOption.Forty,
                    ServiceStatsYOffsetOption.Fifty,
                    ServiceStatsYOffsetOption.Sixty,
                    ServiceStatsYOffsetOption.Seventy,
                    ServiceStatsYOffsetOption.Eighty,
                    ServiceStatsYOffsetOption.Ninety
                },
                options);
            CollectionAssert.AreEqual(
                new[] { "Current Y", "10% Down", "20% Down", "30% Down", "40% Down", "50% Down", "60% Down", "70% Down", "80% Down", "90% Down" },
                labels);
            Assert.AreEqual(ServiceStatsYOffsetOption.Current, ServiceStatsSettings.YOffset);
            Assert.AreEqual(0f, ServiceStatsSettings.YOffsetScreenPercent, 0.001f);
            Assert.AreEqual(options.Length, labels.Length);
        }

        [TestMethod]
        public void SettingsDistanceToggle_DefaultsVisible()
        {
            Assert.IsTrue(ServiceStatsSettings.ShowServed);
            Assert.IsFalse(ServiceStatsSettings.ShowWashed);
            Assert.IsFalse(ServiceStatsSettings.ShowActions);
            Assert.IsTrue(ServiceStatsSettings.ShowDistance);
            Assert.IsTrue(ServiceStatsSettings.ShowIdle);
        }

        [TestMethod]
        public void SettingsIdleThresholdMenus_DefaultCurrentValuesAndExposeOneToTwentySeconds()
        {
            int[] options = GetPrivateStaticField<int[]>("ThresholdSecondOptions");
            string[] labels = GetPrivateStaticField<string[]>("ThresholdSecondLabels");

            CollectionAssert.AreEqual(
                new[] { 1, 3, 4, 5, 7, 9, 11, 13, 15, 17, 19, 20 },
                options);
            CollectionAssert.AreEqual(
                new[] { "1s", "3s", "4s", "5s", "7s", "9s", "11s", "13s", "15s", "17s", "19s", "20s" },
                labels);
            Assert.AreEqual(options.Length, labels.Length);
            Assert.AreEqual(1, ServiceStatsSettings.IdleThresholdSeconds);
            Assert.AreEqual(5, ServiceStatsSettings.AsleepThresholdSeconds);
        }

        [TestMethod]
        public void SyncFromPreferences_AppliesSavedServedVisibility()
        {
            InvokeApplyPreferenceSnapshot(showServed: false);

            Assert.IsFalse(ServiceStatsSettings.ShowServed);
        }

        [TestMethod]
        public void SyncFromPreferences_AppliesSavedIdleAndAsleepThresholds()
        {
            InvokeApplyPreferenceSnapshot(idleThresholdSeconds: 3, asleepThresholdSeconds: 19);

            Assert.AreEqual(3, ServiceStatsSettings.IdleThresholdSeconds);
            Assert.AreEqual(19, ServiceStatsSettings.AsleepThresholdSeconds);
        }

        [TestMethod]
        public void SyncFromPreferences_NormalizesIdleThresholdsToNearestOption()
        {
            InvokeApplyPreferenceSnapshot(idleThresholdSeconds: 4, asleepThresholdSeconds: 99);

            Assert.AreEqual(4, ServiceStatsSettings.IdleThresholdSeconds);
            Assert.AreEqual(20, ServiceStatsSettings.AsleepThresholdSeconds);
        }

        [TestMethod]
        public void SyncFromPreferences_AppliesSavedShowEveryoneBeforeMenuToggle()
        {
            InvokeApplyPreferenceSnapshot(hideZeroServePlayers: false);

            Assert.IsFalse(ServiceStatsSettings.HideZeroServePlayers, "Saved Show Everyone should be applied without requiring a menu toggle.");

            ServiceStatsPlayerState player = new ServiceStatsPlayerState
            {
                PlayerId = 0,
                ResolvedName = "Clay",
                BadgeColor = Color.white,
                Served = 0
            };

            List<ServiceStatsCardViewModel> cards = ServiceStatsHudLogic.BuildVisibleCards(new[] { player }, ServiceStatsSettings.HideZeroServePlayers);
            Assert.AreEqual(1, cards.Count);
        }

        [TestMethod]
        public void SettingsIndexMapping_ResolvesCurrentScaleFontYOffsetAndSplitChoices()
        {
            ServiceStatsScaleOption[] scaleOptions = GetPrivateStaticField<ServiceStatsScaleOption[]>("ScaleOptions");
            ServiceStatsFontOption[] fontOptions = GetPrivateStaticField<ServiceStatsFontOption[]>("FontOptions");
            ServiceStatsYOffsetOption[] yOffsetOptions = GetPrivateStaticField<ServiceStatsYOffsetOption[]>("YOffsetOptions");
            ServiceStatsSplitThresholdOption[] splitOptions = GetPrivateStaticField<ServiceStatsSplitThresholdOption[]>("SplitThresholdOptions");

            Assert.AreEqual(0, InvokeGetSelectedIndex(scaleOptions, ServiceStatsScaleOption.Thirty));
            Assert.AreEqual(5, InvokeGetSelectedIndex(scaleOptions, ServiceStatsScaleOption.Normal));
            Assert.AreEqual(0, InvokeGetSelectedIndex(fontOptions, ServiceStatsFontOption.Default));
            Assert.AreEqual(3, InvokeGetSelectedIndex(fontOptions, ServiceStatsFontOption.Alternate3));
            Assert.AreEqual(0, InvokeGetSelectedIndex(yOffsetOptions, ServiceStatsYOffsetOption.Current));
            Assert.AreEqual(4, InvokeGetSelectedIndex(yOffsetOptions, ServiceStatsYOffsetOption.Forty));
            Assert.AreEqual(8, InvokeGetSelectedIndex(yOffsetOptions, ServiceStatsYOffsetOption.Eighty));
            Assert.AreEqual(2, InvokeGetSelectedIndex(splitOptions, ServiceStatsSplitThresholdOption.Eight));
        }

        [TestMethod]
        public void LifecycleResetLogic_ResetsOnlyWhenGameplayStarts()
        {
            bool hasObservedDayPhase = false;
            bool lastObservedDayPhase = false;

            Assert.IsFalse(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(false, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsTrue(hasObservedDayPhase);
            Assert.IsFalse(lastObservedDayPhase);

            Assert.IsFalse(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(false, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsTrue(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(true, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsTrue(lastObservedDayPhase);
            Assert.IsFalse(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(true, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsFalse(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(false, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsFalse(lastObservedDayPhase);
            Assert.IsTrue(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(true, ref hasObservedDayPhase, ref lastObservedDayPhase));
        }

        [TestMethod]
        public void LifecycleResetLogic_ResetsIfFirstObservedStateIsGameplay()
        {
            bool hasObservedDayPhase = false;
            bool lastObservedDayPhase = false;

            Assert.IsTrue(ServiceStatsLifecycleLogic.ShouldResetForDayPhase(true, ref hasObservedDayPhase, ref lastObservedDayPhase));
            Assert.IsTrue(hasObservedDayPhase);
            Assert.IsTrue(lastObservedDayPhase);
        }

        private static float ResolveScale(ServiceStatsScaleOption option)
        {
            switch (option)
            {
                case ServiceStatsScaleOption.Thirty:
                    return 0.30f;
                case ServiceStatsScaleOption.FortyFive:
                    return 0.45f;
                case ServiceStatsScaleOption.Sixty:
                    return 0.60f;
                case ServiceStatsScaleOption.SeventyFive:
                    return 0.75f;
                case ServiceStatsScaleOption.Small:
                    return 0.85f;
                case ServiceStatsScaleOption.Large:
                    return 1.15f;
                case ServiceStatsScaleOption.ExtraLarge:
                    return 1.30f;
                default:
                    return 1f;
            }
        }

        private static T GetPrivateStaticField<T>(string fieldName)
        {
            FieldInfo field = typeof(ServiceStatsSettings).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, $"Expected private static field '{fieldName}' to exist on ServiceStatsSettings.");
            return (T)field.GetValue(null);
        }

        private static void SetPrivateStaticField(string fieldName, object value)
        {
            FieldInfo field = typeof(ServiceStatsSettings).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, $"Expected private static field '{fieldName}' to exist on ServiceStatsSettings.");
            field.SetValue(null, value);
        }

        private static void SetPrivateStaticProperty(string propertyName, object value)
        {
            PropertyInfo property = typeof(ServiceStatsSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(property, $"Expected static property '{propertyName}' to exist on ServiceStatsSettings.");
            property.SetValue(null, value, null);
        }

        private static int InvokeGetSelectedIndex<T>(T[] values, T currentValue)
        {
            MethodInfo method = typeof(ServiceStatsSettings).GetMethod("GetSelectedIndex", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Expected GetSelectedIndex to exist on ServiceStatsSettings.");
            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
            return (int)genericMethod.Invoke(null, new object[] { values, currentValue });
        }

        private static void InvokeApplyPreferenceSnapshot(
            bool? enabled = null,
            bool? showServed = null,
            bool? showOrders = null,
            bool? showWashed = null,
            bool? showActions = null,
            bool? showDistance = null,
            bool? showIdle = null,
            bool? hideZeroServePlayers = null,
            int? idleThresholdSeconds = null,
            int? asleepThresholdSeconds = null,
            int? scaleIndex = null,
            int? fontIndex = null,
            int? yOffsetIndex = null,
            int? splitThresholdIndex = null)
        {
            MethodInfo method = typeof(ServiceStatsSettings).GetMethod("ApplyPreferenceSnapshot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Expected ApplyPreferenceSnapshot to exist on ServiceStatsSettings.");
            method.Invoke(
                null,
                new object[]
                {
                    enabled,
                    showServed,
                    showOrders,
                    showWashed,
                    showActions,
                    showDistance,
                    showIdle,
                    hideZeroServePlayers,
                    idleThresholdSeconds,
                    asleepThresholdSeconds,
                    scaleIndex,
                    fontIndex,
                    yOffsetIndex,
                    splitThresholdIndex
                });
        }
    }
}
