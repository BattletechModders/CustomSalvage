using BattleTech;
using CustomComponents;
using System.Collections.Generic;

namespace CustomSalvage
{
    public static partial class ChassisHandler
    {
        public static IAssemblyVariant get_variant(MechDef mech)
        {
            return mech.Chassis.GetComponent<IAssemblyVariant>();
        }

        private static HashSet<string> build_mech_tags(MechDef mech)
        {
            var result = new HashSet<string>();
            if (mech.MechTags != null)
                result.UnionWith(mech.MechTags);
            if (mech.Chassis.ChassisTags != null)
                result.UnionWith(mech.Chassis.ChassisTags);

            return result;
        }


    }
}
