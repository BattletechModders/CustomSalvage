using System.Collections.Generic;
using System.Linq;
using BattleTech;

namespace CustomSalvage.MechBroke.Conditions
{
    public static class MechTags
    {
        private static HashSet<string> mechtags;
        public static bool Handler(MechDef mech, Condition condition)
        {
            if (mechtags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return condition.Strings.All(tag => mechtags.Contains(tag));
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
            mechtags = ChassisHandler.GetMechTags(mech);
        }
    }

    public static class AnyMechTags
    {
        private static HashSet<string> mechtags;
        public static bool Handler(MechDef mech, Condition condition)
        {
            if (mechtags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return condition.Strings.Any(tag => mechtags.Contains(tag));
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
            mechtags = ChassisHandler.GetMechTags(mech);
        }
    }



    public static class placehlder
    {
        public static bool Handler(MechDef mech, Condition condition)
        {
            return false;
        }

        public static void Prepare(MechDef mech, SimGameState sim)
        {
        }
    }
}