using System.Collections.Generic;
using System.Linq;
using BattleTech;

namespace CustomSalvage.MechBroke.Conditions
{
    public static class PilotTag
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
            tags = new HashSet<string>();

            foreach (var pilot in sim.PilotRoster)
                if(pilot?.pilotDef?.PilotTags != null)
                    tags.UnionWith(pilot.pilotDef.PilotTags);
        }
    }

    public static class AnyPilotTag
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
            tags = new HashSet<string>();
            tags = new HashSet<string>();

            foreach (var pilot in sim.PilotRoster)
                if (pilot?.pilotDef?.PilotTags != null)
                    tags.UnionWith(pilot.pilotDef.PilotTags);
        }
    }
}