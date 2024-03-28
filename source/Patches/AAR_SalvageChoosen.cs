using BattleTech.UI;
using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BattleTech.BinkMedia;
using UnityEngine.EventSystems;

namespace CustomSalvage;

public class AAR_SalvageChosenCustom: MonoBehaviour
{
    public AAR_SalvageChosen parent;
    public void Init(AAR_SalvageChosen parent)
    {
        this.parent = parent;
        while (this.parent.tempHoldingGridSpaces.Count > 1)
        {
            var go = this.parent.tempHoldingGridSpaces[this.parent.tempHoldingGridSpaces.Count - 1];
            this.parent.tempHoldingGridSpaces.RemoveAt(this.parent.tempHoldingGridSpaces.Count - 1);
            GameObject.DestroyImmediate(go);
        }
    }
    public void Refresh()
    {
        int curSalvageWeight = this.parent.contract.GetSalvageWeight(this.parent.PriorityInventory);
        int restSalvage = this.parent.contract.FinalSalvageCount - curSalvageWeight;
        int restRandom = restSalvage - this.parent.contract.FinalPrioritySalvageCount + this.parent.PriorityInventory.Count;
        int maxPriority = this.parent.contract.FinalPrioritySalvageCount;
        if (restRandom < 0) { maxPriority += restRandom; restRandom = 0; }
        Log.Main.Debug?.Log($"AAR_SalvageChosenCustom.Refresh priority {this.parent.PriorityInventory.Count} weight:{curSalvageWeight}");
        this.parent.selectNumberText.SetText("SELECTED {0}/{1}", this.parent.PriorityInventory.Count, maxPriority);
        if(maxPriority <= this.parent.PriorityInventory.Count)
        {
            this.parent.tempHoldingGridSpaces[0].SetActive(false);
        }
        else
        {
            this.parent.tempHoldingGridSpaces[0].SetActive(true);
        }
        this.parent.leftoversText.SetText("AFTER CHOOSING PRIORITY ITEMS YOU WILL RECEIVE UP TO {0} ADDITIONAL PIECES OF SALVAGE", restRandom);
        this.parent.parent.salvageAgreement.TotalNumberText.SetText($"{restRandom + maxPriority}");
        this.parent.parent.salvageAgreement.PriorityNumberText.SetText($"{maxPriority}");
        this.parent.parent.gameObject.GetComponent<AAR_ScreenFullMechHelper>()?.CheckDisassemble();
        this.parent.ApplySorting();
        this.parent.TestHaveAllPriority();
        this.parent.PriorityAreaBackground.transform.SetAsFirstSibling();
        this.parent.ForceRefreshImmediate();
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "HasAllPriority")]
[HarmonyPatch(new Type[] { })]
internal static class AAR_SalvageChosen_HasAllPriority
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, ref bool __result)
    {
        if (__runOriginal == false) { return; }
        __result = true;
        __runOriginal = false;
    }
    public static void Postfix(AAR_SalvageChosen __instance, ref bool __result)
    {
        __result = true;
    }
}
public class AAR_SalvageChosen_Confirm : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool hovered = false;
    public bool clicked = false;
    public AAR_SalvageChosen parent = null;
    public AAR_SalvageChosenCustom parentCutom = null;
    public void Init(AAR_SalvageChosen p, AAR_SalvageChosenCustom pc)
    {
        this.parent = p;
        this.parentCutom = pc;
        hovered = false;
        clicked = false;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerClick");
        if(this.parent == null)
        {
            Log.Main.Debug?.Log($" -- no parent. skipping");
        }
        if (hovered == false) {
            Log.Main.Debug?.Log($" -- not hovered. skipping");
        }
        if(clicked == true)
        {
            Log.Main.Debug?.Log($" -- multiply clicks detected. skipping");
        }
        try
        {
            this.parentCutom.Refresh();
            this.clicked = true;
            if (this.parent.tempHoldingGridSpaces[0].activeSelf)
            {
                GenericPopupBuilder.Create(GenericPopupType.Warning, "You still not filled priority slots. They will be filled randomly. Are you sure to proceed?")
                    .AddButton("OK", () =>
                    {
                        this.parent.parent.SalvageConfirmed();
                    }, true).AddButton("Cancel", () =>
                    {
                        this.clicked = false;
                    }, true).SetOnClose(() => { this.clicked = false; }).AddFader().SetAlwaysOnTop().Render();
            }
            else
            {
                this.parent.parent.SalvageConfirmed();
            }
        }
        catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }
        //throw new NotImplementedException();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerEnter");
        hovered = true;
        //throw new NotImplementedException();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Log.Main.Debug?.Log($"AAR_SalvageChosen_Confirm.OnPointerExit");
        hovered = false;
        //throw new NotImplementedException();
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "Init")]
[HarmonyPatch(new Type[] { typeof(AAR_SalvageScreen), typeof(SimGameState), typeof(Contract) })]
internal static class AAR_SalvageChosen_Init
{
    public static void Postfix(AAR_SalvageChosen __instance, AAR_SalvageScreen theScreen, SimGameState sim, Contract theContract)
    {
        var custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
        if (custom == null) { custom = __instance.gameObject.AddComponent<AAR_SalvageChosenCustom>(); }
        custom.Init(__instance);
        AAR_SalvageChosen_Confirm confirm = __instance.confirmButton.gameObject.GetComponent<AAR_SalvageChosen_Confirm>();
        if (confirm == null) { confirm = __instance.confirmButton.gameObject.AddComponent<AAR_SalvageChosen_Confirm>(); }
        confirm.Init(__instance, custom);
        AAR_SalvageScreen_ReceiveButtonPress.ClearFlag();
    }
}


