using System;
using System.Linq;
using System.Reflection;
using PreferenceSystem;

namespace KitchenServiceStatsHUD.Helpers
{
    public enum ServiceStatsScaleOption
    {
        Thirty,
        FortyFive,
        Sixty,
        SeventyFive,
        Small,
        Normal,
        Large,
        ExtraLarge
    }

    public enum ServiceStatsSplitThresholdOption
    {
        Four = 4,
        Six = 6,
        Eight = 8
    }

    public enum ServiceStatsLayoutOption
    {
        CompactGrid,
        WideRibbon,
        TallStack,
        MiniChips,
        Spotlight,
        SlimColumn
    }

    public enum ServiceStatsFontOption
    {
        Default,
        Alternate1,
        Alternate2,
        Alternate3,
        Alternate4
    }

    public enum ServiceStatsYOffsetOption
    {
        Current,
        Ten,
        Twenty,
        Thirty,
        Forty,
        Fifty,
        Sixty,
        Seventy,
        Eighty,
        Ninety
    }

    public static class ServiceStatsSettings
    {
        private static bool _registered;
        private static readonly ServiceStatsScaleOption[] ScaleOptions =
        {
            ServiceStatsScaleOption.Thirty,
            ServiceStatsScaleOption.FortyFive,
            ServiceStatsScaleOption.Sixty,
            ServiceStatsScaleOption.SeventyFive,
            ServiceStatsScaleOption.Small,
            ServiceStatsScaleOption.Normal,
            ServiceStatsScaleOption.Large,
            ServiceStatsScaleOption.ExtraLarge
        };
        private static readonly string[] ScaleLabels = { "30%", "45%", "60%", "75%", "85%", "100%", "115%", "130%" };
        private static readonly ServiceStatsLayoutOption[] LayoutOptions =
        {
            ServiceStatsLayoutOption.CompactGrid,
            ServiceStatsLayoutOption.WideRibbon,
            ServiceStatsLayoutOption.TallStack,
            ServiceStatsLayoutOption.MiniChips,
            ServiceStatsLayoutOption.Spotlight,
            ServiceStatsLayoutOption.SlimColumn
        };
        private static readonly string[] LayoutLabels = { "Compact Grid", "Wide Ribbon", "Tall Stack", "Mini Chips", "Spotlight", "Slim Column" };
        private static readonly ServiceStatsFontOption[] FontOptions =
        {
            ServiceStatsFontOption.Default,
            ServiceStatsFontOption.Alternate1,
            ServiceStatsFontOption.Alternate2,
            ServiceStatsFontOption.Alternate3,
            ServiceStatsFontOption.Alternate4
        };
        private static readonly string[] FontLabels = { "Default Font", "Alt Font 1", "Alt Font 2", "Alt Font 3", "Alt Font 4" };
        private static readonly ServiceStatsYOffsetOption[] YOffsetOptions =
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
        };
        private static readonly string[] YOffsetLabels = { "Current Y", "10% Down", "20% Down", "30% Down", "40% Down", "50% Down", "60% Down", "70% Down", "80% Down", "90% Down" };
        private static readonly ServiceStatsSplitThresholdOption[] SplitThresholdOptions =
        {
            ServiceStatsSplitThresholdOption.Four,
            ServiceStatsSplitThresholdOption.Six,
            ServiceStatsSplitThresholdOption.Eight
        };
        private static readonly string[] SplitThresholdLabels = { "Split After 4", "Split After 6", "Split After 8" };

        public static bool Enabled { get; private set; } = true;
        public static bool ShowOrders { get; private set; } = true;
        public static bool ShowWashed { get; private set; } = true;
        public static bool ShowActions { get; private set; } = true;
        public static bool ShowDistance { get; private set; } = true;
        public static bool ShowIdle { get; private set; } = true;
        public static bool HideZeroServePlayers { get; private set; } = true;
        public static ServiceStatsScaleOption Scale { get; private set; } = ServiceStatsScaleOption.Normal;
        public static ServiceStatsLayoutOption Layout { get; private set; } = ServiceStatsLayoutOption.CompactGrid;
        public static ServiceStatsFontOption Font { get; private set; } = ServiceStatsFontOption.Alternate1;
        public static ServiceStatsYOffsetOption YOffset { get; private set; } = ServiceStatsYOffsetOption.Current;
        public static ServiceStatsSplitThresholdOption SplitThreshold { get; private set; } = ServiceStatsSplitThresholdOption.Six;

