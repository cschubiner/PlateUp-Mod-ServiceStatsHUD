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
                return isDayTime;
            }

            if (lastObservedDayPhase == isDayTime)
            {
                return false;
            }

            bool shouldReset = !lastObservedDayPhase && isDayTime;
            lastObservedDayPhase = isDayTime;
            return shouldReset;
        }
    }
}
