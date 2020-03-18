using System;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(MechBayChassisUnitElement))]
    [HarmonyPatch("SetData")]
    public static class MechBayChassisUnitElement_SetData
    {
        [HarmonyPostfix]
        public static void SetColor(MechBayChassisUnitElement __instance, Image ___mechImage, TextMeshProUGUI ___partsText, TextMeshProUGUI ___partsLabelText,
            ChassisDef chassisDef, DataManager dataManager, int partsCount, int partsMax, int chassisQuantity)
        {
            try
            {
                if (partsCount != 0)
                {
                    ___partsLabelText.SetText("Parts");
                    ___partsText.SetText($"{partsCount} / {partsMax}");

                    if (partsCount >= partsMax)
                        ___mechImage.color = Control.Settings.color_ready;
                    else
                    {
                        int min = ChassisHandler.GetInfo(chassisDef.Description.Id).MinParts;
                        var list = ChassisHandler.GetCompatible(chassisDef.Description.Id);
                        if (list == null)
                            ___mechImage.color = Control.Settings.color_exclude;
                        else if (list.Sum(i => ChassisHandler.GetCount(i.Description.Id)) >= partsMax && chassisDef.MechPartCount >= min)
                            ___mechImage.color = Control.Settings.color_variant;
                        else
                            ___mechImage.color = Control.Settings.color_notready;
                    }
                }
                else
                    ___mechImage.color = Control.Settings.color_stored;

                var go = __instance.transform.Find("Representation/contents/storage_OverlayBars");

                var mech = ChassisHandler.GetMech(chassisDef.Description.Id);

                if (Control.Settings.BGColors != null && Control.Settings.BGColors.Length > 0)
                    foreach (var color in Control.Settings.BGColors)
                    {
                        if (mech.MechTags.Contains(color.Tag))
                        {
                            var tracker = go.GetComponent<UIColorRefTracker>();
                            tracker.SetUIColor(UIColor.Custom);
                            tracker.OverrideWithColor(color.color);
                            break;
                        }
                    }
            }
            catch (Exception e)
            {
                Control.LogDebug("Error while get mechdef for " + chassisDef.Description.Id);
            }
        }
    }
}