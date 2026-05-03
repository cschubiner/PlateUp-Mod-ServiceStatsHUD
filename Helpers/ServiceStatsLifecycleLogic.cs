namespace KitchenServiceStatsHUD.Helpers
{
    public static class ServiceStatsLifecycleLogic
    {
        public static bool ShouldResetForDayPhase(bool isDayTime, ref bool hasObservedDayPhase, ref bool lastObservedDayPhase)
        {
            if (!hasObservedDayPhase)
            {
                hasObservedDayPhase = true;
                lastObservedDayPhase = isDayTime;
                return true;
            }

            if (lastObservedDayPhase == isDayTime)
            {
                return false;
            }

            lastObservedDayPhase = isDayTime;
            return true;
        }
    }
}
