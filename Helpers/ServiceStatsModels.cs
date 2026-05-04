using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KitchenServiceStatsHUD.Helpers
{
    public class ServiceStatsPlayerState
    {
        public int PlayerId;
        public int DisplayIndex = -1;
        public string ResolvedName;
        public Color BadgeColor;
        public int Served;
        public int OrdersTaken;
        public int DishesWashed;
        public int ActionsPerformed;
        public float DistanceTravelled;
        public float IdleTime;
        public float AsleepTime;
        public float SecondsSinceLastAction;

        public void ResetDailyTotals()
        {
            Served = 0;
            OrdersTaken = 0;
            DishesWashed = 0;
            ActionsPerformed = 0;
            DistanceTravelled = 0f;
            IdleTime = 0f;
            AsleepTime = 0f;
            SecondsSinceLastAction = 0f;
        }
    }

    public class ServiceStatsCardViewModel
    {
        public int PlayerId;
        public int DisplayIndex;
        public string DisplayName;
        public Color BadgeColor;
        public int Served;
        public int OrdersTaken;
        public int DishesWashed;
        public int ActionsPerformed;
        public float DistanceTravelled;
        public float IdleTime;
        public float AsleepTime;
    }

    public class ServiceStatsHudState
    {
        public static readonly ServiceStatsHudState Empty = new ServiceStatsHudState(new List<ServiceStatsCardViewModel>(), 1, true, true, true, true, true, 1f);

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showOrders, bool showWashed, bool showActions, bool showDistance, float scaleMultiplier)
            : this(cards, columnCount, true, showOrders, showWashed, showActions, showDistance, true, scaleMultiplier, ServiceStatsFontOption.Default, 0f)
        {
        }

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showServed, bool showOrders, bool showWashed, bool showActions, bool showDistance, float scaleMultiplier)
            : this(cards, columnCount, showServed, showOrders, showWashed, showActions, showDistance, true, scaleMultiplier, ServiceStatsFontOption.Default, 0f)
        {
        }

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showOrders, bool showWashed, bool showActions, bool showDistance, float scaleMultiplier, ServiceStatsFontOption font)
            : this(cards, columnCount, true, showOrders, showWashed, showActions, showDistance, true, scaleMultiplier, font, 0f)
        {
        }

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showOrders, bool showWashed, bool showActions, bool showDistance, float scaleMultiplier, ServiceStatsFontOption font, float yOffsetScreenPercent)
            : this(cards, columnCount, true, showOrders, showWashed, showActions, showDistance, true, scaleMultiplier, font, yOffsetScreenPercent)
        {
        }

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showOrders, bool showWashed, bool showActions, bool showDistance, bool showIdle, float scaleMultiplier, ServiceStatsFontOption font, float yOffsetScreenPercent)
            : this(cards, columnCount, true, showOrders, showWashed, showActions, showDistance, showIdle, scaleMultiplier, font, yOffsetScreenPercent)
        {
        }

        public ServiceStatsHudState(List<ServiceStatsCardViewModel> cards, int columnCount, bool showServed, bool showOrders, bool showWashed, bool showActions, bool showDistance, bool showIdle, float scaleMultiplier, ServiceStatsFontOption font, float yOffsetScreenPercent)
        {
            Cards = cards ?? new List<ServiceStatsCardViewModel>();
            ColumnCount = columnCount < 1 ? 1 : columnCount;
            ShowServed = showServed;
            ShowOrders = showOrders;
            ShowWashed = showWashed;
            ShowActions = showActions;
            ShowDistance = showDistance;
            ShowIdle = showIdle;
            ScaleMultiplier = scaleMultiplier <= 0f ? 1f : scaleMultiplier;
            Font = font;
            YOffsetScreenPercent = yOffsetScreenPercent < 0f ? 0f : yOffsetScreenPercent;
        }

        public List<ServiceStatsCardViewModel> Cards { get; private set; }
        public int ColumnCount { get; private set; }
        public bool ShowServed { get; private set; }
        public bool ShowOrders { get; private set; }
        public bool ShowWashed { get; private set; }
        public bool ShowActions { get; private set; }
        public bool ShowDistance { get; private set; }
        public bool ShowIdle { get; private set; }
        public float ScaleMultiplier { get; private set; }
        public ServiceStatsFontOption Font { get; private set; }
        public float YOffsetScreenPercent { get; private set; }
    }

    internal struct ServiceStatsProcessSnapshot
    {
        public Entity Item;
        public Entity RawActor;
        public Entity Actor;
        public Entity Appliance;
        public int Process;
        public bool IsWashCleaningProcess;
        public bool ActionRecorded;
    }

    internal struct ServiceStatsServeAcceptanceState
    {
        public bool ShouldRecord;
        public Entity Transfer;
        public Entity Acceptance;
        public Entity Player;
        public Entity Item;
        public Entity Group;
        public int OrderIndex;
        public bool IsExtra;
        public bool WasSatisfied;
    }

    internal struct ServiceStatsOrderActionState
    {
        public bool ShouldRecord;
        public Entity Player;
    }

    internal struct ServiceStatsTransferActionState
    {
        public bool ShouldRecord;
        public Entity Transfer;
        public Entity Player;
    }

    internal struct ServiceStatsMovementSnapshot
    {
        public bool HasPosition;
        public Vector3 LastPosition;
    }
}
