using System;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using HBS.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(MechBayChassisUnitElement))]
    [HarmonyPatch("SetData")]
    public static class MechBayChassisUnitElement_SetData
    {
        public static Sprite DefaultSprite { get; set; } = null;

        [HarmonyPostfix]
        public static void SetColor(MechBayChassisUnitElement __instance, Image ___mechImage, TextMeshProUGUI ___partsText, TextMeshProUGUI ___partsLabelText,
            ChassisDef chassisDef, DataManager dataManager, int partsCount, int partsMax, int chassisQuantity)
        {
            try
            {
                var settings = Control.Instance.Settings;
                if (DefaultSprite == null)
                    DefaultSprite = ___mechImage.sprite;
                if (partsCount != 0)
                {
                    ___partsLabelText.SetText("Parts");
                    ___partsText.SetText($"{partsCount} / {partsMax}");

                    if (partsCount >= partsMax)
                        ___mechImage.color = settings.color_ready;
                    else
                    {
                        int min = ChassisHandler.GetInfo(chassisDef.Description.Id).MinParts;
                        var list = ChassisHandler.GetCompatible(chassisDef.Description.Id);
                        if (list == null)
                            ___mechImage.color = settings.color_exclude;
                        else if (list.Sum(i => ChassisHandler.GetCount(i.Description.Id)) >= partsMax && chassisDef.MechPartCount >= min)
                            ___mechImage.color = settings.color_variant;
                        else
                            ___mechImage.color = settings.color_notready;
                    }
                }
                else
                    ___mechImage.color = settings.color_stored;

                var go = __instance.transform.Find("Representation/contents/storage_OverlayBars");

                var mechtags = GetMechTags(chassisDef, dataManager);

                if (mechtags != null)
                {

                    if (settings.BGColors != null && settings.BGColors.Length > 0)
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
            catch (Exception e)
            {
                Log.Main.Debug?.Log("Error setting data for " + chassisDef.Description.Id, e);
            }
        }


        public static TagSet GetMechTags(ChassisDef chassis, DataManager dm)
        {
            var mech = ChassisHandler.GetMech(chassis.Description.Id);
            
            return mech?.MechTags;
        }
    }
}