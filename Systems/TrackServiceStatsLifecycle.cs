using Kitchen;
using KitchenMods;
using KitchenServiceStatsHUD.Helpers;

namespace KitchenServiceStatsHUD.Systems
{
    public class TrackServiceStatsLifecycle : GenericSystemBase, IModSystem
    {
        protected override void OnUpdate()
        {
            ServiceStatsRuntime.RefreshDayPhase(Has<SIsDayTime>());
        }
    }
}
