using BattleTech;
using BattleTech.UI;
using CustomComponents;
using HarmonyLib;
using HBS;
using System;
using UnityEngine;

namespace CustomSalvage;

[HarmonyPatch(typeof(MechBayChassisInfoWidget))]
[HarmonyPatch("OnReadyClicked")]
public static class MechBayChassisInfoWidget_OnReadyClicked
{

    public static void SetItemCount(this SimGameState sim, string id, System.Type type, SimGameState.ItemCountType outputType, int count)
    {
        string statid = sim.GetItemStatID(id, type);
        if (outputType == SimGameState.ItemCountType.DAMAGED_ONLY)
        {
            id += string.Format(".{0}", (object)"DAMAGED");
        }
        var stat = sim.CompanyStats.GetStatistic(statid);
        if (stat != null)
        {
            if (count != 0) { stat.SetValue<int>(count); } else { sim.CompanyStats.RemoveStatistic(id); }
        }
        else
        {
            if (count != 0) { sim.CompanyStats.AddStatistic<int>(id, 0).SetValue<int>(count); }
        }
    }
    public static void SetItemCount(this SimGameState sim, string id, string type, SimGameState.ItemCountType outputType, int count)
    {
        string statid = sim.GetItemStatID(id, type);
        if (outputType == SimGameState.ItemCountType.DAMAGED_ONLY)
        {
            id += string.Format(".{0}", (object)"DAMAGED");
        }
        var stat = sim.CompanyStats.GetStatistic(statid);
        if (stat != null) {
            if (count != 0) { stat.SetValue<int>(count); } else { sim.CompanyStats.RemoveStatistic(id); }
        }
        else
        {
            sim.CompanyStats.AddStatistic<int>(id, 0).SetValue<int>(count);
        }
    }
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Prefix(ref bool __runOriginal, MechBayChassisInfoWidget __instance)
    {
        if (!__runOriginal) { return; }
        if (!Control.Instance.Settings.AssemblyVariants) { return; }
        if (__instance.selectedChassis == null) { __runOriginal = false; return; }
        if ((Time.time - ChassisHandler.LAST_MAKE_MECH_TIME) < 10f) { __runOriginal = false; return; }
        try
        {
            if (__instance.mechBay.Sim.GetFirstFreeMechBay() < 0)
            {
                GenericPopupBuilder.Create("Cannot Ready 'Unit", "There are no available slots in the 'Unit's Bay. You must move an active 'Units into storage before readying this chassis.").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                __runOriginal = false;
                return;
            }

            int chassisQuantity = 0;
            if (__instance.selectedChassis.Is<LootableUniqueMech>(out var ulm) && __instance.mechBay.Sim.IsHaveActiveChassis(__instance.selectedChassis.Description.Id))
            {
                Log.Main.Debug?.Log($"Detected unique chassis {__instance.selectedChassis.Description.Id} that is already in bays");
                ChassisHandler.SanitizeUniqueUnits(__instance.mechBay.Sim);
                __instance.mechBay.RefreshData(false);
                __runOriginal = false;
                return;
            }
            ChassisHandler.PreparePopup(__instance.selectedChassis, __instance.mechBay, __instance, __instance.chassisElement);

            chassisQuantity = __instance.mechBay.Sim.GetItemCount(__instance.selectedChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY);
            if (__instance.selectedChassis.MechPartCount == 0)
            {
                if (chassisQuantity <= 0)
                {
                    GenericPopupBuilder.Create("Absent parts", "You does not have parts or complete chassis for this unit in storage")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("Ready", ChassisHandler.OnChassisReady, true, null)
                        .AddFader(
                            new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                                .PopupBackfill), 0f, true)
                        .CancelOnEscape()
                        .Render();
                    __runOriginal = false;
                    return;
                }
                int num2 = Mathf.CeilToInt((float)__instance.mechBay.Sim.Constants.Story.MechReadyTime /
                                           (float)__instance.mechBay.Sim.MechTechSkill);

                GenericPopupBuilder.Create("Ready 'Unit?",
                        $"It will take {num2} day(s) to ready this BattleMech chassis for combat.")
                    .AddButton("Cancel", null, true, null)
                    .AddButton("Ready", ChassisHandler.OnChassisReady, true, null)
                    .AddFader(
                        new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                            .PopupBackfill), 0f, true)
                    .CancelOnEscape()
                    .Render();
                __runOriginal = false;
                return;
            }
            //int mechPartCount = ChassisHandler.GetCount(ChassisHandler.GetMech(___selectedChassis.Description.Id).Description.Id);
            if (__instance.selectedChassis.MechPartCount >= __instance.selectedChassis.MechPartMax)
            {
                if (Control.Instance.Settings.MechBrokeType == BrokeType.Normalized)
                {
                    ChassisHandler.StartDialog();
                }
                else
                {
                    GenericPopupBuilder.Create("Assembly 'Unit?",
                            $"It will take {__instance.selectedChassis.MechPartMax} parts from storage.")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("Ready", ChassisHandler.OnPartsAssembly, true, null)
                        .AddFader(
                            new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                                .PopupBackfill), 0f, true)
                        .CancelOnEscape()
                        .Render();
                }

                __runOriginal = false;
                return;
            }
            if (!Control.Instance.Settings.AssemblyVariants){ return; }
            ChassisHandler.StartDialog();
            __runOriginal = false;
        }catch(Exception e)
        {
            UIManager.logger.LogException(e);
        }
    }

}