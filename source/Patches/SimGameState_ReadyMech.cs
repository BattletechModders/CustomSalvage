using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSalvage.Patches
{
    [HarmonyPatch(typeof(SimGameState), "ReadyMech")]
    public static class SimGameState_ReadyMech_Patch
    {
        public static void Postfix(int baySlot, string id, SimGameState __instance)
        {
            ChassisHandler.SanitizeUniqueUnits(__instance);
        }
    }
}
