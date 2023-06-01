using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using UnityEngine;


namespace CustomSalvage;

[HarmonyPatch(typeof(AAR_SalvageScreen), "AddNewSalvageEntryToWidget")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_AddNewSalvageEntryToWidget
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Postfix(AAR_SalvageScreen __instance, SalvageDef salvageDef, IMechLabDropTarget targetWidget)
    {
        try
        {
            Log.Main.Debug?.Log($"AAR_SalvageScreen.AddNewSalvageEntryToWidget {salvageDef.Description.Id}:{salvageDef.Type}:{salvageDef.ComponentType} count:{salvageDef.Count}");
            if (salvageDef.ComponentType != ComponentType.MechFull) { return; }
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
            ListElementController_SalvageFullMech_NotListView fullMechNotListView = new ListElementController_SalvageFullMech_NotListView();
            fullMechNotListView.InitAndCreate(salvageDef, __instance, __instance.simState.DataManager, targetWidget, 1, false, true);
            targetWidget.OnAddItem((IMechLabDraggableItem)fullMechNotListView.ItemWidget, false);
            __instance.AllSalvageControllers.Add(fullMechNotListView);
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "AddNewSalvageLeftover")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_AddNewSalvageLeftover
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Postfix(AAR_SalvageScreen __instance, SalvageDef salvageDef)
    {
        try
        {
            if (salvageDef.ComponentType != ComponentType.MechFull) { return; }
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
            ListElementController_SalvageFullMech_NotListView fullMechNotListView = new ListElementController_SalvageFullMech_NotListView();
            fullMechNotListView.InitAndCreate(salvageDef, __instance, __instance.simState.DataManager, null, 1, false, false);
            __instance.salvageChosen.AddLeftovers(fullMechNotListView.ItemWidget);
            __instance.AllSalvageControllers.Add(fullMechNotListView);
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "GetNewSalvageUnitFromStackForDrag")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_GetNewSalvageUnitFromStackForDrag
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Postfix(AAR_SalvageScreen __instance, SalvageDef salvageDef, ref InventoryItemElement_NotListView __result)
    {
        try
        {
            if (__result != null) { return; }
            if (salvageDef.ComponentType != ComponentType.MechFull) { return; }
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
            ListElementController_SalvageFullMech_NotListView fullMechNotListView = new ListElementController_SalvageFullMech_NotListView();
            fullMechNotListView.InitAndCreate(salvageDef, __instance, __instance.simState.DataManager, null, 1, false, false);
            __instance.AllSalvageControllers.Add(fullMechNotListView);
            __result = fullMechNotListView.ItemWidget;
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}
