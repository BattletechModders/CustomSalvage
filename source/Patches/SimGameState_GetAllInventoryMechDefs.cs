using System;
using System.Collections.Generic;
using BattleTech;

namespace CustomSalvage;

[HarmonyPatch(typeof(SimGameState))]
[HarmonyPatch("GetAllInventoryMechDefs")]
public static class SimGameState_GetAllInventoryMechDefs
{
    private static bool show = false;


    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Prefix(ref bool __runOriginal, bool showMechParts, ref List<ChassisDef> __result, SimGameState __instance)
    {
        if (!__runOriginal)
        {
            return;
        }

        __result = new List<ChassisDef>();
        ChassisHandler.ClearParts();
        List<string> allInventoryStrings = __instance.GetAllInventoryStrings();
        foreach (string text in allInventoryStrings)
        {
            int count = __instance.CompanyStats.GetValue<int>(text);
            if (count >= 1)
            {
                string[] array = text.Split(new char[]
                {
                    '.'
                });
                if (array[1] != "MECHPART")
                {
                    BattleTechResourceType battleTechResourceType =
                        (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), array[1]);
                    if (battleTechResourceType == BattleTechResourceType.MechDef)
                    {
                        var cdef = array[2];
                        var mdef = ChassisHandler.GetMDefFromCDef(array[2]);

                        if (__instance.DataManager.Exists(BattleTechResourceType.ChassisDef, cdef) &&
                            __instance.DataManager.Exists(BattleTechResourceType.MechDef, mdef))
                        {
                            var mech = __instance.DataManager.MechDefs.Get(mdef);
                            __result.Add(mech.Chassis);
                            ChassisHandler.RegisterMechDef(mech);
                        }
                        else
                        {
                            Log.Main.Error?.Log($"ERROR: {cdef}/{mdef} not found");
                        }
                    }
                }
                else
                {
                    if (__instance.DataManager.Exists(BattleTechResourceType.MechDef, array[2]))
                    {
                        var mdef = __instance.DataManager.MechDefs.Get(array[2]);
                        if (showMechParts)
                        {
                            var chassisDef = new ChassisDef(mdef.Chassis) { DataManager = __instance.DataManager };
                            chassisDef.Refresh();
                            chassisDef.MechPartCount = count;
                            chassisDef.MechPartMax = __instance.Constants.Story.DefaultMechPartMax;
                            __result.Add(chassisDef);
                        }
                        ChassisHandler.RegisterMechDef(mdef, count);
                    }
                    else
                    {
                        Log.Main.Error?.Log($"ERROR: {array[2]} not found");
                        if (show)
                        {
                            Log.Main.Error?.Log($"AllMechDefs:");
                            foreach (var pair in __instance.DataManager.MechDefs)
                            {
                                Log.Main.Error?.Log($"-- {pair.Key}: {(pair.Value == null ? "null" : "exist")}");
                            }

                            show = false;
                        }
                    }
                }
            }
        }

        //ChassisHandler.ShowInfo();
        __runOriginal = false;
    }
}