[HarmonyPatch(typeof(AAR_SalvageChosen), "ReceiveButtonPress")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class AAR_SalvageScreen_ReceiveButtonPress
{
    private static bool ReceiveButtonPress_flag = false;
    public static void ClearFlag() { ReceiveButtonPress_flag = false; }
    public static void SetFlag() { ReceiveButtonPress_flag = true; }
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, string button)
    {
        try
        {            
            if (__runOriginal == false) { return; }
            if (button != "ConfirmSalvage") { return; }
            __runOriginal = false;
            return;
            //Log.Main.Debug?.Log($"AAR_SalvageChosen.ReceiveButtonPress flag:{ReceiveButtonPress_flag}");
            //if (ReceiveButtonPress_flag) { __runOriginal = false; return; }
            //ReceiveButtonPress_flag = true;
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "SetInitialText")]
internal static class AAR_SalvageChosen_SetInitialText
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance)
    {
        if (__runOriginal == false) { return; }
        //__instance.contract.FinalSalvageCount = 15;
        //__instance.contract.FinalPrioritySalvageCount = 5;
        var custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
        if (custom == null) { return; }
        __runOriginal = false;
        __instance.currentSort = new Comparison<InventoryItemElement_NotListView>(__instance.SortBy_SalvageTypeThenName);
        custom.Refresh();
    }
}


[HarmonyPatch(typeof(AAR_SalvageChosen), "OnAddItem")]
internal static class AAR_SalvageChosen_OnAddItem
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool validate, ref bool __result)
    {
        if (!__runOriginal) { return; }
        try
        {
            InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
            ListElementController_BASE_NotListView controller = elementNotListView.controller;
            if (controller == null) { goto main_process; }
            if (controller.salvageDef == null) { goto main_process; }
            if (controller.salvageDef.Type != SalvageDef.SalvageType.MECH) { goto main_process; }
            if (Control.Instance.Settings.MaxFullUnitsInSalvage > 0)
            {
                int fullunits = 0;
                foreach (var salvage in __instance.PriorityInventory)
                {
                    if (salvage.controller.salvageDef.Type == SalvageDef.SalvageType.MECH) { ++fullunits; }
                }
                if (fullunits >= Control.Instance.Settings.MaxFullUnitsInSalvage)
                {
                    __runOriginal = false;
                    GenericPopupBuilder.Create("UNABLE TO COMPLY", $"You can only have up to {Control.Instance.Settings.MaxFullUnitsInSalvage} units in priority salvage").CancelOnEscape().Render();
                    __result = false;
                    return;
                }
            }
            int restSlots = __instance.contract.GetFinalSalvageCount(__instance.PriorityInventory) - __instance.PriorityInventory.Count;
            int neededSlots = controller.salvageDef.mechDef.RandomSlotsUsing(__instance.Sim.Constants) + 1;
            if (restSlots < neededSlots)
            {
                __runOriginal = false;
                GenericPopupBuilder.Create("UNABLE TO COMPLY", $"You have only {restSlots} of salvage. But to get this unit you need {neededSlots}").CancelOnEscape().Render();
                __result = false;
                return;
            }
        main_process:
            Log.Main.Debug?.Log($"AAR_SalvageChosen.OnAddItem");
            if (__instance.tempHoldingGridSpaces[0].activeSelf == false) {
                __runOriginal = false;
                __result = false;
                return;
            }
            __instance.PriorityInventory.Add(elementNotListView);
            __instance.parent.RemoveFromInventoryList(elementNotListView);
            elementNotListView.DropParent = (IMechLabDropTarget)__instance;
            HBSDOTweenToggle component = elementNotListView.GetComponent<HBSDOTweenToggle>();
            if (component != null) { component.SetState(ButtonState.Enabled); }
            elementNotListView.gameObject.transform.SetParent(__instance.chosenListParent, false);
            elementNotListView.gameObject.transform.localScale = Vector3.one;
            AAR_SalvageChosenCustom custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
            if (custom != null)
            {
                custom.Refresh();
            }
            else
            {
                Log.Main.Warning?.Log($"can't find AAR_SalvageChosenCustom component on {__instance.gameObject.name}");
            }
            __runOriginal = false;
            __result = true;
        }catch(Exception e)
        {
            __runOriginal = false;
            __result = false;
            UIManager.logger.LogException(e);
        }
    }
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool __result)
    {
        if (__result == false) { return; }
        try
        {
            InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
            FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
            if (info != null)
            {
                info.allowDisassemble = false;
            }
            AAR_ScreenFullMechHelper helper = __instance.parent.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
            helper?.CheckDisassemble();
        }catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "OnRemoveItem")]
internal static class AAR_SalvageChosen_OnRemoveItem
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, IMechLabDraggableItem item, ref bool __result)
    {
        if (__runOriginal == false) { return; }
        try
        {
            if (!__instance.IsSimGame) { __result = false; __runOriginal = false; return; }
            Log.Main.Debug?.Log($"AAR_SalvageChosen.OnRemoveItem");
            __instance.PriorityInventory.Remove(item as InventoryItemElement_NotListView);
            AAR_SalvageChosenCustom custom = __instance.gameObject.GetComponent<AAR_SalvageChosenCustom>();
            if (custom != null)
            {
                custom.Refresh();
            }
            else
            {
                Log.Main.Warning?.Log($"can't find AAR_SalvageChosenCustom component on {__instance.gameObject.name}");
            }
            __result = false; __runOriginal = false;
        }
        catch(Exception e)
        {
            __result = false; __runOriginal = false;
            UIManager.logger.LogException(e);
        }
    }
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item)
    {
        InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
        FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
        if (info != null)
        {
            info.allowDisassemble = true;
        }
    }
}
