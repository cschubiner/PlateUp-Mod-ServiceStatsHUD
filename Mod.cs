using HarmonyLib;
using Kitchen;
using KitchenLib;
using KitchenLib.Interfaces;
using KitchenLib.Logging;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;
using KitchenServiceStatsHUD.Visuals;
using System.Reflection;

namespace KitchenServiceStatsHUD
{
    public class Mod : BaseMod, IAutoRegisterAll
    {
        public const string MOD_GUID = "dev.clay.plateup.servicestatshud";
        public const string MOD_NAME = "Service Stats HUD";
        public const string MOD_AUTHOR = "Clay";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_GAMEVERSION = ">=1.2.0";

        private static readonly KitchenLogger Logger = new KitchenLogger(MOD_NAME);
        private static bool _patched;

        public Mod() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly())
        {
            if (!_patched)
            {
                new Harmony(MOD_GUID).PatchAll(Assembly.GetExecutingAssembly());
                _patched = true;
            }
        }

        protected override void OnInitialise()
        {
            ServiceStatsRuntime.ResetAll();
            ServiceStatsHudManager.GetOrCreate();
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            ServiceStatsSettings.RegisterMenus();
            LogInfo($"Activated {MOD_NAME} v{MOD_VERSION} for PlateUp {MOD_GAMEVERSION}");
        }

        protected override void OnFrameUpdate()
        {
            ServiceStatsHudManager.GetOrCreate().Sync(ServiceStatsRuntime.BuildHudState());
        }

        public static void LogInfo(string message)
        {
            Logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            Logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            Logger.LogError(message);
        }
    }
}
