using BattleTech;

namespace CustomSalvage.MechBroke.Conditions
{
    public static class CompanyStat
    {
        private static StatCollection stat;
        public static bool Handler(MechDef mech, Condition condition)
        {
            if (condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return stat.GetStatistic(condition.Strings[0]) == null;
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
            stat = sim.CompanyStats;
        }
    }
}