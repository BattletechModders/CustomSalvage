using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using UnityEngine;


namespace CustomSalvage;

[HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class Contract_FinalizeSalvage
{
    public static int GetFinalSalvageCount(this Contract contract, List<SalvageDef> priorityItems)
    {
        int result = contract.FinalSalvageCount;
        foreach(var salvageDef in priorityItems)
        {
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (Control.Instance.Settings.FullUnitUsedAllRandomSalvageSlots) { return contract.FinalPrioritySalvageCount; }
            result -= (Mathf.FloorToInt(contract.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax * Control.Instance.Settings.FullUnitRandomSalvageSlotUsingMod)-1);
        }
        return result;
    }
    public static int GetFinalSalvageCount(this Contract contract, List<InventoryItemElement_NotListView> PriorityInventory)
    {
        int result = contract.FinalSalvageCount;
        foreach (var elementNotListView in PriorityInventory)
        {
            ListElementController_BASE_NotListView controller = elementNotListView.controller;
            if (controller == null) { continue; }
            if (controller.salvageDef == null) { continue; }
            if (controller.salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (Control.Instance.Settings.FullUnitUsedAllRandomSalvageSlots) { return contract.FinalPrioritySalvageCount; }
            result -= (Mathf.FloorToInt(contract.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax * Control.Instance.Settings.FullUnitRandomSalvageSlotUsingMod) - 1);
        }
        return result;
    }
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, List<SalvageDef> priorityItems, Contract __instance)
    {
        if (!__runOriginal)
        {
            return;
        }
        __runOriginal = false;
        Log.Main.Debug?.Log($"Finalize salvage {__instance.Name}");
        try
        {
            SimGameState simulation = __instance.BattleTechGame.Simulation;
            var Contract = new ContractHelper(__instance, false);
            int finalSalvageCount = __instance.GetFinalSalvageCount(priorityItems);
            int prioritySalvageCount = 0;
            if (priorityItems != null)
                prioritySalvageCount = priorityItems.Count;
            //int num2 = prioritySalvageCount;
            int randomSalvageCount = finalSalvageCount - prioritySalvageCount;
            if (priorityItems != null)
            {
                while (priorityItems.Count > 0)
                {
                    SalvageDef def = new SalvageDef(priorityItems[0]);
                    priorityItems.RemoveAt(0);
                    def.Count = 1;
                    __instance.AddToFinalSalvage(def);
                    SalvageDef salvageDef = __instance.finalPotentialSalvage.Find((Predicate<SalvageDef>)(x => x.Description.Id == def.Description.Id && x.Damaged == def.Damaged && x.Type == def.Type && x.mechDef == def.mechDef));
                    if (salvageDef != null)
                    {
                        --salvageDef.Count;
                        if (salvageDef.Count < 1)
                            __instance.finalPotentialSalvage.Remove(salvageDef);
                    }
                }
            }
            List<SalvageDef> restFullChassis = new List<SalvageDef>();
            Log.Main.Debug?.Log($" -- salvage before dissassemble {__instance.finalPotentialSalvage.Count}");
            for (int i=0;i < __instance.finalPotentialSalvage.Count;)
            {
                if (__instance.finalPotentialSalvage[i].Type == SalvageDef.SalvageType.MECH)
                {
                    SalvageDef salvageDef = __instance.finalPotentialSalvage[i];
                    __instance.finalPotentialSalvage.RemoveAt(i);
                    Contract_GenerateSalvage.AddMechToSalvage(salvageDef.mechDef, Contract, __instance.BattleTechGame.Simulation, __instance.BattleTechGame.Simulation.Constants, true, true);
                    continue;
                }
                ++i;
            }
            Log.Main.Debug?.Log($" -- salvage after dissassemble {__instance.finalPotentialSalvage.Count}");
            List<int> weights = new List<int>();
            for (int index = 0; index < __instance.finalPotentialSalvage.Count; ++index)
                weights.Add(__instance.finalPotentialSalvage[index].Weight);
            while (__instance.finalPotentialSalvage.Count > 0 && randomSalvageCount > 0)
            {
                int weightedResult = SimGameState.GetWeightedResult(weights, simulation.NetworkRandom.Float());
                SalvageDef original = __instance.finalPotentialSalvage[weightedResult];
                int num4 = 0;
                if (original != null)
                {
                    num4 = original.Count - 1;
                    --randomSalvageCount;
                    __instance.AddToFinalSalvage(new SalvageDef(original)
                    {
                        Count = 1
                    });
                }
                if (num4 > 0)
                {
                    __instance.finalPotentialSalvage[weightedResult].Count = num4;
                }
                else
                {
                    __instance.finalPotentialSalvage.RemoveAt(weightedResult);
                    weights.RemoveAt(weightedResult);
                }
            }
            if (!__instance.loggingSalvageResults)
                return;
            __instance.PushReport("Received Salvage");
            foreach (SalvageDef salvageResult in __instance.SalvageResults)
                __instance.ReportLog(string.Format("{0} of type {1} in damage state of {2}", (object)salvageResult.Description.Id, (object)salvageResult.Type, (object)salvageResult.Damaged));
            __instance.PopReport();
            __instance.PopReport();
        }
        catch(Exception e)
        {
            Contract.logger.LogException(e);
        }
    }
}