using System.Linq;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
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
            GameObject ___readyBtnObj, GameObject ___partsCountObj, TextMeshProUGUI ___partsCountText)
        {
            if (___selectedChassis == null)
                return;

            if (!Control.Settings.AssemblyVariants)
                return;


            var list = ChassisHandler.GetCompatible(___selectedChassis.Description.Id);

            if (___selectedChassis.MechPartCount != 0)
            {
                int min = Mathf.CeilToInt(___selectedChassis.MechPartMax * Control.Settings.MinPartsToAssembly);

                if (___selectedChassis.MechPartCount >= ___selectedChassis.MechPartMax)
                {
                    ___readyBtnObj.SetActive(true);
                    ___partsCountObj.SetActive(false);
                    ___chassisStorageTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject("Enough parts to assemble: Press Ready to move to a Bay"));
                }
                else
                {
                    if (list == null || ___selectedChassis.MechPartCount < min ||  list.Sum(i => ChassisHandler.GetCount(i.Description.Id)) < ___selectedChassis.MechPartMax)
                    {
                        ___readyBtnObj.SetActive(false);
                        ___partsCountObj.SetActive(true);
                        ___partsCountText.SetText("{0} / {1}", new object[]
                        {
                            ___selectedChassis.MechPartCount,
                            ___selectedChassis.MechPartMax
                        });
                        ___chassisStorageTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject($"Chassis still needs at least {min} base and total {___selectedChassis.MechPartMax} compatible parts to be completed"));
                    }
                    else
                    {
                        ___readyBtnObj.SetActive(true);
                        ___partsCountObj.SetActive(false);
                        ___chassisStorageTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject("Chassis can by assembled using other parts: Press Ready to move to a Bay"));
                    }
                }
            }
            else
            {
                ___readyBtnObj.SetActive(true);
                ___partsCountObj.SetActive(false);
                ___chassisStorageTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject("Chassis in storage: Press Ready to move to a Bay"));
            }

            string add = "";

            if (list == null)
            {
                add = $"\n<color=#FFFF00>Special mech: cannot be assembled using other variants</color>";
            }
            else if (list.Count > 1)
            {
                add = $"\n<color=#32CD32>Compatible with owned variants:";

                if (list.Count > Control.Settings.MaxVariantsInDescription + 1)
                {
                    int showed = 0;
                    foreach (var mechDef in list)
                    {
                        if (mechDef.ChassisID == ___selectedChassis.Description.Id)
                            continue;
                        add += "\n" + mechDef.Description.UIName;
                        showed += 1;
                        if (showed == Control.Settings.MaxVariantsInDescription - 1)
                            break;
                    }

                    add += $"\n and {Control.Settings.MaxVariantsInDescription - showed} other variants</color>";
                }
                else
                {
                    foreach (var mechDef in list)
                    {
                        if (mechDef.ChassisID == ___selectedChassis.Description.Id) 
                            continue;
                        add += "\n" + mechDef.Description.UIName;
                    }

                    add += "</color>";
                }

            }

            ___mechDetails.SetText(___mechDetails.text + add);
        }
    }
}