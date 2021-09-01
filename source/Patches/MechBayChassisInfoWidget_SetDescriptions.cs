using System;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
using HBS.Collections;
using Localize;
using TMPro;
using UnityEngine;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(MechBayChassisInfoWidget))]
    [HarmonyPatch("SetDescriptions")]
    public static class MechBayChassisInfoWidget_SetDescriptions
    {
        [HarmonyPostfix]
        public static void AddVariantsToDescriptions(
            MechBayChassisInfoWidget __instance, ChassisDef ___selectedChassis,
            TextMeshProUGUI ___mechDetails, HBSTooltip ___chassisStorageTooltip,
            GameObject ___readyBtnObj, GameObject ___partsCountObj,
            MechUnitElementWidget ___mechIcon, TextMeshProUGUI ___partsCountText)
        {
            if (___selectedChassis == null)
                return;


            var settings = Control.Instance.Settings;
            if (!settings.AssemblyVariants)
                return;


            var list = ChassisHandler.GetCompatible(___selectedChassis.Description.Id);

            if (___selectedChassis.MechPartCount != 0)
            {
                int min = ChassisHandler.GetInfo(___selectedChassis.Description.Id).MinParts;

                if (___selectedChassis.MechPartCount >= ___selectedChassis.MechPartMax)
                {
                    ___readyBtnObj.SetActive(true);
                    ___partsCountObj.SetActive(false);
                    ___chassisStorageTooltip.SetDefaultStateData(
                        TooltipUtilities.GetStateDataFromObject(
                            "Enough parts to assemble: Press Ready to move to a Bay"));
                }
                else
                {
                    if (list == null || ___selectedChassis.MechPartCount < min ||
                        list.Sum(i => ChassisHandler.GetCount(i.Description.Id)) < ___selectedChassis.MechPartMax)
                    {
                        ___readyBtnObj.SetActive(false);
                        ___partsCountObj.SetActive(true);
                        ___partsCountText.SetText(
                            $"{___selectedChassis.MechPartCount} / {___selectedChassis.MechPartMax}");
                        ___chassisStorageTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(
                            $"Chassis still needs at least {min} base and total {___selectedChassis.MechPartMax} compatible parts to be completed"));
                    }
                    else
                    {
                        ___readyBtnObj.SetActive(true);
                        ___partsCountObj.SetActive(false);
                        ___chassisStorageTooltip.SetDefaultStateData(
                            TooltipUtilities.GetStateDataFromObject(
                                "Chassis can by assembled using other parts: Press Ready to move to a Bay"));
                    }
                }
            }
            else
            {
                ___readyBtnObj.SetActive(true);
                ___partsCountObj.SetActive(false);
                ___chassisStorageTooltip.SetDefaultStateData(
                    TooltipUtilities.GetStateDataFromObject("Chassis in storage: Press Ready to move to a Bay"));
            }

            string after = "";
            string before = "";

            if (list == null)
            {
                after = $"\n<color=#FFFF00>Special mech: cannot be assembled using other variants</color>";
            }
            else if (list.Count > 1)
            {
                after = $"\n<color=#32CD32>Compatible with owned variants:";

                if (list.Count > settings.MaxVariantsInDescription + 1)
                {
                    int showed = 0;
                    foreach (var mechDef in list)
                    {
                        if (mechDef.ChassisID == ___selectedChassis.Description.Id)
                            continue;
                        after += "\n" + new Text(mechDef.Description.UIName).ToString();
                        showed += 1;
                        if (showed == settings.MaxVariantsInDescription - 1)
                            break;
                    }

                    after += $"\n and {settings.MaxVariantsInDescription - showed} other variants</color>";
                }
                else
                {
                    foreach (var mechDef in list)
                    {
                        if (mechDef.ChassisID == ___selectedChassis.Description.Id)
                            continue;
                        after += "\n" + new Text(mechDef.Description.UIName).ToString();
                    }

                    after += "</color>";
                }

            }


            if (settings.ShowBrokeChances)
            {
                string broke_chance = "";

                var mech = ChassisHandler.GetMech(___selectedChassis.Description.Id);
                switch (settings.MechBrokeType)
                {
                    case BrokeType.Random:
                        var chances =
                            new ChassisHandler.AssemblyChancesResult(mech, UnityGameInstance.BattleTechGame.Simulation,
                                0);

                        broke_chance += $"Base Tech: {chances.BaseTP}";
                        broke_chance += $"\nLimb Repair: {(int)(chances.LimbChance * 100)}%";
                        broke_chance += $"\nItem Recovered: {(int)(chances.CompNFChance * 100)}%";
                        broke_chance += $"\nItem Repair: {(int)(chances.CompFChance * 100)}%";
#if CCDEBUG
                        if (settings.ShowDEBUGChances)
                        {
                            after += chances.DEBUGText;
                        }
#endif
                        break;
                    case BrokeType.Normalized:
                        broke_chance = "Total TechBonus: " + MechBroke.DiceBroke
                            .GetBonus(mech, UnityGameInstance.BattleTechGame.Simulation, 0)
                            .ToString("+0;-#");
                        break;
                }

                if (settings.ShowBrokeChancesFirst)
                    before = broke_chance + "\n";
                else
                    after += "\n" + broke_chance;
            }
            ___mechDetails.SetText(before + ___mechDetails.text + after);

        }
    }
}