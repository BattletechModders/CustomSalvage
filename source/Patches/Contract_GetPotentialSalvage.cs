using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using System.Threading;
using IRBTModUtils;
using BattleTech.Data;
using BattleTech.UI.TMProWrapper;
using CustomUnits;
using BattleTech.Save.SaveGameStructure;
using HBS;

namespace CustomSalvage;

[HarmonyPatch(typeof(Contract), "GetPotentialSalvage")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class Contract_GetPotentialSalvage
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, Contract __instance,ref List<SalvageDef> __result)
    {
        if (!__runOriginal)
        {
            return;
        }
        __runOriginal = false;
        __result = new List<SalvageDef>();
        Log.Main.Debug?.Log($"Contract.GetPotentialSalvage {__instance.Name}");
        try
        {
            if (__instance.finalPotentialSalvage != null)
            {
                foreach (SalvageDef salvageDef in __instance.finalPotentialSalvage)
                {
                    if (salvageDef.Type == SalvageDef.SalvageType.MECH) { __result.Add(new SalvageDef(salvageDef)); }
                    if (__result.Find((x) => x.Description.Id == salvageDef.Description.Id && x.Damaged == salvageDef.Damaged && x.Type == salvageDef.Type) != null) { continue; }
                    var def = new SalvageDef(salvageDef);
                    def.Count = __instance.finalPotentialSalvage.FindAll((x) => x.Description.Id == def.Description.Id && x.Damaged == def.Damaged && x.Type == def.Type).Count;
                    __result.Add(def);
                }
                Log.Main.Debug?.Log($" -- result {__result.Count}");
                foreach(var salvageDef in __result)
                {
                    Log.Main.Debug?.Log($"   -- {salvageDef.Description.Id}:{salvageDef.Type}:{salvageDef.ComponentType} count:{salvageDef.Count}");
                }
            }
        }
        catch(Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
        }
    }
}

[HarmonyPatch(typeof(Contract), "IsSalvageInContent")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class Contract_IsSalvageInContent
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, Contract __instance, SalvageDef def, ref bool __result)
    {
        if (!__runOriginal) { return; }
        if (def.Type != SalvageDef.SalvageType.MECH) { return; }
        try
        {
            __result = __instance.DataManager.ResourceLocator.EntryByID(def.Description.Id, BattleTechResourceType.MechDef, true) != null;
            __runOriginal = false; return;
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e.ToString());
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
        foreach(var salvageDef in contract.FinalPotentialSalvage)
        {
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (salvageDef.ComponentType != ComponentType.MechFull) { continue; }
            fullMechs.Add(salvageDef);
        }
        foreach(var salvageDef in fullMechs)
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
            if(__instance.contract.FinalPrioritySalvageCount < 1)
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
        catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }

    }
}

[HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
internal static class Contract_ResolveCompleteContract
{
    public static List<SalvageDef> salvageMechDefsReplacements { get; set; } = new List<SalvageDef>();
    public static int CreateMechPlacementPopup_i = 0;
    public static void Prefix(SimGameState __instance, ref bool __state)
    {
        try
        {
            Thread.CurrentThread.SetFlag("IN_ResolveCompleteContract");
            salvageMechDefsReplacements.Clear();
            CreateMechPlacementPopup_i = 0;
            Log.Main.Debug?.Log("SimGameState.ResolveCompleteContract");
            if (__instance.CompletedContract.SalvageResults != null)
            {
                foreach (SalvageDef salvageResult in __instance.CompletedContract.SalvageResults)
                {
                    if (salvageResult.Type == SalvageDef.SalvageType.MECH) {
                        salvageResult.Type = SalvageDef.SalvageType.CHASSIS;
                        Log.Main.Debug?.Log($" -- found full mech salvage {salvageResult.Description.Id}");
                        salvageResult.Description.Id = salvageResult.mechDef.ChassisID;
                        salvageMechDefsReplacements.Add(salvageResult);
                        Log.Main.Debug?.Log($"   -- replacing {salvageResult.mechDef.Description.Id} to {salvageResult.Description.Id}");
                    }
                    else if(salvageResult.Type == SalvageDef.SalvageType.CHASSIS)
                    {
                        salvageMechDefsReplacements.Add(salvageResult);
                    }
                }
            }
            __state = true;
        }catch(Exception e)
        {
            SimGameState.logger.LogException(e);
        }
    }
    public static void Finalizer(SimGameState __instance,Exception __exception, bool __state)
    {
        if (__state) { Thread.CurrentThread.ClearFlag("IN_ResolveCompleteContract"); }
        if (__exception != null) { SimGameState.logger.LogException(__exception); }
    }
}

[HarmonyPatch(typeof(SimGameState), "CreateMechPlacementPopup")]
[HarmonyPatch(new Type[] { typeof(MechDef) })]
internal static class Contract_CreateMechPlacementPopup
{
    public static void Prefix(ref bool __runOriginal, SimGameState __instance, ref MechDef m)
    {
        Log.Main.Debug?.Log("SimGameState.CreateMechPlacementPopup");
        if (Thread.CurrentThread.isFlagSet("IN_ResolveCompleteContract") == false)
        {
            Log.Main.Debug?.Log(" -- not in ResolveCompleteContract. pass");
            return;
        }
        if(Contract_ResolveCompleteContract.CreateMechPlacementPopup_i < 0)
        {
            Log.Main.Debug?.Log($" -- CreateMechPlacementPopup_i {Contract_ResolveCompleteContract.CreateMechPlacementPopup_i}. should not happen");
            return;
        }
        if (Contract_ResolveCompleteContract.CreateMechPlacementPopup_i >= Contract_ResolveCompleteContract.salvageMechDefsReplacements.Count)
        {
            Log.Main.Debug?.Log($" -- CreateMechPlacementPopup_i {Contract_ResolveCompleteContract.CreateMechPlacementPopup_i} >= {Contract_ResolveCompleteContract.salvageMechDefsReplacements.Count}. should not happen");
            return;
        }
        try
        {
            SalvageDef salvageResult = Contract_ResolveCompleteContract.salvageMechDefsReplacements[Contract_ResolveCompleteContract.CreateMechPlacementPopup_i];
            Log.Main.Debug?.Log($" -- salvage [{Contract_ResolveCompleteContract.CreateMechPlacementPopup_i}] {salvageResult.Description.Id} type:{salvageResult.Type} ComponentType:{salvageResult.ComponentType}");
            if (salvageResult.ComponentType != ComponentType.MechFull)
            {
                Log.Main.Debug?.Log($"   -- not a full mech. Just chassis. pass");
            }
            else
            {
                salvageResult.Type = SalvageDef.SalvageType.MECH;
                salvageResult.Description.Id = salvageResult.mechDef.Description.Id;
                m = salvageResult.mechDef;
                m.SetGuid(__instance.GenerateSimGameUID());
                Log.Main.Debug?.Log($"   -- replacing bare chassis to real mech {m.Description.Id}:{m.GUID}");
                if (Control.Instance.Settings.VehicleAddSanitize)
                {
                    if (m.IsVehicle())
                    {
                        foreach(var component in m.inventory)
                        {
                            if (component.DamageLevel >= ComponentDamageLevel.Destroyed) { component.DamageLevel = ComponentDamageLevel.NonFunctional; }
                        }
                        foreach(var location in m.Locations)
                        {
                            location.CurrentArmor = location.AssignedArmor;
                        }
                    }
                }
                //Log.Main.Debug?.Log(m.ToJSON());
                Thread.CurrentThread.ClearFlag("IN_ResolveCompleteContract");
                try
                {
                    __instance.AddMech(-1, m, true, false, true, "Unit salvaged");
                }catch(Exception ex)
                {
                    SimGameState.logger.LogException(ex);
                }
                Thread.CurrentThread.SetFlag("IN_ResolveCompleteContract");
                __runOriginal = false;
            }
        }catch(Exception e)
        {
            SimGameState.logger.LogException(e);
        }
        ++Contract_ResolveCompleteContract.CreateMechPlacementPopup_i;
    }
}
[HarmonyPatch(typeof(ListElementController_BASE_NotListView), "Pool")]
internal static class ListElementController_BASE_NotListView_Pool
{
    [HarmonyWrapSafe]
    public static void Prefix(ref bool __runOriginal, ListElementController_BASE_NotListView __instance)
    {
        if (!__runOriginal) { return; }
        if (__instance.ItemWidget == null) { return; }
        if (__instance.ItemWidget.gameObject.name.Contains(FullMechSalvageInfo.FULL_MECH_SUFFIX) == false) { return; }
        __runOriginal = false;
        __instance.ItemWidget.SetClickable(true);
        __instance.ItemWidget.SetDraggable(true);
        FullMechSalvageInfo info = __instance.ItemWidget.GetComponentInChildren<FullMechSalvageInfo>();
        if (info != null) { info.Pool(); }
        __instance.dataManager.PoolGameObject(ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView + FullMechSalvageInfo.FULL_MECH_SUFFIX, __instance.ItemWidget.gameObject);
        __instance.ItemWidget = null;
    }
}


[HarmonyPatch(typeof(ListElementController_SalvageFullMech_NotListView), "InitAndCreate")]
internal static class ListElementController_SalvageFullMech_NotListView_InitAndCreate
{
    public static void InitAndCreate(this ListElementController_SalvageFullMech_NotListView __instance, SalvageDef theSalvageDef, AAR_SalvageScreen salvageScreen, DataManager dm, IMechLabDropTarget dropParent, int theQuantity, bool isStoreItem, bool allow_disassemble)
    {
        try
        {
            __instance.dataManager = dm;
            GameObject gameObject = __instance.dataManager.PooledInstantiate(ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView + FullMechSalvageInfo.FULL_MECH_SUFFIX, BattleTechResourceType.UIModulePrefabs);
            if (gameObject == null)
            {
                gameObject = __instance.dataManager.PooledInstantiate(ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView, BattleTechResourceType.UIModulePrefabs);
                gameObject.name = ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView + FullMechSalvageInfo.FULL_MECH_SUFFIX + "(Clone)";
            }
            if (gameObject != null)
            {
                __instance.ItemWidget = gameObject.GetComponent<InventoryItemElement_NotListView>();
                __instance.ItemWidget.SetData(__instance, dropParent, theQuantity);
            }
            __instance.simState = salvageScreen.simState;
            __instance.salvageDef = theSalvageDef;
            __instance.componentDef = theSalvageDef.MechComponentDef;
            __instance.quantity = theQuantity;
            __instance.mechDef = theSalvageDef.mechDef;
            __instance.chassisDef = __instance.mechDef.Chassis;
            __instance.SetupLook(__instance.ItemWidget);
            FullMechSalvageInfo mechinfo = __instance.ItemWidget.GetComponentInChildren<FullMechSalvageInfo>(true);
            if (mechinfo == null) { mechinfo = FullMechSalvageInfo.Instantine(__instance.ItemWidget); }
            mechinfo.Init(__instance, salvageScreen);
        }
        catch (Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }

    public static void Prefix(ref bool __runOriginal, ListElementController_SalvageFullMech_NotListView __instance, SalvageDef theSalvageDef, SimGameState theSim, DataManager dm, IMechLabDropTarget dropParent, int theQuantity, bool isStoreItem)
    {
        if (!__runOriginal) { return; }
        if (theSalvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
        __runOriginal = false;
        __instance.InitAndCreate(theSalvageDef, UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<AAR_SalvageScreen>(), dm, dropParent, theQuantity, isStoreItem, false);
    }
}

[HarmonyPatch(typeof(ListElementController_SalvageFullMech_NotListView), "GetCBillValue")]
internal static class ListElementController_SalvageFullMech_NotListView_GetCBillValue
{
    public static void Postfix(ListElementController_SalvageFullMech_NotListView __instance, ref int __result)
    {
        if(__instance.salvageDef.Type == SalvageDef.SalvageType.MECH)
        {
            __result = __instance.mechDef.Description.Cost;
        }
    }
}

[HarmonyPatch(typeof(ListElementController_SalvageFullMech_NotListView), "RefreshInfoOnWidget")]
internal static class ListElementController_SalvageFullMech_NotListView_RefreshInfoOnWidget
{
    public static void Postfix(ListElementController_SalvageFullMech_NotListView __instance, InventoryItemElement_NotListView theWidget)
    {
        var texts = theWidget.mechPartsNumbersText.transform.parent.gameObject.GetComponentsInChildren<LocalizableText>(true);
        LocalizableText count_label = null;
        foreach(var text in texts)
        {
            if (text.transform.name == "parts_Label") {
                count_label = text;
                break; 
            }
        }
        if (count_label != null) { count_label.SetText("You Have:"); }
        if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
        if (__instance.salvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
        if (__instance.salvageDef.ComponentType != ComponentType.MechFull) { return; }
        if (__instance.salvageDef.mechDef == null) { return; }
        if (count_label != null) { count_label.SetText("Need slots:"); }
        theWidget.mechPartsNumbersText.SetText("{0}+{1}",1, __instance.salvageDef.mechDef.RandomSlotsUsing(UnityGameInstance.BattleTechGame.Simulation.Constants));
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "OnAddItem")]
internal static class AAR_SalvageChosen_OnAddItem
{
    public static void Prefix(ref bool __runOriginal, AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool validate, ref bool __result)
    {
        if (!__runOriginal) { return; }
        InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
        ListElementController_BASE_NotListView controller = elementNotListView.controller;
        if (controller == null) { return; }
        if (controller.salvageDef == null) { return; }
        if (controller.salvageDef.Type != SalvageDef.SalvageType.MECH) { return; }
        if(Control.Instance.Settings.MaxFullUnitsInSalvage > 0)
        {
            int fullunits = 0;
            foreach(var salvage in __instance.PriorityInventory)
            {
                if (salvage.controller.salvageDef.Type == SalvageDef.SalvageType.MECH) { ++fullunits; }
            }
            if (fullunits >= Control.Instance.Settings.MaxFullUnitsInSalvage) {
                __runOriginal = false;
                GenericPopupBuilder.Create("UNABLE TO COMPLY",$"You can only have up to {Control.Instance.Settings.MaxFullUnitsInSalvage} units in priority salvage").CancelOnEscape().Render();
                __result = false;
                return;
            }
        }
        //if (Control.Instance.Settings.FullUnitUsedAllRandomSalvageSlots == false)
        //{
        int restSlots = __instance.contract.GetFinalSalvageCount(__instance.PriorityInventory) - __instance.PriorityInventory.Count;
        int neededSlots = controller.salvageDef.mechDef.RandomSlotsUsing(__instance.Sim.Constants) + 1;
        if (restSlots < neededSlots)
        {
            __runOriginal = false;
            GenericPopupBuilder.Create("UNABLE TO COMPLY", $"You have only {restSlots} of salvage. But to get this unit you need {neededSlots}").CancelOnEscape().Render();
            __result = false;
            return;
        }
        //}
    }
    private static int real_FinalPrioritySalvageCount = 0;
    private static int ContractHash = 0;
    public static void InitRealFinalPrioritySalvageCount(this Contract contract)
    {
        if (contract.GetHashCode() != ContractHash) {
            real_FinalPrioritySalvageCount = contract.FinalPrioritySalvageCount;
            ContractHash = contract.GetHashCode();
        }
    }
    public static int RealFinalPrioritySalvageCount(this Contract contract)
    {
        if (contract.GetHashCode() != ContractHash)
        {
            real_FinalPrioritySalvageCount = contract.FinalPrioritySalvageCount;
            ContractHash = contract.GetHashCode();
        }
        return real_FinalPrioritySalvageCount;
    }
    public static void ReconsiderPrioritySalvage(this AAR_SalvageScreen salvageScreen)
    {
        Log.Main.Debug?.Log($"ReconsiderPrioritySalvage");
        int restSlots = salvageScreen.contract.GetFinalSalvageCount(salvageScreen.salvageChosen.PriorityInventory);
        salvageScreen.contract.InitRealFinalPrioritySalvageCount();
        int neededPrioritySalvageCount = salvageScreen.contract.FinalPrioritySalvageCount;
        if(restSlots < salvageScreen.contract.FinalPrioritySalvageCount)
        {
            neededPrioritySalvageCount = restSlots;
        }
        if((restSlots > salvageScreen.contract.FinalPrioritySalvageCount) && (salvageScreen.contract.RealFinalPrioritySalvageCount() != salvageScreen.contract.FinalPrioritySalvageCount))
        {
            neededPrioritySalvageCount = restSlots;
            if (neededPrioritySalvageCount > salvageScreen.contract.RealFinalPrioritySalvageCount()) {
                neededPrioritySalvageCount = salvageScreen.contract.RealFinalPrioritySalvageCount();
            }
        }
        Log.Main.Debug?.Log($" rest slots:{restSlots} final salvage:{salvageScreen.contract.FinalPrioritySalvageCount} neededPrioritySalvage:{neededPrioritySalvageCount} real final salvage:{salvageScreen.contract.RealFinalPrioritySalvageCount()}");
        if (neededPrioritySalvageCount != salvageScreen.contract.FinalPrioritySalvageCount) {
            salvageScreen.contract.FinalPrioritySalvageCount = neededPrioritySalvageCount;
            int num1 = salvageScreen.contract.FinalPrioritySalvageCount;
            int num2 = salvageScreen.contract.FinalSalvageCount - num1;
            int salvageMadeAvailable = salvageScreen.TotalSalvageMadeAvailable;
            if (num1 > salvageMadeAvailable)
                num1 = salvageMadeAvailable;
            if (num2 > salvageMadeAvailable - num1)
                num2 = salvageMadeAvailable - num1;
            salvageScreen.salvageChosen.selectNumberText.SetText("SELECT {0} ITEMS", (object)num1);
            salvageScreen.salvageChosen.leftoversText.SetText("AFTER CHOOSING PRIORITY ITEMS YOU WILL RECEIVE UP TO {0} ADDITIONAL PIECES OF SALVAGE", (object)num2);
            int holdingSpaces = salvageScreen.contract.FinalPrioritySalvageCount - salvageScreen.salvageChosen.PriorityInventory.Count;
            for (int index = 0; index < salvageScreen.salvageChosen.tempHoldingGridSpaces.Count; ++index)
            {
                if (index < holdingSpaces)
                    salvageScreen.salvageChosen.tempHoldingGridSpaces[index].SetActive(true);
                else
                    salvageScreen.salvageChosen.tempHoldingGridSpaces[index].SetActive(false);
            }
            salvageScreen.salvageChosen.TestHaveAllPriority();
            salvageScreen.salvageChosen.ForceRefreshImmediate();
            salvageScreen.salvageAgreement.FillInData();
        }
    }
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item, bool __result)
    {
        if (__result == false) { return; }
        InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
        FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
        if (info != null) { 
            info.allowDisassemble = false;
            __instance.parent.ReconsiderPrioritySalvage();
        }
        AAR_ScreenFullMechHelper helper = __instance.parent.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
        helper?.CheckDisassemble();
        int restSlots = __instance.contract.GetFinalSalvageCount(__instance.PriorityInventory);
    }
}

[HarmonyPatch(typeof(AAR_SalvageChosen), "OnRemoveItem")]
internal static class AAR_SalvageChosen_OnRemoveItem
{
    public static void Postfix(AAR_SalvageChosen __instance, IMechLabDraggableItem item)
    {
        InventoryItemElement_NotListView elementNotListView = item as InventoryItemElement_NotListView;
        FullMechSalvageInfo info = elementNotListView.gameObject.GetComponentInChildren<FullMechSalvageInfo>(true);
        if (info != null) { 
            info.allowDisassemble = true;
            __instance.parent.ReconsiderPrioritySalvage();
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
        __instance.salvageAgreement.TotalNumberText.SetText(__instance.contract.GetFinalSalvageCount(__instance.salvageChosen.PriorityInventory).ToString());
    }
}

//[HarmonyPatch(typeof(SimGameState), "ResolveSalvage")]
//internal static class Contract_ResolveSalvage
//{
//    public static void Postfix(SimGameState __instance, StringBuilder report)
//    {
//        Log.Main.Debug?.Log($"SimGameState.ResolveSalvage");
//        try
//        {
//            foreach (SalvageDef salvageDef in __instance.CompletedContract.SalvageResults)
//            {
//                switch (salvageDef.Type)
//                {
//                    case SalvageDef.SalvageType.COMPONENT:
//                        for (int i = 0; i < salvageDef.Count; i++)
//                        {
//                            __instance.AddItemStat(salvageDef.Description.Id, salvageDef.ComponentType.ToString() + "Def", salvageDef.Damaged);
//                        }
//                        break;
//                    case SalvageDef.SalvageType.MECH_PART:
//                        for (int j = 0; j < salvageDef.Count; j++)
//                        {
//                            __instance.AddMechPart(salvageDef.Description.Id);
//                        }
//                        break;
//                    case SalvageDef.SalvageType.CHASSIS:
//                        {
//                            MechDef m = new MechDef(__instance.DataManager.ChassisDefs.Get(salvageDef.Description.Id).Description, salvageDef.Description.Id, new MechComponentRef[0], __instance.DataManager);
//                            __instance.CreateMechPlacementPopup(m);
//                            break;
//                        }
//                    case SalvageDef.SalvageType.MECH:
//                        {
//                            MechDef m = new MechDef(salvageDef.mechDef, __instance.GenerateSimGameUID());
//                            Log.Main.Debug?.Log($"  -- add full mech {m.Description.Id}:{m.GUID}");
//                            __instance.CreateMechPlacementPopup(m);
//                            break;
//                        }
//                }
//                report.AppendFormat("• {0}", salvageDef.Description.Name).AppendLine();
//            }

//        }
//        catch (Exception e)
//        {
//            Log.Main.Error?.Log(e.ToString());
//        }
//    }
//}
