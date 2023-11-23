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
