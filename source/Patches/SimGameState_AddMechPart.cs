using BattleTech;

namespace CustomSalvage;

[HarmonyPatch(typeof(SimGameState), "AddMechPart")]
public static class SimGameState_AddMechPart
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.VeryHigh)]
    public static void Prefix(ref bool __runOriginal, SimGameState __instance, string id)
    {
        if (!__runOriginal)
        {
            return;
        }

        __instance.AddItemStat(id, "MECHPART", false);
        __runOriginal = false;
    }
}