        public static float ScaleMultiplier
        {
            get
            {
                switch (Scale)
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
        }

        public static float YOffsetScreenPercent
        {
            get
            {
                switch (YOffset)
                {
                    case ServiceStatsYOffsetOption.Ten:
                        return 0.10f;
                    case ServiceStatsYOffsetOption.Twenty:
                        return 0.20f;
                    case ServiceStatsYOffsetOption.Thirty:
                        return 0.30f;
                    case ServiceStatsYOffsetOption.Forty:
                        return 0.40f;
                    case ServiceStatsYOffsetOption.Fifty:
                        return 0.50f;
                    case ServiceStatsYOffsetOption.Sixty:
                        return 0.60f;
                    case ServiceStatsYOffsetOption.Seventy:
                        return 0.70f;
                    case ServiceStatsYOffsetOption.Eighty:
                        return 0.80f;
                    case ServiceStatsYOffsetOption.Ninety:
                        return 0.90f;
                    default:
                        return 0f;
                }
            }
        }

        public static void RegisterMenus()
        {
            if (_registered)
            {
                return;
            }

            PreferenceSystemManager manager = new PreferenceSystemManager(Mod.MOD_GUID, Mod.MOD_NAME)
                .AddSubmenu("Service Stats HUD", "service_stats_hud", false)
                .AddLabel("Scoreboard Settings")
                .AddInfo("Change visibility and layout from the main menu or during a run.")
                .AddOption(
                    "hud_enabled",
                    Enabled,
                    new[] { true, false },
                    new[] { "HUD On", "HUD Off" },
                    value => Enabled = value)
                .AddOption(
                    "show_orders",
                    ShowOrders,
                    new[] { true, false },
                    new[] { "Show Orders", "Hide Orders" },
                    value => ShowOrders = value)
                .AddOption(
                    "show_washed",
                    ShowWashed,
                    new[] { true, false },
                    new[] { "Show Washed", "Hide Washed" },
                    value => ShowWashed = value)
                .AddOption(
                    "show_actions",
                    ShowActions,
                    new[] { true, false },
                    new[] { "Show Actions", "Hide Actions" },
                    value => ShowActions = value)
                .AddOption(
                    "show_distance",
                    ShowDistance,
                    new[] { true, false },
                    new[] { "Show Distance", "Hide Distance" },
                    value => ShowDistance = value)
                .AddOption(
                    "show_idle",
                    ShowIdle,
                    new[] { true, false },
                    new[] { "Show Idle/Asleep", "Hide Idle/Asleep" },
                    value => ShowIdle = value)
                .AddOption(
                    "hide_zero_serve",
                    HideZeroServePlayers,
                    new[] { true, false },
                    new[] { "Hide Zero-Serve", "Show Everyone" },
                    value => HideZeroServePlayers = value)
                .AddOption(
                    "hud_scale",
                    GetSelectedIndex(ScaleOptions, Scale),
                    Enumerable.Range(0, ScaleOptions.Length).ToArray(),
                    ScaleLabels,
                    value => Scale = ScaleOptions[ClampIndex(value, ScaleOptions.Length)])
                .AddOption(
                    "hud_layout",
                    GetSelectedIndex(LayoutOptions, Layout),
                    Enumerable.Range(0, LayoutOptions.Length).ToArray(),
                    LayoutLabels,
                    value => Layout = LayoutOptions[ClampIndex(value, LayoutOptions.Length)],
                    true)
                .AddOption(
                    "hud_font",
                    GetSelectedIndex(FontOptions, Font),
                    Enumerable.Range(0, FontOptions.Length).ToArray(),
                    FontLabels,
                    value => Font = FontOptions[ClampIndex(value, FontOptions.Length)],
                    true)
                .AddOption(
                    "hud_y_offset",
                    GetSelectedIndex(YOffsetOptions, YOffset),
                    Enumerable.Range(0, YOffsetOptions.Length).ToArray(),
                    YOffsetLabels,
                    value => YOffset = YOffsetOptions[ClampIndex(value, YOffsetOptions.Length)],
                    true)
                .AddOption(
                    "split_threshold",
                    GetSelectedIndex(SplitThresholdOptions, SplitThreshold),
                    Enumerable.Range(0, SplitThresholdOptions.Length).ToArray(),
                    SplitThresholdLabels,
                    value => SplitThreshold = SplitThresholdOptions[ClampIndex(value, SplitThresholdOptions.Length)]);

            Type registryType = Type.GetType("PreferenceSystem.PreferenceSystemRegistry, PreferenceSystem-Workshop");
            MethodInfo addMethod = registryType?.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (addMethod == null)
            {
                Mod.LogWarning("PreferenceSystem registry could not be resolved. Service Stats HUD settings were not registered.");
                return;
            }

            addMethod.Invoke(null, new object[] { manager });
            manager.RegisterMenu(PreferenceSystemManager.MenuType.MainMenu);
            manager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
            _registered = true;
        }

        private static int GetSelectedIndex<T>(T[] values, T currentValue)
        {
            int index = Array.IndexOf(values, currentValue);
            return index >= 0 ? index : 0;
        }

        private static int ClampIndex(int index, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            if (index < 0)
            {
                return 0;
            }

            if (index >= length)
            {
                return length - 1;
            }

            return index;
        }
    }
}
