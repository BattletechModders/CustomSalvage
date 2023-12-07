using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using UIWidgets;
using UIWidgetsSamples.Shops;
using UnityEngine;
using UnityEngine.EventSystems;


namespace CustomSalvage;

public class DelayConfirmSlavage: MonoBehaviour
{
    public GenericPopup popup = null;
    public AAR_SalvageScreen parent = null;
    public bool eventFired = false;
    public float t = 0f;
    public void Init(AAR_SalvageScreen parent, GenericPopup p) { this.parent = parent; this.popup = p; this.eventFired = false; t = 0f; }
    public void Update()
    {
        if (eventFired) { return; }
        if (popup == null) { return; }
        Log.Main.Debug?.Log($"DelayConfirmSlavage.Update {t}");
        if (t > 1f)
        {
            try
            {
                Log.Main.Debug?.Log($" -- invoke");
                popup.buttons[0].OnClicked.Invoke();
                eventFired = true;
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
        else
        {
            t += Time.deltaTime;
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "SalvageConfirmed")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_SalvageConfirmed
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, AAR_SalvageScreen __instance)
    {
        try
        {
            if (__runOriginal == false) { return; }
            Log.Main.Debug?.Log($"AAR_SalvageScreen.SalvageConfirmed");
            bool needDisassemble = false;
            var helper = __instance.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
            if (helper != null) { needDisassemble = helper.CheckDisassemble(); }
            if (needDisassemble) {
                __runOriginal = false;
                DelayConfirmSlavage delay = __instance.gameObject.GetComponent<DelayConfirmSlavage>();
                if (delay == null) { delay = __instance.gameObject.AddComponent<DelayConfirmSlavage>(); }
                delay.Init(__instance, GenericPopupBuilder.Create("WARNING", $"Units dissassembling. Please wait ...").AddButton("OK", () =>
                {
                    try
                    {
                        delay.popup = null; delay.eventFired = true; __instance.SalvageConfirmed();
                    }catch(Exception ex)
                    {
                        UIManager.logger.LogException(ex);
                    }
                }, true).SetAlwaysOnTop().CancelOnEscape().Render());
                helper.OnClicked();
            }
            else
            {
                DelayConfirmSlavage delay = __instance.gameObject.GetComponent<DelayConfirmSlavage>();
                if (delay != null)
                {
                    GameObject.Destroy(delay);
                }
            }
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "OnButtonDoubleClicked")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_OnButtonDoubleClicked
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, AAR_SalvageScreen __instance,IMechLabDraggableItem item)
    {
        try
        {
            if (__runOriginal == false) { return; }
            Log.Main.Debug?.Log($"AAR_SalvageScreen.OnButtonDoubleClicked");
            __runOriginal = false;
            if (item.DropParent.dropTargetType == MechLabDropTargetType.SalvageChose)
            {
                __instance.OnItemGrab(item, (PointerEventData)null);
                __instance.OnMechLabDrop((PointerEventData)null, MechLabDropTargetType.SalvageList);
            }
            else
            {
                if ((item.DropParent.dropTargetType != MechLabDropTargetType.InventoryList) ||( __instance.salvageChosen.tempHoldingGridSpaces[0].activeSelf == false))
                {
                    return;
                }
                __instance.OnItemGrab(item, (PointerEventData)null);
                __instance.OnMechLabDrop((PointerEventData)null, MechLabDropTargetType.SalvageChose);
            }
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
            __runOriginal = true;
        }
    }
}


[HarmonyPatch(typeof(AAR_SalvageScreen), "AddNewSalvageEntryToWidget")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_AddNewSalvageEntryToWidget
{
    [HarmonyPostfix]
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

[HarmonyPatch(typeof(AAR_SalvageScreen), "InitializeData")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_InitializeData
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(AAR_SalvageScreen __instance)
    {
        //GameInstance game = SceneSingletonBehavior<UnityGameInstance>.Instance.Game;
        //game.Combat.ActiveContract.FinalPrioritySalvageCount = 7;
        //game.Combat.ActiveContract.FinalSalvageCount = 28;
    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "BeginSalvageScreen")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_BeginSalvageScreen
{

    public static void DisassembleAllFinalPotentialSalvage(this AAR_SalvageScreen __instance)
    {
        ContractHelper contract = new ContractHelper(__instance.contract, false);
        List<SalvageDef> fullMechs = new List<SalvageDef>();
        foreach (var salvageDef in contract.FinalPotentialSalvage)
        {
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (salvageDef.ComponentType != ComponentType.MechFull) { continue; }
            fullMechs.Add(salvageDef);
        }
        foreach (var salvageDef in fullMechs)
        {
            if (contract.FinalPotentialSalvage.Remove(salvageDef))
            {
                Contract_GenerateSalvage.AddMechToSalvage(salvageDef.mechDef, contract, __instance.simState, __instance.simState.Constants, true, true);
            }
        }
        //AAR_ScreenFullMechHelper helper = __instance.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
        //if (helper != null) { helper.ReadSalvage(contract.FinalPotentialSalvage); }
    }
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(AAR_SalvageScreen __instance)
    {
        try
        {
            AAR_ScreenFullMechHelper helper = __instance.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
            if (helper == null) { helper = AAR_ScreenFullMechHelper.Instantine(__instance); }
            helper.Init(__instance);
            if (__instance.contract.FinalPrioritySalvageCount < 1)
            {
                __instance.DisassembleAllFinalPotentialSalvage();
                return;
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
    public static void Finalizer(AAR_SalvageScreen __instance)
    {
        try
        {
            AAR_ScreenFullMechHelper helper = __instance.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
            helper?.CheckDisassemble();
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }

    }
}

[HarmonyPatch(typeof(AAR_SalvageScreen), "OnAddItem")]
internal static class AAR_SalvageScreen_OnAddItem
{
    public static void Postfix(AAR_SalvageScreen __instance)
    {
        AAR_ScreenFullMechHelper helper = __instance.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
        helper?.CheckDisassemble();
    }
}


[HarmonyPatch(typeof(AAR_SalvageScreen), "AddItemToLocation")]
internal static class AAR_SalvageScreen_AddItemToLocation
{
    public static void Postfix(AAR_SalvageScreen __instance)
    {
        //__instance.salvageAgreement.TotalNumberText.SetText(__instance.contract.GetFinalSalvageCount(__instance.salvageChosen.PriorityInventory).ToString());
    }
}

