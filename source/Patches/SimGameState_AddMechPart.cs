using BattleTech;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(SimGameState), "AddMechPart")]
    public static class SimGameState_AddMechPart
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        public static bool AddMechPart(SimGameState __instance, string id)
        {
            __instance.AddItemStat(id, "MECHPART", false);
            return false;
        }
    }
}