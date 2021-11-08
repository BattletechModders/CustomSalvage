using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSalvage
{
    public static partial class ChassisHandler
    {


#if USE_CC
        public static IAssemblyVariant get_variant(MechDef mech)
        {
            return mech.Chassis.GetComponent<IAssemblyVariant>();
        }
#endif
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
