using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using TMPro;
using UnityEngine;

namespace CustomSalvage;

[HarmonyPatch(typeof(SGEventPanel), "SetEvent")]
public static class SGEventPanel_SetEvent
{
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Postfix(SimGameEventDef evt, SGEventPanel __instance,
        TextMeshProUGUI ___eventDescription,
        DataManager ___dm, RectTransform ___optionParent, List<SGEventOption> ___optionsList)
    {
        Log.Main.Debug?.Log("Started Event: " + evt.Description.Id);
        if (evt.Description.Id != "CustomSalvageAssemblyEvent")
        {
            return;
        }

        ChassisHandler.MakeOptions(___eventDescription, __instance, ___dm, ___optionParent, ___optionsList);
    }
}