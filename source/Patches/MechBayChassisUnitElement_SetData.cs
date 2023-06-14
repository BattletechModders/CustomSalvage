using System;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using HBS.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomSalvage;

[HarmonyPatch(typeof(MechBayChassisUnitElement))]
[HarmonyPatch("SetData")]
public static class MechBayChassisUnitElement_SetData
{
    public static Sprite DefaultSprite { get; set; } = null;

    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Postfix(MechBayChassisUnitElement __instance,
        ChassisDef chassisDef, DataManager dataManager, int partsCount, int partsMax, int chassisQuantity)
    {
        var settings = Control.Instance.Settings;
        if (DefaultSprite == null)
        {
            DefaultSprite = __instance.mechImage.sprite;
        }

        if (partsCount != 0)
        {
            __instance.partsLabelText.SetText("Parts");
            if (Control.Instance.Settings.FullEnemyUnitSalvage == false)
            {
                int empty_parts = ChassisHandler.GetEmptyPartsCount(ChassisHandler.GetMech(chassisDef.Description.Id).Description.Id);
                if (partsCount < empty_parts)
                {
                    ChassisHandler.ResetEmptyPartsCount(ChassisHandler.GetMech(chassisDef.Description.Id).Description.Id);
                    empty_parts = 0;
                }
                __instance.partsText.SetText(Control.Instance.Settings.AllowScrapToParts ? $"<size=80%>({empty_parts}){partsCount}/{partsMax}</size>" : $"{partsCount}/{partsMax}");
            }
            else
            {
                __instance.partsText.SetText($"{partsCount}/{partsMax}");
            }
            if (partsCount >= partsMax)
            {
                __instance.mechImage.color = settings.color_ready;
            }
            else
            {
                int min = ChassisHandler.GetInfo(chassisDef.Description.Id).MinParts;
                var list = ChassisHandler.GetCompatible(chassisDef.Description.Id);
                if (list == null)
                {
                    __instance.mechImage.color = settings.color_exclude;
                }
                else if (list.Sum(i => ChassisHandler.GetCount(i.Description.Id)) >= partsMax && chassisDef.MechPartCount >= min)
                {
                    __instance.mechImage.color = settings.color_variant;
                }
                else
                {
                    __instance.mechImage.color = settings.color_notready;
                }
            }
        }
        else
        {
            __instance.mechImage.color = settings.color_stored;
        }

        var go = __instance.transform.Find("Representation/contents/storage_OverlayBars");

        var mechtags = GetMechTags(chassisDef, dataManager);

        if (mechtags != null)
        {

            if (settings.BGColors != null && settings.BGColors.Length > 0)
            {
                foreach (var color in settings.BGColors)
                {
                    if (mechtags.Contains(color.Tag))
                    {
                        var tracker = go.GetComponent<UIColorRefTracker>();
                        tracker.SetUIColor(UIColor.Custom);
                        tracker.OverrideWithColor(color.color);
                        break;
                    }
                }
            }

            //var tags = Control.Instance.Settings.IconTags;
            //bool set = false;
            //if (tags != null && tags.Length > 0)
            //    foreach (var pair in tags.Where(i => !string.IsNullOrEmpty(i.Tag)))
            //        if (mechtags.Contains(pair.Tag))
            //        {
            //            ___mechImage.sprite = pair.Sprite;
            //            set = true;
            //            break;
            //        }

            //if (!set)
            //    ___mechImage.sprite = DefaultSprite;
        }
        else
        {
            var tracker = go.GetComponent<UIColorRefTracker>();
            tracker.SetUIColor(UIColor.Custom);
            tracker.OverrideWithColor(Color.white);
            //___mechImage.sprite = DefaultSprite;
        }

    }


    public static TagSet GetMechTags(ChassisDef chassis, DataManager dm)
    {
        var mech = ChassisHandler.GetMech(chassis.Description.Id);
            
        return mech?.MechTags;
    }
}