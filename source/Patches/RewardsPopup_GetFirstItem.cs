using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CustomComponents;
using CustomUnits;

namespace CustomSalvage.Patches;

[HarmonyPatch(typeof(RewardsPopup), "GetFirstItem")]
public static class RewardsPopup_GetFirstItem
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Prefix(ref bool __runOriginal, RewardsPopup __instance)
    {
        Settings settings = Control.Instance.Settings;
        if (!__runOriginal || !settings.RewardRareItemByValue)
        {
            return;
        }

        List<SalvageDef> salvageDefs = __instance.allSalvageDefs;
        if (salvageDefs == null || salvageDefs.Count == 0)
        {
            return;
        }

        salvageDefs.Sort((a, b) => CompareByValue(a, b, __instance.dm));
        Log.Main.Debug?.Log($"Highest value item was {salvageDefs[0].Description?.Id} ({GetValue(salvageDefs[0], __instance.dm)} c-bills)");
        __instance.rarestSalvageDef = salvageDefs[0];
        salvageDefs.RemoveAt(0);
        switch (settings.RewardSortOrder)
        {
            case RewardSortType.Rarity:
                salvageDefs.Sort(__instance.SortBy_RarityThenCBills);
                break;
            case RewardSortType.Value:
                break;
            case RewardSortType.CategoryThenValue:
                salvageDefs.Sort((a, b) => CompareByCategoryThenValue(a, b, __instance.dm));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        __instance.SetLabelText();
        __runOriginal = false;
    }

    private static int CompareByValue(SalvageDef a, SalvageDef b, DataManager dataManager)
    {
        if (a == null)
        {
            return b == null ? 0 : 1;
        }
        if (b == null)
        {
            return -1;
        }
        int result = GetValue(b, dataManager).CompareTo(GetValue(a, dataManager));
        if (result != 0)
        {
            return result;
        }
        return string.CompareOrdinal(a.Description?.Id, b.Description?.Id);
    }

    private static int CompareByCategoryThenValue(SalvageDef a, SalvageDef b, DataManager dataManager)
    {
        if (a == null || b == null)
        {
            return CompareByValue(a, b, dataManager);
        }
        int result = GetUnitGroup(a, dataManager).CompareTo(GetUnitGroup(b, dataManager));
        if (result != 0)
        {
            return result;
        }
        result = string.CompareOrdinal(InventorySorterComparer.SortKey(a.MechComponentDef), InventorySorterComparer.SortKey(b.MechComponentDef));
        if (result != 0)
        {
            return result;
        }
        return CompareByValue(a, b, dataManager);
    }

    private static int GetUnitGroup(SalvageDef salvageDef, DataManager dataManager)
    {
        bool isPart = salvageDef.Type == SalvageDef.SalvageType.MECH_PART;
        if (!isPart && salvageDef.Type != SalvageDef.SalvageType.CHASSIS)
        {
            return 4;
        }
        bool isVehicle = salvageDef.Description != null && dataManager.MechDefs.TryGet(salvageDef.Description.Id, out MechDef mechDef) && mechDef.IsVehicle();
        if (isVehicle)
        {
            return isPart ? 3 : 2;
        }
        return isPart ? 1 : 0;
    }

    private static long GetValue(SalvageDef salvageDef, DataManager dataManager)
    {
        if (salvageDef.Description == null)
        {
            return 0;
        }
        if (salvageDef.Type == SalvageDef.SalvageType.MECH_PART && dataManager.MechDefs.TryGet(salvageDef.Description.Id, out MechDef mechDef))
        {
            return Math.Max(0, mechDef.SimGameMechPartCost);
        }
        return Math.Max(0, salvageDef.Description.Cost);
    }
}
