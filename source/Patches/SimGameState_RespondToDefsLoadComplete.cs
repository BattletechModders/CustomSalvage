using BattleTech;
using Harmony;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(SimGameState), "RespondToDefsLoadComplete")]
    public static class SimGameState_RespondToDefsLoadComplete
    {
        [HarmonyPrefix]
        public static void FixDefaults(SimGameState __instance)
        {
            var items = Control.Instance.Settings.IconTags;
            if (items != null && items.Length > 0)
                foreach (var tagiconDef in items)
                {
                    tagiconDef.Complete(__instance);
                }

        }
    }
}