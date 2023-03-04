using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using TMPro;
using UnityEngine;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(SGEventPanel), "SetEvent")]
    public static class SGEventPanel_SetEvent
    {
        [HarmonyPostfix]
        public static void ModifyOptions(SimGameEventDef evt, SGEventPanel __instance,
            TextMeshProUGUI ___eventDescription,
            DataManager ___dm, RectTransform ___optionParent, List<SGEventOption> ___optionsList)
        {
            try
            {
                Log.Main.Debug?.Log("Started Event: " + evt.Description.Id);
                if (evt.Description.Id != "CustomSalvageAssemblyEvent")
                    return;
                ChassisHandler.MakeOptions(___eventDescription, __instance, ___dm, ___optionParent, ___optionsList);

            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("ModifyOptions error", e);
                
            }
        }
    }
}