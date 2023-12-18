using BattleTech;
using BattleTech.UI;
using CustomComponents;
using HarmonyLib;
using HBS;
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
        if (!__runOriginal)
        {
            return;
        }

        if (!Control.Instance.Settings.AssemblyVariants)
        {
            return;
        }

        if (__instance.selectedChassis == null)
        {
            __runOriginal = false;
            return;
        }

        if (__instance.mechBay.Sim.GetFirstFreeMechBay() < 0)
        {
            GenericPopupBuilder.Create("Cannot Ready 'Unit", "There are no available slots in the 'Unit's Bay. You must move an active 'Units into storage before readying this chassis.").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
            __runOriginal = false;
            return;
        }

        int chassisQuantity = 0;
        if (__instance.selectedChassis.Is<LootableUniqueMech>(out var ulm) && __instance.mechBay.Sim.IsHaveActiveChassis(__instance.selectedChassis.Description.Id))
        {
            if (__instance.mechBay.Sim.DataManager.ChassisDefs.TryGet(ulm.ReplaceID, out var replaceChassis))
            {
                chassisQuantity = __instance.mechBay.Sim.GetItemCount(__instance.selectedChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY);
                __instance.mechBay.Sim.SetItemCount(__instance.selectedChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY, 0);
                chassisQuantity += __instance.mechBay.Sim.GetItemCount(replaceChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY);
                __instance.mechBay.Sim.SetItemCount(replaceChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY, chassisQuantity);
                replaceChassis.MechPartCount += __instance.selectedChassis.MechPartCount;
                __instance.selectedChassis.MechPartCount = 0;
                __instance.chassisElement.chassisDef = replaceChassis;
                __instance.chassisElement.partsCount = replaceChassis.MechPartCount;
                __instance.mechBay.Sim.SetItemCount(replaceChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY, chassisQuantity);
                var replaceMechId = ChassisHandler.GetMDefFromCDef(replaceChassis.Description.Id);
                var currentMechId = ChassisHandler.GetMDefFromCDef(__instance.selectedChassis.Description.Id);
                __instance.selectedChassis = replaceChassis;
                if ((string.IsNullOrEmpty(replaceMechId) == false)&&(string.IsNullOrEmpty(currentMechId) == false)
                    &&(__instance.mechBay.Sim.DataManager.ChassisDefs.TryGet(replaceMechId, out var replacemech))
                    && (__instance.mechBay.Sim.DataManager.ChassisDefs.TryGet(currentMechId, out var currentmech)))
                {
                    var mechPartsCount = __instance.mechBay.Sim.GetItemCount(currentMechId, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                    mechPartsCount += __instance.mechBay.Sim.GetItemCount(replaceMechId, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                    __instance.mechBay.Sim.SetItemCount(replaceMechId, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY, mechPartsCount);
                    __instance.mechBay.Sim.SetItemCount(currentMechId, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY, 0);
                }
            }
        }
        ChassisHandler.PreparePopup(__instance.selectedChassis, __instance.mechBay, __instance, __instance.chassisElement);

        chassisQuantity = __instance.mechBay.Sim.GetItemCount(__instance.selectedChassis.Description.Id, typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY);
        if (__instance.selectedChassis.MechPartCount == 0)
        {
            if(chassisQuantity <= 0)
            {
                GenericPopupBuilder.Create("Absent parts","You does not have parts or complete chassis for this unit in storage")
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

        if (!Control.Instance.Settings.AssemblyVariants)
        {
            return;
        }

        ChassisHandler.StartDialog();

        __runOriginal = false;
    }

}