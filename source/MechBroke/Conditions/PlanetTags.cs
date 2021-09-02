using System.Collections.Generic;
using System.Linq;
using BattleTech;

namespace CustomSalvage.MechBroke.Conditions
{
    public static class PlanetTags
    {
        private static HashSet<string> tags;
        public static bool Handler(MechDef mech, Condition condition)
        {
            if (tags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return condition.Strings.All(tag => tags.Contains(tag));
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
            tags = sim.CurSystem.Def.Tags.ToHashSet();
        }
    }

    public static class AnyPlanetTags
    {
        private static HashSet<string> tags;
        public static bool Handler(MechDef mech, Condition condition)
        {
            if (tags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return condition.Strings.Any(tag => tags.Contains(tag));
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
            tags = sim.CurSystem.Def.Tags.ToHashSet();
        }
    }
}